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

public class BannerVersionRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private BannerVersionRepository _repository;
    private readonly Guid _bannerId = Guid.NewGuid();
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _otherShopId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new BannerVersionRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var version1 = new BannerVersion
        {
            Id = Guid.NewGuid(),
            BannerId = _bannerId,
            ShopId = _shopId,
            VersionNumber = 1,
            VersionJson = "{\"name\": \"Banner v1\"}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var version2 = new BannerVersion
        {
            Id = Guid.NewGuid(),
            BannerId = _bannerId,
            ShopId = _shopId,
            VersionNumber = 2,
            VersionJson = "{\"name\": \"Banner v2\"}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var version3 = new BannerVersion
        {
            Id = Guid.NewGuid(),
            BannerId = _bannerId,
            ShopId = _shopId,
            VersionNumber = 3,
            VersionJson = "{\"name\": \"Banner v3\"}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var otherShopVersion = new BannerVersion
        {
            Id = Guid.NewGuid(),
            BannerId = Guid.NewGuid(),
            ShopId = _otherShopId,
            VersionNumber = 1,
            VersionJson = "{\"name\": \"Other Banner\"}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.BannerVersions.AddRange(version1, version2, version3, otherShopVersion);
        await _context.SaveChangesAsync();
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithValidVersion_ShouldAddToDatabase()
    {
        // Arrange
        var newVersion = new BannerVersion
        {
            Id = Guid.NewGuid(),
            BannerId = _bannerId,
            ShopId = _shopId,
            VersionNumber = 4,
            VersionJson = "{\"name\": \"Banner v4\"}",
            IsActive = true
        };

        // Act
        var result = await _repository.SaveAsync(newVersion);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.BannerVersions.FindAsync(newVersion.Id);
        Assert.NotNull(saved);
    }

    #endregion

    #region GetVersionsAsync Tests

    [Fact]
    public async Task GetVersionsAsync_WithValidBannerAndShop_ShouldReturnVersions()
    {
        // Act
        var results = await _repository.GetVersionsAsync(_bannerId, _shopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(3, results.Count);
        Assert.All(results, v =>
        {
            Assert.Equal(_bannerId, v.BannerId);
            Assert.Equal(_shopId, v.ShopId);
            Assert.True(v.IsActive);
        });
    }

    [Fact]
    public async Task GetVersionsAsync_ShouldBeOrderedByVersionNumberDescending()
    {
        // Act
        var results = await _repository.GetVersionsAsync(_bannerId, _shopId);

        // Assert
        var versions = results.Select(v => v.VersionNumber).ToList();
        var sorted = versions.OrderByDescending(v => v).ToList();
        Assert.Equal(sorted, versions);
    }

    [Fact]
    public async Task GetVersionsAsync_ShouldOnlyReturnActiveVersions()
    {
        // Act
        var results = await _repository.GetVersionsAsync(_bannerId, _shopId);

        // Assert
        Assert.All(results, v => Assert.True(v.IsActive));
    }

    #endregion

    #region GetVersionAsync Tests

    [Fact]
    public async Task GetVersionAsync_WithValidData_ShouldReturnVersion()
    {
        // Act
        var result = await _repository.GetVersionAsync(_bannerId, 2, _shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.VersionNumber);
        Assert.Equal(_bannerId, result.BannerId);
    }

    [Fact]
    public async Task GetVersionAsync_WithInvalidVersionNumber_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetVersionAsync(_bannerId, 999, _shopId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVersionAsync_WithDifferentShop_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetVersionAsync(_bannerId, 2, _otherShopId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetNextVersionNumberAsync Tests

    [Fact]
    public async Task GetNextVersionNumberAsync_ShouldReturnNextNumber()
    {
        // Act
        var nextNumber = await _repository.GetNextVersionNumberAsync(_bannerId, _shopId);

        // Assert
        Assert.Equal(4, nextNumber);
    }

    [Fact]
    public async Task GetNextVersionNumberAsync_WithNewBanner_ShouldReturnOne()
    {
        // Act
        var nextNumber = await _repository.GetNextVersionNumberAsync(Guid.NewGuid(), _shopId);

        // Assert
        Assert.Equal(1, nextNumber);
    }

    #endregion
}
