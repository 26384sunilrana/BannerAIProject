namespace BannerService.Domain.Tests.Services;

using Xunit;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.ValueObjects;
using BannerService.Application.Services;
using Moq;

public class VersionControlServiceTests
{
    private readonly Mock<IBannerVersionRepository> _mockVersionRepository = new();
    private readonly Mock<IBannerRepository> _mockBannerRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IPublishWorkflowRepository> _mockWorkflowRepository = new();
    private readonly VersionControlService _service;

    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public VersionControlServiceTests()
    {
        _service = new VersionControlService(
            _mockVersionRepository.Object,
            _mockBannerRepository.Object,
            _mockUnitOfWork.Object,
            _mockWorkflowRepository.Object);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithValidBanner_ShouldCreateVersion()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test Banner", "Test Description", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        banner.AddComponent(component);

        _mockVersionRepository.Setup(r => r.GetNextVersionNumberAsync(banner.Id, _shopId))
            .ReturnsAsync(1);
        _mockVersionRepository.Setup(r => r.SaveAsync(It.IsAny<BannerVersion>()))
            .ReturnsAsync((BannerVersion v) => v);

        // Act
        var result = await _service.CreateSnapshotAsync(banner, "Initial snapshot", _userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.VersionNumber);
        Assert.Equal(banner.Id, result.BannerId);
        Assert.Equal(_shopId, result.ShopId);
        Assert.Equal("Initial snapshot", result.ChangeDescription);
        Assert.NotNull(result.Snapshot);
        Assert.Equal(banner.Name, result.Snapshot.Name);
        Assert.Single(result.Snapshot.Components);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithNullBanner_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateSnapshotAsync(null!, null, _userId));
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithLongChangeDescription_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var longDescription = new string('x', 501);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateSnapshotAsync(banner, longDescription, _userId));
    }

    [Fact]
    public async Task ListVersionsAsync_WithValidBannerId_ShouldReturnVersions()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var snapshot = new BannerSnapshot(banner);
        var version = new BannerVersion(banner.Id, _shopId, 1, snapshot, _userId, "Test");

        _mockVersionRepository.Setup(r => r.GetVersionsAsync(banner.Id, _shopId))
            .ReturnsAsync(new List<BannerVersion> { version });

        // Act
        var result = await _service.ListVersionsAsync(banner.Id, _shopId);

        // Assert
        Assert.NotEmpty(result);
        Assert.Single(result);
        Assert.Equal(1, result[0].VersionNumber);
        Assert.Equal("Test", result[0].ChangeDescription);
    }

    [Fact]
    public async Task GetVersionDetailAsync_WithValidVersion_ShouldReturnDetail()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        banner.AddComponent(component);

        var snapshot = new BannerSnapshot(banner);
        var version = new BannerVersion(banner.Id, _shopId, 1, snapshot, _userId, "Test");

        _mockVersionRepository.Setup(r => r.GetVersionAsync(banner.Id, 1, _shopId))
            .ReturnsAsync(version);

        // Act
        var result = await _service.GetVersionDetailAsync(banner.Id, 1, _shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.VersionNumber);
        Assert.Equal("Test", result.ChangeDescription);
        Assert.Single(result.Components);
    }

    [Fact]
    public async Task RestoreVersionAsync_WithValidVersion_ShouldRestoreBanner()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Original", "Original Desc", 1200, 600);
        var originalComponent = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        banner.AddComponent(originalComponent);

        // Create a snapshot
        var snapshot = new BannerSnapshot(banner);
        var version = new BannerVersion(banner.Id, _shopId, 1, snapshot, _userId, "Initial");

        _mockVersionRepository.Setup(r => r.GetVersionAsync(banner.Id, 1, _shopId))
            .ReturnsAsync(version);
        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);
        _mockVersionRepository.Setup(r => r.GetNextVersionNumberAsync(banner.Id, _shopId))
            .ReturnsAsync(2);
        _mockVersionRepository.Setup(r => r.SaveAsync(It.IsAny<BannerVersion>()))
            .ReturnsAsync((BannerVersion v) => v);
        _mockBannerRepository.Setup(r => r.UpdateAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RestoreVersionAsync(banner.Id, 1, _shopId, _userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Original", result.Name);
        Assert.Equal("Original Desc", result.Description);
        Assert.NotEmpty(result.Components);

        _mockBannerRepository.Verify(r => r.UpdateAsync(It.IsAny<Banner>()), Times.Once);
        _mockVersionRepository.Verify(r => r.SaveAsync(It.IsAny<BannerVersion>()), Times.Once);
    }

    [Fact]
    public async Task RestoreVersionAsync_WithInvalidVersionNumber_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RestoreVersionAsync(Guid.NewGuid(), 0, _shopId, _userId));
    }

    [Fact]
    public async Task RestoreVersionAsync_WithNonExistentVersion_ShouldThrow()
    {
        // Arrange
        _mockVersionRepository.Setup(r => r.GetVersionAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Guid>()))
            .ReturnsAsync((BannerVersion?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.RestoreVersionAsync(Guid.NewGuid(), 1, _shopId, _userId));
    }

    [Fact]
    public async Task CreateSnapshotAsync_ShouldIncludeAllComponents()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        for (int i = 0; i < 5; i++)
        {
            var component = new Component(banner.Id, ComponentType.Text,
                new Position(i * 10, 0), new Size(100, 50), i, "{}");
            banner.AddComponent(component);
        }

        _mockVersionRepository.Setup(r => r.GetNextVersionNumberAsync(banner.Id, _shopId))
            .ReturnsAsync(1);
        _mockVersionRepository.Setup(r => r.SaveAsync(It.IsAny<BannerVersion>()))
            .ReturnsAsync((BannerVersion v) => v);

        // Act
        var result = await _service.CreateSnapshotAsync(banner, null, _userId);

        // Assert
        Assert.Equal(5, result.Snapshot.Components.Count);
    }
}
