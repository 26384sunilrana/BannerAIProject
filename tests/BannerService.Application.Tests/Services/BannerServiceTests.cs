namespace BannerService.Application.Tests.Services;

using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using BannerService.Application.Services;
using BannerService.Application.Dto;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;

public class BannerServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IBannerRepository> _mockBannerRepository;
    private readonly Mock<ILogger<BannerService>> _mockLogger;
    private readonly BannerService _service;
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public BannerServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBannerRepository = new Mock<IBannerRepository>();
        _mockLogger = new Mock<ILogger<BannerService>>();

        _mockUnitOfWork.Setup(u => u.BannerRepository).Returns(_mockBannerRepository.Object);

        _service = new BannerService(
            _mockUnitOfWork.Object,
            new ComponentValidationService(),
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateBannerAsync_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var request = new CreateBannerRequestDto
        {
            Name = "Test Banner",
            Description = "A test banner",
            Width = 1200,
            Height = 600
        };

        _mockBannerRepository
            .Setup(r => r.CreateAsync(It.IsAny<Banner>()))
            .ReturnsAsync((Banner b) => b);

        // Act
        var result = await _service.CreateBannerAsync(_shopId, _userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Banner", result.Name);
        Assert.Equal(_shopId, result.ShopId);
        _mockBannerRepository.Verify(r => r.CreateAsync(It.IsAny<Banner>()), Times.Once);
    }

    [Fact]
    public async Task CreateBannerAsync_WithInvalidName_ShouldThrow()
    {
        // Arrange
        var request = new CreateBannerRequestDto
        {
            Name = "",
            Description = "A test banner",
            Width = 1200,
            Height = 600
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateBannerAsync(_shopId, _userId, request));
    }

    [Fact]
    public async Task GetBannerAsync_WithValidId_ShouldReturnBanner()
    {
        // Arrange
        var bannerId = Guid.NewGuid();
        var banner = new Banner(_shopId, _userId, "Test", "Description", 1200, 600) { Id = bannerId };

        _mockBannerRepository
            .Setup(r => r.GetByIdAsync(bannerId, _shopId))
            .ReturnsAsync(banner);

        // Act
        var result = await _service.GetBannerAsync(bannerId, _shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bannerId, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetBannerAsync_WithInvalidId_ShouldThrow()
    {
        // Arrange
        _mockBannerRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _shopId))
            .ReturnsAsync((Banner?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetBannerAsync(Guid.NewGuid(), _shopId));
    }

    [Fact]
    public async Task ListBannersAsync_ShouldReturnPaginatedList()
    {
        // Arrange
        var banners = new List<Banner>
        {
            new Banner(_shopId, _userId, "Banner 1", "Desc", 1200, 600),
            new Banner(_shopId, _userId, "Banner 2", "Desc", 1200, 600)
        };

        _mockBannerRepository
            .Setup(r => r.GetAllByShopAsync(_shopId, 1, 20))
            .ReturnsAsync(banners);

        // Act
        var result = await _service.ListBannersAsync(_shopId, 1, 20);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(_shopId, b.ShopId));
    }

    [Fact]
    public async Task DeleteBannerAsync_WithPublishedBanner_ShouldThrow()
    {
        // Arrange
        var bannerId = Guid.NewGuid();
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600) { Id = bannerId };
        banner.Publish();

        _mockBannerRepository
            .Setup(r => r.GetByIdAsync(bannerId, _shopId))
            .ReturnsAsync(banner);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteBannerAsync(bannerId, _shopId));
    }

    [Fact]
    public async Task AddComponentAsync_WithValidRequest_ShouldSucceed()
    {
        // Arrange
        var bannerId = Guid.NewGuid();
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600) { Id = bannerId };

        var request = new AddComponentRequestDto
        {
            ComponentType = 1,
            PositionX = 10,
            PositionY = 20,
            SizeWidth = 100,
            SizeHeight = 50,
            ZIndex = 1,
            Properties = new Dictionary<string, object> { { "content", "Hello" } }
        };

        _mockBannerRepository
            .Setup(r => r.GetByIdAsync(bannerId, _shopId))
            .ReturnsAsync(banner);

        _mockBannerRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Banner>()))
            .ReturnsAsync((Banner b) => b);

        // Act
        var result = await _service.AddComponentAsync(bannerId, _shopId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ComponentType);
        Assert.Equal(1, result.ZIndex);
    }
}
