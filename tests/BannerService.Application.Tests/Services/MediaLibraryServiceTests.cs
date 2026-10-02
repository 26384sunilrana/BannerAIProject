using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Infrastructure.Security;
using BannerService.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class MediaLibraryServiceTests
{
    private readonly Mock<IMediaFileRepository> _files = new();
    private readonly Mock<IUploadChunkRepository> _chunks = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<IStorageProvider> _storage = new();
    private readonly Mock<IMediaUrlSigner> _signer = new();
    private readonly MediaLibraryService _library;
    private readonly Guid _shop = Guid.NewGuid();

    public MediaLibraryServiceTests()
    {
        _signer.Setup(s => s.Sign(It.IsAny<Guid>(), It.IsAny<DateTime>())).Returns("SIG");
        _chunks.Setup(c => c.GetChunksByMediaFileAsync(It.IsAny<Guid>())).ReturnsAsync(new List<UploadChunk>());
        _library = new MediaLibraryService(_files.Object, _chunks.Object, _subscriptions.Object, _storage.Object, _signer.Object, NullLogger<MediaLibraryService>.Instance);
    }

    private MediaFile AFile(string name = "logo.png", int type = 1, long size = 1000, int status = 2) =>
        new(_shop, name, type == 1 ? "image/png" : "video/mp4", size, type, Guid.NewGuid()) { Status = status };

    // ----- listing

    [Fact]
    public async Task List_GivesEachFileASignedLinkAndSaysWhichAreInUse()
    {
        var used = AFile("used.png");
        var free = AFile("free.png");
        _files.Setup(f => f.GetActivePageAsync(_shop, null, null, 1, 24)).ReturnsAsync((new List<MediaFile> { used, free }, 2));
        _files.Setup(f => f.FindUsingBannersAsync(_shop, used.Id)).ReturnsAsync(new List<string> { "Summer Sale" });
        _files.Setup(f => f.FindUsingBannersAsync(_shop, free.Id)).ReturnsAsync(new List<string>());

        var page = await _library.ListAsync(_shop, null, null, 1, 24);

        Assert.Equal(2, page.Total);
        Assert.True(page.Items[0].InUse);
        Assert.False(page.Items[1].InUse);
        Assert.StartsWith($"/api/media/{used.Id}/download?expires=", page.Items[0].Url);
        Assert.EndsWith("&sig=SIG", page.Items[0].Url);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-5, 1000, 100)]
    public async Task List_KeepsThePageAndSizeInRange(int page, int size, int expectedSize)
    {
        _files.Setup(f => f.GetActivePageAsync(_shop, null, null, 1, expectedSize)).ReturnsAsync((new List<MediaFile>(), 0));

        var result = await _library.ListAsync(_shop, null, null, page, size);

        Assert.Equal(1, result.Page);
        Assert.Equal(expectedSize, result.PageSize);
    }

    // ----- usage

    [Fact]
    public async Task Usage_ReportsWhatIsStoredAndThePlansAllowance()
    {
        _files.Setup(f => f.GetUsageAsync(_shop)).ReturnsAsync(new MediaUsage(5_000, 3, 2, 1));
        _subscriptions.Setup(s => s.GetByShopIdAsync(_shop)).ReturnsAsync(new Subscription
        {
            Plan = new SubscriptionPlan { Features = new SubscriptionFeatures { MaxStorageGB = 10 } }
        });

        var usage = await _library.GetUsageAsync(_shop);

        Assert.Equal(5_000, usage.UsedBytes);
        Assert.Equal((3, 2, 1), (usage.FileCount, usage.ImageCount, usage.VideoCount));
        Assert.Equal(10L * 1024 * 1024 * 1024, usage.LimitBytes);
    }

    [Fact]
    public async Task Usage_WithoutAPlan_HasNoLimit()
    {
        _files.Setup(f => f.GetUsageAsync(_shop)).ReturnsAsync(new MediaUsage(0, 0, 0, 0));

        Assert.Null((await _library.GetUsageAsync(_shop)).LimitBytes);
    }

    // ----- deleting

    [Fact]
    public async Task Delete_RemovesTheBytesAndMarksTheRecord()
    {
        var file = AFile();
        _files.Setup(f => f.GetByIdAsync(file.Id, _shop)).ReturnsAsync(file);
        _files.Setup(f => f.FindUsingBannersAsync(_shop, file.Id)).ReturnsAsync(new List<string>());

        var result = await _library.DeleteAsync(_shop, file.Id);

        Assert.Equal(MediaDeleteOutcome.Deleted, result.Outcome);
        Assert.Equal((int)MediaFileStatus.Deleted, file.Status);
        Assert.NotNull(file.DeletedAt);
        _storage.Verify(s => s.DeleteChunkAsync(file.StoragePath), Times.Once);
        _files.Verify(f => f.UpdateAsync(file), Times.Once);
    }

    [Fact]
    public async Task Delete_IsRefusedWhileABannerUsesTheFile_AndNamesTheBanners()
    {
        var file = AFile("hero.png");
        _files.Setup(f => f.GetByIdAsync(file.Id, _shop)).ReturnsAsync(file);
        _files.Setup(f => f.FindUsingBannersAsync(_shop, file.Id)).ReturnsAsync(new List<string> { "Summer Sale", "Winter Sale" });

        var result = await _library.DeleteAsync(_shop, file.Id);

        Assert.Equal(MediaDeleteOutcome.InUse, result.Outcome);
        Assert.Contains("Summer Sale, Winter Sale", result.Message);
        Assert.Equal(2, result.Banners.Count);
        Assert.Equal((int)MediaFileStatus.Active, file.Status);
        _storage.Verify(s => s.DeleteChunkAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_LongListOfBanners_IsShortenedInTheMessage()
    {
        var file = AFile();
        _files.Setup(f => f.GetByIdAsync(file.Id, _shop)).ReturnsAsync(file);
        _files.Setup(f => f.FindUsingBannersAsync(_shop, file.Id)).ReturnsAsync(Enumerable.Range(1, 8).Select(i => $"B{i}").ToList());

        var result = await _library.DeleteAsync(_shop, file.Id);

        Assert.Contains("and 3 more", result.Message);
    }

    [Fact]
    public async Task Delete_OfAnotherShopsOrAnAlreadyDeletedFile_IsNotFound()
    {
        var gone = AFile(status: (int)MediaFileStatus.Deleted);
        _files.Setup(f => f.GetByIdAsync(gone.Id, _shop)).ReturnsAsync(gone);

        Assert.Equal(MediaDeleteOutcome.NotFound, (await _library.DeleteAsync(_shop, Guid.NewGuid())).Outcome);
        Assert.Equal(MediaDeleteOutcome.NotFound, (await _library.DeleteAsync(_shop, gone.Id)).Outcome);
    }

    [Fact]
    public async Task Delete_WhileABrowserStillStreamsTheFile_StillSucceeds_AndTheCleanupRemovesTheBytesLater()
    {
        var file = AFile();
        _files.Setup(f => f.GetByIdAsync(file.Id, _shop)).ReturnsAsync(file);
        _files.Setup(f => f.FindUsingBannersAsync(_shop, file.Id)).ReturnsAsync(new List<string>());
        _storage.Setup(s => s.DeleteChunkAsync(file.StoragePath)).ThrowsAsync(new IOException("in use"));

        var result = await _library.DeleteAsync(_shop, file.Id);

        Assert.Equal(MediaDeleteOutcome.Deleted, result.Outcome);
        Assert.Equal((int)MediaFileStatus.Deleted, file.Status);
    }

    [Fact]
    public async Task Delete_OfAnUnfinishedUpload_AlsoRemovesItsPieces()
    {
        var file = AFile(status: (int)MediaFileStatus.Pending);
        var piece = new UploadChunk(file.Id, 0, 10, new string('a', 32));
        _files.Setup(f => f.GetByIdAsync(file.Id, _shop)).ReturnsAsync(file);
        _files.Setup(f => f.FindUsingBannersAsync(_shop, file.Id)).ReturnsAsync(new List<string>());
        _chunks.Setup(c => c.GetChunksByMediaFileAsync(file.Id)).ReturnsAsync(new List<UploadChunk> { piece });

        await _library.DeleteAsync(_shop, file.Id);

        _storage.Verify(s => s.DeleteChunkAsync(piece.StoragePath), Times.Once);
        _chunks.Verify(c => c.DeleteAllForFileAsync(file.Id), Times.Once);
    }
}

public class MediaCleanupServiceTests
{
    private readonly Mock<IMediaFileRepository> _files = new();
    private readonly Mock<IUploadChunkRepository> _chunks = new();
    private readonly Mock<IStorageProvider> _storage = new();
    private readonly MediaCleanupService _cleanup;
    private static readonly DateTime Now = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

    public MediaCleanupServiceTests()
    {
        _chunks.Setup(c => c.GetChunksByMediaFileAsync(It.IsAny<Guid>())).ReturnsAsync(new List<UploadChunk>());
        _files.Setup(f => f.GetAbandonedAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<MediaFile>());
        _files.Setup(f => f.GetTombstonesAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<MediaFile>());
        _cleanup = new MediaCleanupService(_files.Object, _chunks.Object, _storage.Object, new ConfigurationBuilder().Build(), NullLogger<MediaCleanupService>.Instance);
    }

    [Fact]
    public async Task UnfinishedUploadsOlderThanADay_LoseTheirBytesAndAreMarkedDeleted()
    {
        var file = new MediaFile(Guid.NewGuid(), "a.png", "image/png", 10, 1, Guid.NewGuid());
        var piece = new UploadChunk(file.Id, 0, 10, new string('b', 32));
        _files.Setup(f => f.GetAbandonedAsync(Now.AddHours(-24))).ReturnsAsync(new List<MediaFile> { file });
        _chunks.Setup(c => c.GetChunksByMediaFileAsync(file.Id)).ReturnsAsync(new List<UploadChunk> { piece });

        var report = await _cleanup.RunAsync(Now);

        Assert.Equal(1, report.AbandonedRemoved);
        Assert.Equal((int)MediaFileStatus.Deleted, file.Status);
        _storage.Verify(s => s.DeleteChunkAsync(piece.StoragePath), Times.Once);
        _storage.Verify(s => s.DeleteChunkAsync(file.StoragePath), Times.Once);
        _chunks.Verify(c => c.DeleteAllForFileAsync(file.Id), Times.Once);
    }

    [Fact]
    public async Task OldRecordsOfDeletedFiles_AreDroppedAfterThirtyDays()
    {
        var file = new MediaFile(Guid.NewGuid(), "a.png", "image/png", 10, 1, Guid.NewGuid());
        file.MarkAsDeleted();
        _files.Setup(f => f.GetTombstonesAsync(Now.AddDays(-30))).ReturnsAsync(new List<MediaFile> { file });

        var report = await _cleanup.RunAsync(Now);

        Assert.Equal(1, report.RecordsDropped);
        _files.Verify(f => f.RemoveAsync(file), Times.Once);
    }

    [Fact]
    public async Task ARecordWhoseBytesAreStillInUse_IsKeptForTheNextRun()
    {
        var file = new MediaFile(Guid.NewGuid(), "a.png", "image/png", 10, 1, Guid.NewGuid());
        file.MarkAsDeleted();
        _files.Setup(f => f.GetTombstonesAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<MediaFile> { file });
        _storage.Setup(s => s.DeleteChunkAsync(file.StoragePath)).ThrowsAsync(new IOException("in use"));

        var report = await _cleanup.RunAsync(Now);

        Assert.Equal(0, report.RecordsDropped);
        _files.Verify(f => f.RemoveAsync(It.IsAny<MediaFile>()), Times.Never);
    }

    [Fact]
    public async Task WithNothingToDo_ChangesNothing()
    {
        var report = await _cleanup.RunAsync(Now);

        Assert.Equal(new MediaCleanupReport(0, 0), report);
        _files.Verify(f => f.UpdateAsync(It.IsAny<MediaFile>()), Times.Never);
        _files.Verify(f => f.RemoveAsync(It.IsAny<MediaFile>()), Times.Never);
    }

    [Fact]
    public async Task TheLimitsCanBeChangedInSettings()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Media:Cleanup:AbandonedUploadHours"] = "2",
            ["Media:Cleanup:KeepDeletedRecordsDays"] = "7",
        }).Build();
        var cleanup = new MediaCleanupService(_files.Object, _chunks.Object, _storage.Object, settings, NullLogger<MediaCleanupService>.Instance);

        await cleanup.RunAsync(Now);

        _files.Verify(f => f.GetAbandonedAsync(Now.AddHours(-2)), Times.Once);
        _files.Verify(f => f.GetTombstonesAsync(Now.AddDays(-7)), Times.Once);
    }
}
