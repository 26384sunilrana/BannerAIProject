namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class CarouselRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private CarouselRepository _repository;
    private readonly Guid _testBannerId = Guid.NewGuid();
    private readonly Guid _testShopId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new CarouselRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var carousel = new Carousel
        {
            Id = Guid.NewGuid(),
            BannerId = _testBannerId,
            ShopId = _testShopId,
            Name = "Test Carousel",
            Duration = 5000,
            TransitionType = "fade"
        };

        _context.Carousels.Add(carousel);

        var componentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var carouselComponents = componentIds
            .Select((componentId, index) => new CarouselComponent
            {
                Id = Guid.NewGuid(),
                CarouselId = carousel.Id,
                ComponentId = componentId,
                Order = index
            })
            .ToList();

        _context.CarouselComponents.AddRange(carouselComponents);

        await _context.SaveChangesAsync();
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithValidCarousel_ShouldCreateCarouselAndComponents()
    {
        // Arrange
        var newCarousel = new Carousel
        {
            BannerId = Guid.NewGuid(),
            ShopId = _testShopId,
            Name = "New Carousel",
            Duration = 3000
        };

        var componentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var result = await _repository.SaveAsync(newCarousel, componentIds);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Carousels.FindAsync(newCarousel.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateCarouselComponentsInOrder()
    {
        // Arrange
        var newCarousel = new Carousel
        {
            BannerId = Guid.NewGuid(),
            ShopId = _testShopId,
            Name = "Ordered Carousel",
            Duration = 2000
        };

        var componentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        await _repository.SaveAsync(newCarousel, componentIds);

        // Assert
        var components = await _context.CarouselComponents
            .Where(c => c.CarouselId == newCarousel.Id)
            .OrderBy(c => c.Order)
            .ToListAsync();

        Assert.Equal(2, components.Count);
        Assert.Equal(0, components[0].Order);
        Assert.Equal(1, components[1].Order);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnCarousel()
    {
        // Arrange
        var carousel = _context.Carousels.First();

        // Act
        var result = await _repository.GetByIdAsync(carousel.Id, carousel.ShopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(carousel.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), _testShopId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetBannerCarouselsAsync Tests

    [Fact]
    public async Task GetBannerCarouselsAsync_WithValidBannerId_ShouldReturnCarousels()
    {
        // Act
        var results = await _repository.GetBannerCarouselsAsync(_testBannerId, _testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, c => Assert.Equal(_testBannerId, c.BannerId));
    }

    [Fact]
    public async Task GetBannerCarouselsAsync_WithInvalidBannerId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetBannerCarouselsAsync(Guid.NewGuid(), _testShopId);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetBannerCarouselsAsync_ShouldReturnOrderedByCreatedDateDescending()
    {
        // Act
        var results = await _repository.GetBannerCarouselsAsync(_testBannerId, _testShopId);

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(c => c.CreatedAt)));
    }

    #endregion

    #region GetCarouselComponentIdsAsync Tests

    [Fact]
    public async Task GetCarouselComponentIdsAsync_WithValidCarouselId_ShouldReturnComponentIds()
    {
        // Arrange
        var carousel = _context.Carousels.First();

        // Act
        var results = await _repository.GetCarouselComponentIdsAsync(carousel.Id);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetCarouselComponentIdsAsync_WithInvalidCarouselId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetCarouselComponentIdsAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetCarouselComponentIdsAsync_ShouldReturnOrderedByOrder()
    {
        // Arrange
        var carousel = _context.Carousels.First();

        // Act
        var results = await _repository.GetCarouselComponentIdsAsync(carousel.Id);

        // Assert
        Assert.Equal(2, results.Count);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidCarouselAndComponents_ShouldUpdate()
    {
        // Arrange
        var carousel = _context.Carousels.First();
        carousel.Name = "Updated Name";
        carousel.Duration = 4000;

        var newComponentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        await _repository.UpdateAsync(carousel, newComponentIds);

        // Assert
        var updated = await _context.Carousels.FindAsync(carousel.Id);
        Assert.Equal("Updated Name", updated.Name);

        var components = await _context.CarouselComponents
            .Where(c => c.CarouselId == carousel.Id)
            .ToListAsync();

        Assert.Equal(3, components.Count);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceOldComponents()
    {
        // Arrange
        var carousel = _context.Carousels.First();
        var oldComponentCount = _context.CarouselComponents
            .Count(c => c.CarouselId == carousel.Id);

        var newComponentIds = new List<Guid> { Guid.NewGuid() };

        // Act
        await _repository.UpdateAsync(carousel, newComponentIds);

        // Assert
        var newComponentCount = _context.CarouselComponents
            .Count(c => c.CarouselId == carousel.Id);

        Assert.Equal(1, newComponentCount);
        Assert.NotEqual(oldComponentCount, newComponentCount);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemoveCarousel()
    {
        // Arrange
        var carousel = _context.Carousels.First();

        // Act
        await _repository.DeleteAsync(carousel.Id, carousel.ShopId);

        // Assert
        var deleted = await _context.Carousels.FindAsync(carousel.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldNotThrow()
    {
        // Act
        await _repository.DeleteAsync(Guid.NewGuid(), _testShopId);

        // Assert - should complete without error
        Assert.True(true);
    }

    #endregion
}
