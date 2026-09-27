namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class BannerRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private Mock<IShopContextAccessor> _mockShopContext;
    private BannerRepository _repository;
    private readonly Guid _testShopId = Guid.NewGuid();
    private readonly Guid _otherShopId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _mockShopContext = new Mock<IShopContextAccessor>();
        _mockShopContext.Setup(s => s.ShopId).Returns(_testShopId);
        _repository = new BannerRepository(_context, _mockShopContext.Object);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var banner1 = new Banner(_testShopId, Guid.NewGuid(), "Banner 1", "Description 1", 1920, 1080);
        var banner2 = new Banner(_testShopId, Guid.NewGuid(), "Banner 2", "Description 2", 1280, 720);
        var otherShopBanner = new Banner(_otherShopId, Guid.NewGuid(), "Other Banner", "Description", 800, 600);

        _context.Banners.AddRange(banner1, banner2, otherShopBanner);
        await _context.SaveChangesAsync();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidBanner_ShouldAddToDatabase()
    {
        // Arrange
        var newBanner = new Banner(_testShopId, Guid.NewGuid(), "New Banner", "New Description", 1920, 1080);

        // Act
        var result = await _repository.CreateAsync(newBanner);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.Banners.FindAsync(newBanner.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateAsync_WithDifferentShop_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var bannerFromOtherShop = new Banner(_otherShopId, Guid.NewGuid(), "Other Banner", "Desc", 800, 600);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.CreateAsync(bannerFromOtherShop)
        );
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnBanner()
    {
        // Arrange
        var banner = _context.Banners.First(b => b.ShopId == _testShopId);

        // Act
        var result = await _repository.GetByIdAsync(banner.Id, _testShopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(banner.Id, result.Id);
        Assert.Equal(banner.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), _testShopId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentShop_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var banner = _context.Banners.First(b => b.ShopId == _testShopId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.GetByIdAsync(banner.Id, _otherShopId)
        );
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeComponents()
    {
        // Arrange
        var banner = _context.Banners.First(b => b.ShopId == _testShopId);

        // Act
        var result = await _repository.GetByIdAsync(banner.Id, _testShopId);

        // Assert
        Assert.NotNull(result);
        // Components should be loaded (even if empty)
        Assert.NotNull(result.Components);
    }

    #endregion

    #region GetAllByShopAsync Tests

    [Fact]
    public async Task GetAllByShopAsync_WithValidShop_ShouldReturnBanners()
    {
        // Act
        var results = await _repository.GetAllByShopAsync(_testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(2, results.Count);
        Assert.All(results, b => Assert.Equal(_testShopId, b.ShopId));
    }

    [Fact]
    public async Task GetAllByShopAsync_WithPagination_ShouldReturnPagedResults()
    {
        // Act
        var page1 = await _repository.GetAllByShopAsync(_testShopId, page: 1, pageSize: 1);
        var page2 = await _repository.GetAllByShopAsync(_testShopId, page: 2, pageSize: 1);

        // Assert
        Assert.Single(page1);
        Assert.Single(page2);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task GetAllByShopAsync_ShouldOrderByCreationDate()
    {
        // Act
        var results = await _repository.GetAllByShopAsync(_testShopId);

        // Assert
        Assert.True(results[0].CreatedAt >= results[1].CreatedAt);
    }

    [Fact]
    public async Task GetAllByShopAsync_WithDifferentShop_ShouldThrowUnauthorizedAccessException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.GetAllByShopAsync(_otherShopId)
        );
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidBanner_ShouldUpdateDatabase()
    {
        // Arrange
        var banner = _context.Banners.First(b => b.ShopId == _testShopId);
        banner.Name = "Updated Name";

        // Act
        await _repository.UpdateAsync(banner);

        // Assert
        var updated = await _context.Banners.FindAsync(banner.Id);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithDifferentShop_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var bannerFromOtherShop = new Banner(_otherShopId, Guid.NewGuid(), "Other", "Desc", 800, 600);
        bannerFromOtherShop.Name = "Updated";

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.UpdateAsync(bannerFromOtherShop)
        );
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemoveFromDatabase()
    {
        // Arrange
        var banner = _context.Banners.First(b => b.ShopId == _testShopId);

        // Act
        var result = await _repository.DeleteAsync(banner.Id, _testShopId);

        // Assert
        Assert.True(result);
        var deleted = await _context.Banners.FindAsync(banner.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid(), _testShopId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithDifferentShop_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var banner = _context.Banners.First(b => b.ShopId == _testShopId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.DeleteAsync(banner.Id, _otherShopId)
        );
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithValidBanner_ShouldReturnTrue()
    {
        // Arrange
        var banner = _context.Banners.First(b => b.ShopId == _testShopId);

        // Act
        var result = await _repository.ExistsAsync(banner.Id, _testShopId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid(), _testShopId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WithDifferentShop_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var banner = _context.Banners.First(b => b.ShopId == _testShopId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.ExistsAsync(banner.Id, _otherShopId)
        );
    }

    #endregion

    #region Shop Context Isolation Tests

    [Fact]
    public async Task Repository_ShouldOnlyReturnBannersFromAuthorizedShop()
    {
        // Act
        var results = await _repository.GetAllByShopAsync(_testShopId);

        // Assert
        Assert.All(results, b =>
        {
            Assert.Equal(_testShopId, b.ShopId);
            Assert.NotEqual(_otherShopId, b.ShopId);
        });
    }

    #endregion
}
