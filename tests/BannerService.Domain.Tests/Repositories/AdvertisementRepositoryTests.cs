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

public class AdvertisementRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private AdvertisementRepository _repository;
    private readonly Guid _testShopId = Guid.NewGuid();
    private readonly Guid _otherShopId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new AdvertisementRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var ad1 = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Title = "Ad 1",
            Description = "Test Ad 1",
            Type = AdType.Banner,
            Status = AdStatus.Active,
            IsActive = true,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var ad2 = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Title = "Ad 2",
            Description = "Test Ad 2",
            Type = AdType.Text,
            Status = AdStatus.Draft,
            IsActive = false,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(31)
        };

        var otherShopAd = new Advertisement
        {
            ShopId = _otherShopId,
            CreatedByUserId = Guid.NewGuid(),
            Title = "Other Shop Ad",
            Description = "Ad from other shop",
            Type = AdType.Video,
            Status = AdStatus.Active,
            IsActive = true,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        _context.Set<Advertisement>().AddRange(ad1, ad2, otherShopAd);
        await _context.SaveChangesAsync();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnAdvertisement()
    {
        // Arrange
        var ad = _context.Set<Advertisement>().First(a => a.ShopId == _testShopId);

        // Act
        var result = await _repository.GetByIdAsync(ad.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ad.Id, result.Id);
        Assert.Equal(ad.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByShopIdAsync Tests

    [Fact]
    public async Task GetByShopIdAsync_WithValidShopId_ShouldReturnAdvertisements()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(_testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(2, results.Count);
        Assert.All(results, a => Assert.Equal(_testShopId, a.ShopId));
    }

    [Fact]
    public async Task GetByShopIdAsync_WithInvalidShopId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByShopIdAsync_ShouldOrderByCreatedDateDescending()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(_testShopId);

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(a => a.CreatedAt)));
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithValidStatus_ShouldReturnAds()
    {
        // Act
        var results = await _repository.GetByStatusAsync(AdStatus.Active);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, a => Assert.Equal(AdStatus.Active, a.Status));
    }

    [Fact]
    public async Task GetByStatusAsync_WithStatusHavingNoAds_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByStatusAsync(AdStatus.Completed);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldOrderByCreatedDateDescending()
    {
        // Act
        var results = await _repository.GetByStatusAsync(AdStatus.Active);

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(a => a.CreatedAt)));
    }

    #endregion

    #region GetActiveAdsAsync Tests

    [Fact]
    public async Task GetActiveAdsAsync_ShouldReturnOnlyActiveAds()
    {
        // Act
        var results = await _repository.GetActiveAdsAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, a => Assert.True(a.IsActive && a.Status == AdStatus.Active));
    }

    [Fact]
    public async Task GetActiveAdsAsync_ShouldOrderByPriorityThenDate()
    {
        // Act
        var results = await _repository.GetActiveAdsAsync();

        // Assert
        Assert.NotEmpty(results);
    }

    #endregion

    #region GetByCreatorAsync Tests

    [Fact]
    public async Task GetByCreatorAsync_WithValidUserId_ShouldReturnAds()
    {
        // Act
        var results = await _repository.GetByCreatorAsync(_testUserId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, a => Assert.Equal(_testUserId, a.CreatedByUserId));
    }

    [Fact]
    public async Task GetByCreatorAsync_WithInvalidUserId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByCreatorAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region GetExpiringAdsAsync Tests

    [Fact]
    public async Task GetExpiringAdsAsync_ShouldReturnAdsExpiringWithinThreshold()
    {
        // Arrange
        var expiringAd = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Title = "Expiring Ad",
            Description = "Ad expiring soon",
            Type = AdType.Banner,
            Status = AdStatus.Active,
            IsActive = true,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(3)
        };

        _context.Set<Advertisement>().Add(expiringAd);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetExpiringAdsAsync(daysThreshold: 7);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetExpiringAdsAsync_ShouldReturnOrderedByEndDate()
    {
        // Act
        var results = await _repository.GetExpiringAdsAsync();

        // Assert
        Assert.True(results.SequenceEqual(results.OrderBy(a => a.EndDate)));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAdvertisements()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.True(results.Count >= 3);
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderByCreatedDateDescending()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(a => a.CreatedAt)));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidAd_ShouldAddToDatabase()
    {
        // Arrange
        var newAd = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Title = "New Ad",
            Description = "New Advertisement",
            Type = AdType.Banner,
            Status = AdStatus.Draft
        };

        // Act
        var result = await _repository.CreateAsync(newAd);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<Advertisement>().FindAsync(newAd.Id);
        Assert.NotNull(saved);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidAd_ShouldUpdateDatabase()
    {
        // Arrange
        var ad = _context.Set<Advertisement>().First(a => a.ShopId == _testShopId);
        ad.Title = "Updated Title";

        // Act
        await _repository.UpdateAsync(ad);

        // Assert
        var updated = await _context.Set<Advertisement>().FindAsync(ad.Id);
        Assert.Equal("Updated Title", updated.Title);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemoveFromDatabase()
    {
        // Arrange
        var ad = _context.Set<Advertisement>().First(a => a.ShopId == _testShopId);

        // Act
        var result = await _repository.DeleteAsync(ad.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Set<Advertisement>().FindAsync(ad.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetByTypeAsync Tests

    [Fact]
    public async Task GetByTypeAsync_WithValidType_ShouldReturnAds()
    {
        // Act
        var results = await _repository.GetByTypeAsync(AdType.Banner);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, a => Assert.Equal(AdType.Banner, a.Type));
    }

    [Fact]
    public async Task GetByTypeAsync_WithTypeHavingNoAds_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByTypeAsync(AdType.Native);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region GetAdsByDateRangeAsync Tests

    [Fact]
    public async Task GetAdsByDateRangeAsync_WithValidRange_ShouldReturnAds()
    {
        // Act
        var results = await _repository.GetAdsByDateRangeAsync(
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(40)
        );

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetAdsByDateRangeAsync_WithFutureRange_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetAdsByDateRangeAsync(
            DateTime.UtcNow.AddDays(100),
            DateTime.UtcNow.AddDays(200)
        );

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task GetCountByShopAsync_WithValidShopId_ShouldReturnCount()
    {
        // Act
        var count = await _repository.GetCountByShopAsync(_testShopId);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetCountByStatusAsync_WithValidStatus_ShouldReturnCount()
    {
        // Act
        var count = await _repository.GetCountByStatusAsync(AdStatus.Active);

        // Assert
        Assert.True(count > 0);
    }

    #endregion
}
