namespace BannerService.Domain.Tests.Services;

using Xunit;
using Moq;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using BannerService.Domain.ValueObjects;
using BannerService.Application.Services;

public class CarouselServiceTests
{
    private readonly Mock<IBannerRepository> _mockBannerRepository = new();
    private readonly Mock<ICarouselRepository> _mockCarouselRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly EffectValidator _validator = new();
    private readonly CarouselService _service;

    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public CarouselServiceTests()
    {
        _service = new CarouselService(
            _mockBannerRepository.Object,
            _mockCarouselRepository.Object,
            _validator,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task CreateCarouselAsync_WithValidConfig_ShouldCreateCarousel()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var image1 = new Component(banner.Id, ComponentType.Image, new Position(0, 0), new Size(600, 600), 1, "{}");
        var image2 = new Component(banner.Id, ComponentType.Image, new Position(600, 0), new Size(600, 600), 2, "{}");
        banner.AddComponent(image1);
        banner.AddComponent(image2);

        var componentIds = new List<Guid> { image1.Id, image2.Id };

        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);
        _mockCarouselRepository.Setup(r => r.SaveAsync(It.IsAny<Carousel>(), componentIds))
            .ReturnsAsync((Carousel c, List<Guid> _) => c);
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateCarouselAsync(
            banner.Id, _shopId, 5000, 1000, 1, componentIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000, result.IntervalMs);
        Assert.Equal(1000, result.TransitionDuration);
        Assert.Equal(1, result.TransitionType);
        Assert.Equal(2, result.ComponentIds.Count);
    }

    [Fact]
    public async Task CreateCarouselAsync_WithSingleComponent_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var componentIds = new List<Guid> { Guid.NewGuid() };

        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateCarouselAsync(
                banner.Id, _shopId, 5000, 1000, 1, componentIds));
    }

    [Fact]
    public async Task CreateCarouselAsync_WithInvalidIntervalMs_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId)).ReturnsAsync(banner);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateCarouselAsync(
                banner.Id, _shopId, 999, 1000, 1, new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }));
    }

    [Fact]
    public async Task GetCarouselAsync_WithValidId_ShouldReturnCarousel()
    {
        // Arrange
        var carousel = new Carousel(Guid.NewGuid(), 5000, 1000, 1, new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });
        var componentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _mockCarouselRepository.Setup(r => r.GetByIdAsync(carousel.Id, _shopId))
            .ReturnsAsync(carousel);
        _mockCarouselRepository.Setup(r => r.GetCarouselComponentIdsAsync(carousel.Id))
            .ReturnsAsync(componentIds);

        // Act
        var result = await _service.GetCarouselAsync(carousel.Id, _shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(carousel.Id, result.Id);
        Assert.Equal(2, result.ComponentIds.Count);
    }

    [Fact]
    public async Task GetCarouselAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        _mockCarouselRepository.Setup(r => r.GetByIdAsync(Guid.NewGuid(), _shopId))
            .ReturnsAsync((Carousel?)null);

        // Act
        var result = await _service.GetCarouselAsync(Guid.NewGuid(), _shopId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteCarouselAsync_ShouldRemoveCarousel()
    {
        // Arrange
        var carouselId = Guid.NewGuid();

        _mockCarouselRepository.Setup(r => r.DeleteAsync(carouselId, _shopId))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteCarouselAsync(carouselId, _shopId);

        // Assert
        _mockCarouselRepository.Verify(r => r.DeleteAsync(carouselId, _shopId), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCarouselAsync_WithValidNewComponents_ShouldUpdate()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var image1 = new Component(banner.Id, ComponentType.Image, new Position(0, 0), new Size(600, 600), 1, "{}");
        var image2 = new Component(banner.Id, ComponentType.Image, new Position(600, 0), new Size(600, 600), 2, "{}");
        banner.AddComponent(image1);
        banner.AddComponent(image2);

        var oldCarousel = new Carousel(banner.Id, 5000, 1000, 1, new List<Guid> { image1.Id, image2.Id });
        var newComponentIds = new List<Guid> { image1.Id, image2.Id };

        _mockCarouselRepository.Setup(r => r.GetByIdAsync(oldCarousel.Id, _shopId))
            .ReturnsAsync(oldCarousel);
        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);
        _mockCarouselRepository.Setup(r => r.UpdateAsync(It.IsAny<Carousel>(), newComponentIds))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateCarouselAsync(
            oldCarousel.Id, _shopId, 6000, 1200, 2, newComponentIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6000, result.IntervalMs);
        Assert.Equal(1200, result.TransitionDuration);
        Assert.Equal(2, result.ComponentIds.Count);
    }
}
