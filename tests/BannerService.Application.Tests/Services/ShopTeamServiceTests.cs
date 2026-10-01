using BannerService.Application.DTOs;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class ShopTeamServiceTests
{
    private readonly Mock<IShopRepository> _shops = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly IPasswordHashService _hasher = new PasswordHashService();
    private readonly ShopTeamService _service;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Shop _shop;
    private readonly List<User> _shopUsers = new();

    public ShopTeamServiceTests()
    {
        _shop = new Shop { Id = Guid.NewGuid(), Name = "Test shop", OwnerUserId = _ownerId };
        _shopUsers.Add(new User { Id = _ownerId.ToString(), Email = "owner@example.com", ShopId = _shop.Id.ToString() });

        _shops.Setup(r => r.GetByIdAsync(_shop.Id)).ReturnsAsync(_shop);
        _shops.Setup(r => r.UpdateAsync(It.IsAny<Shop>())).ReturnsAsync((Shop s) => s);
        _users.Setup(r => r.GetByShopIdAsync(_shop.Id.ToString())).ReturnsAsync(() => _shopUsers.Where(u => u.IsActive).ToList());
        _users.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync((string e) => _shopUsers.Any(u => u.Email == e.ToLowerInvariant()));
        _users.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync((User u) => { _shopUsers.Add(u); return u; });
        _users.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _subscriptions.Setup(r => r.GetByShopIdAsync(_shop.Id))
            .ReturnsAsync(new Subscription { ShopId = _shop.Id, Status = SubscriptionStatus.Active });

        _service = new ShopTeamService(_shops.Object, _users.Object, _subscriptions.Object, _roles.Object, _hasher);
    }

    private static AddSalesExecutiveDto Executive(string email) =>
        new() { Email = email, Password = "Password123!", FirstName = "Sam", LastName = "Seller" };

    [Fact]
    public async Task AddSalesExecutive_UpToTwo_Succeeds()
    {
        await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("a@example.com"));
        await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("b@example.com"));

        var team = await _service.GetTeamAsync(_shop.Id, _ownerId);
        Assert.Equal(2, team.SalesExecutives.Count);
        _roles.Verify(r => r.AssignRoleAsync(It.IsAny<string>(), "3"), Times.Exactly(2));
    }

    [Fact]
    public async Task AddSalesExecutive_ThirdLogin_IsRejected()
    {
        await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("a@example.com"));
        await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("b@example.com"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("c@example.com")));
    }

    [Fact]
    public async Task RemoveThenAdd_StaysAtTwoActiveLogins()
    {
        var first = await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("a@example.com"));
        await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("b@example.com"));

        await _service.RemoveSalesExecutiveAsync(_shop.Id, _ownerId, first.UserId);
        await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("c@example.com"));

        var team = await _service.GetTeamAsync(_shop.Id, _ownerId);
        Assert.Equal(2, team.SalesExecutives.Count);
        Assert.DoesNotContain(team.SalesExecutives, m => m.UserId == first.UserId);
    }

    [Fact]
    public async Task AddSalesExecutive_WithoutSubscription_IsRejected()
    {
        _subscriptions.Setup(r => r.GetByShopIdAsync(_shop.Id)).ReturnsAsync((Subscription?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("a@example.com")));
    }

    [Fact]
    public async Task AddSalesExecutive_ByNonOwner_IsForbidden()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.AddSalesExecutiveAsync(_shop.Id, Guid.NewGuid(), Executive("a@example.com")));
    }

    [Fact]
    public async Task AddSalesExecutive_ShortPassword_IsRejected()
    {
        var request = Executive("a@example.com");
        request.Password = "short";

        await Assert.ThrowsAsync<ArgumentException>(() => _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, request));
    }

    [Fact]
    public async Task SetApprovers_OwnerAndOneExecutive_BothCanApprove()
    {
        var exec = await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("a@example.com"));

        await _service.SetApproversAsync(_shop.Id, _ownerId,
            new SetApproversDto { OwnerIsApprover = true, ApproverUserIds = new List<string> { exec.UserId } });

        Assert.True(_shop.CanApprove(_ownerId));
        Assert.True(_shop.CanApprove(Guid.Parse(exec.UserId)));
    }

    [Fact]
    public async Task SetApprovers_ExecutiveOnly_OwnerCannotApprove()
    {
        var exec = await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("a@example.com"));

        await _service.SetApproversAsync(_shop.Id, _ownerId,
            new SetApproversDto { OwnerIsApprover = false, ApproverUserIds = new List<string> { exec.UserId } });

        Assert.False(_shop.CanApprove(_ownerId));
        Assert.True(_shop.CanApprove(Guid.Parse(exec.UserId)));
    }

    [Fact]
    public async Task SetApprovers_NoApproverAtAll_IsRejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SetApproversAsync(_shop.Id, _ownerId,
            new SetApproversDto { OwnerIsApprover = false, ApproverUserIds = new List<string>() }));
    }

    [Fact]
    public async Task SetApprovers_UnknownUser_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SetApproversAsync(_shop.Id, _ownerId,
            new SetApproversDto { OwnerIsApprover = true, ApproverUserIds = new List<string> { Guid.NewGuid().ToString() } }));
    }

    [Fact]
    public async Task RemovingTheOnlyApproverExecutive_FallsBackToOwner()
    {
        var exec = await _service.AddSalesExecutiveAsync(_shop.Id, _ownerId, Executive("a@example.com"));
        await _service.SetApproversAsync(_shop.Id, _ownerId,
            new SetApproversDto { OwnerIsApprover = false, ApproverUserIds = new List<string> { exec.UserId } });

        await _service.RemoveSalesExecutiveAsync(_shop.Id, _ownerId, exec.UserId);

        Assert.True(_shop.OwnerIsApprover);
        Assert.True(_shop.CanApprove(_ownerId));
        Assert.False(_shop.CanApprove(Guid.Parse(exec.UserId)));
    }
}
