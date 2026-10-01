using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.ValueObjects;
using Moq;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class VersionRestoreApprovalTests
{
    private readonly Mock<IBannerVersionRepository> _versions = new();
    private readonly Mock<IBannerRepository> _banners = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPublishWorkflowRepository> _workflows = new();
    private readonly VersionControlService _service;

    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Banner _banner;
    private readonly List<BannerVersion> _stored = new();

    public VersionRestoreApprovalTests()
    {
        _banner = new Banner(_shopId, _userId, "Original", "Desc", 1200, 600);
        _banner.AddComponent(new Component(_banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}"));

        _versions.Setup(r => r.SaveAsync(It.IsAny<BannerVersion>())).ReturnsAsync((BannerVersion v) => { _stored.Add(v); return v; });
        _versions.Setup(r => r.GetVersionsAsync(_banner.Id, _shopId))
            .ReturnsAsync(() => _stored.OrderByDescending(v => v.VersionNumber).ToList());
        _versions.Setup(r => r.GetVersionAsync(_banner.Id, It.IsAny<int>(), _shopId))
            .ReturnsAsync((Guid _, int n, Guid _) => _stored.FirstOrDefault(v => v.VersionNumber == n));
        _versions.Setup(r => r.GetNextVersionNumberAsync(_banner.Id, _shopId))
            .ReturnsAsync(() => _stored.Count == 0 ? 1 : _stored.Max(v => v.VersionNumber) + 1);
        _banners.Setup(r => r.GetByIdAsync(_banner.Id, _shopId)).ReturnsAsync(_banner);
        _banners.Setup(r => r.UpdateAsync(It.IsAny<Banner>())).Returns(Task.FromResult(_banner));
        _unitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        _service = new VersionControlService(_versions.Object, _banners.Object, _unitOfWork.Object, _workflows.Object);
    }

    private Task<BannerVersion> Snapshot(string? note = null) => _service.CreateSnapshotAsync(_banner, note, _userId);

    [Fact]
    public async Task CreateSnapshot_AsksRepositoryToKeepOnlyTenVersions()
    {
        await Snapshot();

        _versions.Verify(r => r.DeactivateOldVersionsAsync(_banner.Id, _shopId, 10), Times.Once);
    }

    [Fact]
    public async Task Restore_WithNoExistingWorkflow_CreatesPendingApproval()
    {
        await Snapshot("v1");
        _banner.UpdateName("Edited");
        _banner.Publish();

        await _service.RestoreVersionAsync(_banner.Id, 1, _shopId, _userId, "Olive");

        Assert.False(_banner.IsPublished);
        _workflows.Verify(r => r.CreateAsync(It.Is<PublishWorkflow>(w =>
            w.BannerId == _banner.Id && w.Status == PublishStatus.PendingApproval)), Times.Once);
    }

    [Fact]
    public async Task Restore_WithPublishedWorkflow_ResubmitsItForApproval()
    {
        await Snapshot("v1");
        var workflow = new PublishWorkflow { BannerId = _banner.Id, ShopId = _shopId, Status = PublishStatus.Published };
        _workflows.Setup(r => r.GetByBannerIdAsync(_banner.Id)).ReturnsAsync(workflow);

        await _service.RestoreVersionAsync(_banner.Id, 1, _shopId, _userId, "Olive");

        Assert.Equal(PublishStatus.PendingApproval, workflow.Status);
        _workflows.Verify(r => r.UpdateAsync(workflow), Times.Once);
        Assert.Contains(workflow.Events, e => e.Comment == "Restored to version 1");
    }

    [Fact]
    public async Task Restore_PreservesTheUnsavedLatestStateAsItsOwnVersion()
    {
        await Snapshot("v1");
        _banner.UpdateName("Edited after v1");

        await _service.RestoreVersionAsync(_banner.Id, 1, _shopId, _userId, "Olive");

        // v1, the preserved "Edited after v1" state, and the restoration itself become the new latest
        Assert.Equal(3, _stored.Count);
        Assert.Equal("Edited after v1", _stored[1].Snapshot.Name);
        Assert.Equal("Original", _stored[2].Snapshot.Name);
        Assert.Equal("Restored to version 1", _stored[2].ChangeDescription);
    }

    [Fact]
    public async Task Restore_WhenNothingChangedSinceLatestVersion_DoesNotAddExtraVersion()
    {
        await Snapshot("v1");

        await _service.RestoreVersionAsync(_banner.Id, 1, _shopId, _userId, "Olive");

        Assert.Equal(2, _stored.Count);
    }

    [Fact]
    public void ContentEquals_IgnoresCapturedAtAndComponentIds()
    {
        var first = new BannerSnapshot(_banner);
        var second = new BannerSnapshot(_banner) { CapturedAt = DateTime.UtcNow.AddHours(1) };
        second.Components[0].ComponentId = Guid.NewGuid();

        Assert.True(first.ContentEquals(second));

        second.Components[0].PositionX = 5;
        Assert.False(first.ContentEquals(second));
    }
}
