using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _repository = new();
    private readonly NotificationService _service;
    private List<InAppNotification> _added = new();

    public NotificationServiceTests()
    {
        _repository.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyCollection<InAppNotification>>()))
            .Callback((IReadOnlyCollection<InAppNotification> items) => _added = items.ToList()).Returns(Task.CompletedTask);
        _service = new NotificationService(_repository.Object, NullLogger<NotificationService>.Instance);
    }

    [Fact]
    public async Task OneMessagePerPerson_EvenWhenAPersonIsListedTwice_AndBlanksAreSkipped()
    {
        await _service.NotifyUsersAsync(new[] { "a", "b", "a", " ", "" }, "Kind", "Title", "Message", "/approvals");

        Assert.Equal(new[] { "a", "b" }, _added.Select(n => n.UserId).OrderBy(x => x));
        Assert.All(_added, n => Assert.Equal("/approvals", n.LinkUrl));
    }

    [Theory]
    [InlineData("https://evil.example/x")]
    [InlineData("//evil.example/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData(@"/\evil.example")]
    public async Task OnlyAPageOfThisApplicationCanBeLinked(string link)
    {
        await _service.NotifyUsersAsync(new[] { "a" }, "Kind", "Title", "Message", link);

        Assert.Null(Assert.Single(_added).LinkUrl);
    }

    [Fact]
    public async Task LongTextIsShortenedToFitTheColumns()
    {
        await _service.NotifyUsersAsync(new[] { "a" }, new string('k', 100), new string('t', 500), new string('m', 5000));

        var n = Assert.Single(_added);
        Assert.Equal(40, n.Kind.Length);
        Assert.Equal(NotificationService.MaxTitle, n.Title.Length);
        Assert.Equal(NotificationService.MaxMessage, n.Message.Length);
    }

    [Fact]
    public async Task AFailureToStoreNeverReachesTheCaller()
    {
        _repository.Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyCollection<InAppNotification>>())).ThrowsAsync(new InvalidOperationException("db down"));
        _repository.Setup(r => r.FindUserIdsByRoleAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ThrowsAsync(new InvalidOperationException("db down"));

        await _service.NotifyUsersAsync(new[] { "a" }, "K", "T", "M");
        await _service.NotifyAdminsAsync("K", "T", "M");
    }

    [Fact]
    public async Task AdminsAndShopOwnersAreFoundByRole()
    {
        var shop = Guid.NewGuid();
        _repository.Setup(r => r.FindUserIdsByRoleAsync(Role.Admin, null)).ReturnsAsync(new List<string> { "admin1" });
        _repository.Setup(r => r.FindUserIdsByRoleAsync(Role.ShopOwner, shop)).ReturnsAsync(new List<string> { "owner1", "owner2" });

        await _service.NotifyAdminsAsync("K", "T", "M");
        Assert.Equal(new[] { "admin1" }, _added.Select(n => n.UserId));

        await _service.NotifyShopOwnersAsync(shop, "K", "T", "M");
        Assert.Equal(new[] { "owner1", "owner2" }, _added.Select(n => n.UserId));
    }

    [Fact]
    public async Task ListingClampsThePageAndMarksReadState()
    {
        var read = new InAppNotification { UserId = "a", ReadAt = DateTime.UtcNow };
        _repository.Setup(r => r.ListAsync("a", false, 1, 50)).ReturnsAsync((new List<InAppNotification> { read, new() { UserId = "a" } }, 2));
        _repository.Setup(r => r.CountUnreadAsync("a")).ReturnsAsync(1);

        var page = await _service.ListAsync("a", false, 0, 9999);

        Assert.Equal(1, page.Page);
        Assert.Equal(50, page.PageSize);
        Assert.Equal(1, page.Unread);
        Assert.Equal(new[] { true, false }, page.Items.Select(i => i.IsRead));
    }

    [Fact]
    public async Task MarkingSomeoneElsesMessageReadIsNotFound()
    {
        _repository.Setup(r => r.MarkReadAsync("a", It.IsAny<Guid>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.MarkReadAsync("a", Guid.NewGuid()));
    }

    [Fact]
    public async Task PurgeKeepsReadMessagesForThirtyDaysAndUnreadForNinety()
    {
        var now = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await _service.PurgeAsync(now);

        _repository.Verify(r => r.PurgeAsync(now.AddDays(-30), now.AddDays(-90)), Times.Once);
    }
}
