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

public class ShopRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private ShopRepository _repository;
    private readonly Guid _ownerUserId = Guid.NewGuid();
    private readonly string _ownerUserIdStr = Guid.NewGuid().ToString();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new ShopRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var shop1 = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "Main Shop",
            Description = "Main shop description",
            Status = ShopStatus.Active,
            OwnerUserId = _ownerUserId,
            CreatedByUserId = Guid.NewGuid(),
            PhoneNumber = "1234567890",
            City = "New York"
        };

        var shop2 = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "Branch Shop",
            Description = "Branch shop",
            Status = ShopStatus.Active,
            OwnerUserId = _ownerUserId,
            CreatedByUserId = Guid.NewGuid(),
            ParentShopId = shop1.Id,
            City = "Boston"
        };

        var inactiveShop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Shop",
            Status = ShopStatus.Inactive,
            OwnerUserId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid()
        };

        var archivedShop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "Archived Shop",
            Status = ShopStatus.Archived,
            OwnerUserId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid()
        };

        _context.Shops.AddRange(shop1, shop2, inactiveShop, archivedShop);
        await _context.SaveChangesAsync();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnShop()
    {
        // Arrange
        var shop = _context.Shops.First(s => s.Status == ShopStatus.Active);

        // Act
        var result = await _repository.GetByIdAsync(shop.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shop.Id, result.Id);
        Assert.Equal(shop.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOwnerUserId()
    {
        // Arrange
        var shop = _context.Shops.First(s => s.Status == ShopStatus.Active);

        // Act
        var result = await _repository.GetByIdAsync(shop.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shop.OwnerUserId, result.OwnerUserId);
    }

    #endregion

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_WithValidName_ShouldReturnShop()
    {
        // Act
        var result = await _repository.GetByNameAsync("Main Shop");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Main Shop", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("Nonexistent Shop");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldNotReturnArchivedShops()
    {
        // Act
        var result = await _repository.GetByNameAsync("Archived Shop");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnNonArchivedShops()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.NotEqual(ShopStatus.Archived, s.Status));
    }

    [Fact]
    public async Task GetAllAsync_ShouldBeOrderedByName()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        var names = results.Select(s => s.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, names);
    }

    #endregion

    #region GetActiveAsync Tests

    [Fact]
    public async Task GetActiveAsync_ShouldReturnOnlyActiveShops()
    {
        // Act
        var results = await _repository.GetActiveAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal(ShopStatus.Active, s.Status));
    }

    #endregion

    #region GetByOwnerAsync Tests

    [Fact]
    public async Task GetByOwnerAsync_WithValidOwnerId_ShouldReturnShops()
    {
        // Act
        var results = await _repository.GetByOwnerAsync(_ownerUserId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal(_ownerUserId, s.OwnerUserId));
    }

    [Fact]
    public async Task GetByOwnerAsync_ShouldNotReturnArchivedShops()
    {
        // Act
        var results = await _repository.GetByOwnerAsync(_ownerUserId);

        // Assert
        Assert.All(results, s => Assert.NotEqual(ShopStatus.Archived, s.Status));
    }

    [Fact]
    public async Task GetByOwnerAsync_WithInvalidOwnerId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByOwnerAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region GetByParentAsync Tests

    [Fact]
    public async Task GetByParentAsync_WithValidParentId_ShouldReturnChildShops()
    {
        // Arrange
        var parentShop = _context.Shops.First(s => s.Name == "Main Shop");

        // Act
        var results = await _repository.GetByParentAsync(parentShop.Id);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal(parentShop.Id, s.ParentShopId));
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNameTerm_ShouldReturnMatchingShops()
    {
        // Act
        var results = await _repository.SearchAsync("Main");

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, s => s.Name.Contains("Main"));
    }

    [Fact]
    public async Task SearchAsync_WithCityFilter_ShouldReturnShopsInCity()
    {
        // Act
        var results = await _repository.SearchAsync("", city: "New York");

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal("New York", s.City));
    }

    [Fact]
    public async Task SearchAsync_WithStatusFilter_ShouldReturnShopsWithStatus()
    {
        // Act
        var results = await _repository.SearchAsync("", status: ShopStatus.Active);

        // Assert
        Assert.All(results, s => Assert.Equal(ShopStatus.Active, s.Status));
    }

    #endregion

    #region GetPaginatedAsync Tests

    [Fact]
    public async Task GetPaginatedAsync_WithPageOne_ShouldReturnFirstPage()
    {
        // Act
        var results = await _repository.GetPaginatedAsync(pageNumber: 1, pageSize: 1);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public async Task GetPaginatedAsync_WithPageTwo_ShouldReturnSecondPage()
    {
        // Act
        var page1 = await _repository.GetPaginatedAsync(pageNumber: 1, pageSize: 1);
        var page2 = await _repository.GetPaginatedAsync(pageNumber: 2, pageSize: 1);

        // Assert
        Assert.NotEqual(page1.First().Id, page2.First().Id);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidShop_ShouldAddToDatabase()
    {
        // Arrange
        var newShop = new Shop
        {
            Name = "New Shop",
            Status = ShopStatus.Active,
            OwnerUserId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid()
        };

        // Act
        var result = await _repository.CreateAsync(newShop);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.Shops.FindAsync(result.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetTimestamps()
    {
        // Arrange
        var newShop = new Shop
        {
            Name = "Timestamp Shop",
            OwnerUserId = Guid.NewGuid(),
            CreatedByUserId = Guid.NewGuid()
        };

        // Act
        await _repository.CreateAsync(newShop);

        // Assert
        var saved = await _context.Shops.FindAsync(newShop.Id);
        Assert.True(saved.CreatedAt > DateTime.MinValue);
        Assert.True(saved.UpdatedAt > DateTime.MinValue);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidShop_ShouldUpdateDatabase()
    {
        // Arrange
        var shop = _context.Shops.First(s => s.Status == ShopStatus.Active);
        shop.Name = "Updated Shop Name";

        // Act
        await _repository.UpdateAsync(shop);

        // Assert
        var updated = await _context.Shops.FindAsync(shop.Id);
        Assert.Equal("Updated Shop Name", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateModificationTime()
    {
        // Arrange
        var shop = _context.Shops.First(s => s.Status == ShopStatus.Active);
        var originalTime = shop.UpdatedAt;
        System.Threading.Thread.Sleep(10);
        shop.Name = "Modified";

        // Act
        await _repository.UpdateAsync(shop);

        // Assert
        var updated = await _context.Shops.FindAsync(shop.Id);
        Assert.True(updated.UpdatedAt > originalTime);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldArchiveShop()
    {
        // Arrange
        var shop = _context.Shops.First(s => s.Status == ShopStatus.Active && s.ParentShopId == null);

        // Act
        var result = await _repository.DeleteAsync(shop.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Shops.FindAsync(shop.Id);
        Assert.Equal(ShopStatus.Archived, deleted.Status);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldArchiveChildShops()
    {
        // Arrange
        var parentShop = _context.Shops.First(s => s.Name == "Main Shop");
        var childShop = _context.Shops.First(s => s.ParentShopId == parentShop.Id);

        // Act
        await _repository.DeleteAsync(parentShop.Id);

        // Assert
        var archivedChild = await _context.Shops.FindAsync(childShop.Id);
        Assert.Equal(ShopStatus.Archived, archivedChild.Status);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var shop = _context.Shops.First();

        // Act
        var result = await _repository.ExistsAsync(shop.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonexistentId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        Assert.True(count > 0);
    }

    [Fact]
    public async Task GetCountByStatusAsync_ShouldReturnCorrectCount()
    {
        // Act
        var activeCount = await _repository.GetCountByStatusAsync(ShopStatus.Active);

        // Assert
        Assert.True(activeCount > 0);
    }

    [Fact]
    public async Task GetCountByOwnerAsync_ShouldReturnCorrectCount()
    {
        // Act
        var count = await _repository.GetCountByOwnerAsync(_ownerUserId);

        // Assert
        Assert.Equal(2, count);
    }

    #endregion

    #region Hierarchy Tests

    [Fact]
    public async Task GetHierarchyAsync_WithValidId_ShouldLoadChildren()
    {
        // Arrange
        var parentShop = _context.Shops.First(s => s.Name == "Main Shop");

        // Act
        var result = await _repository.GetHierarchyAsync(parentShop.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ChildShops);
    }

    [Fact]
    public async Task GetDescendantsAsync_WithValidId_ShouldReturnDescendants()
    {
        // Arrange
        var parentShop = _context.Shops.First(s => s.Name == "Main Shop");

        // Act
        var descendants = await _repository.GetDescendantsAsync(parentShop.Id);

        // Assert
        Assert.NotEmpty(descendants);
    }

    [Fact]
    public async Task GetAncestorsAsync_WithValidId_ShouldReturnAncestors()
    {
        // Arrange
        var childShop = _context.Shops.First(s => s.ParentShopId != null);

        // Act
        var ancestors = await _repository.GetAncestorsAsync(childShop.Id);

        // Assert
        Assert.NotEmpty(ancestors);
    }

    #endregion
}
