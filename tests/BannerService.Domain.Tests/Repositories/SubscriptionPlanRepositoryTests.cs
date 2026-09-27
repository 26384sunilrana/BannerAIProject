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

public class SubscriptionPlanRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private SubscriptionPlanRepository _repository;

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new SubscriptionPlanRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var plans = new[]
        {
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Basic",
                Description = "Basic plan",
                Price = 99.99m,
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Premium",
                Description = "Premium plan",
                Price = 199.99m,
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Enterprise",
                Description = "Enterprise plan",
                Price = 499.99m,
                IsActive = false,
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.SubscriptionPlans.AddRange(plans);
        await _context.SaveChangesAsync();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnPlan()
    {
        // Arrange
        var plan = _context.SubscriptionPlans.First(p => p.IsActive);

        // Act
        var result = await _repository.GetByIdAsync(plan.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(plan.Id, result.Id);
        Assert.Equal(plan.Name, result.Name);
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

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_WithValidName_ShouldReturnPlan()
    {
        // Act
        var result = await _repository.GetByNameAsync("Basic");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Basic", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("NonexistentPlan");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldNotReturnInactivePlans()
    {
        // Act
        var result = await _repository.GetByNameAsync("Enterprise");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActivePlans()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, p => Assert.True(p.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_ShouldBeOrderedByDisplayOrder()
    {
        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        var orders = results.Select(p => p.DisplayOrder).ToList();
        var sortedOrders = orders.OrderBy(o => o).ToList();
        Assert.Equal(sortedOrders, orders);
    }

    #endregion

    #region GetActiveAsync Tests

    [Fact]
    public async Task GetActiveAsync_ShouldReturnOnlyActivePlans()
    {
        // Act
        var results = await _repository.GetActiveAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, p => Assert.True(p.IsActive));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidPlan_ShouldAddToDatabase()
    {
        // Arrange
        var newPlan = new SubscriptionPlan
        {
            Name = "Starter",
            Description = "Starter plan",
            Price = 49.99m,
            IsActive = true,
            DisplayOrder = 1
        };

        // Act
        var result = await _repository.CreateAsync(newPlan);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.SubscriptionPlans.FindAsync(result.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetCreatedAt()
    {
        // Arrange
        var newPlan = new SubscriptionPlan
        {
            Name = "NewPlan",
            Price = 99.99m,
            IsActive = true
        };

        // Act
        var result = await _repository.CreateAsync(newPlan);

        // Assert
        Assert.True(result.CreatedAt > DateTime.MinValue);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidPlan_ShouldUpdateDatabase()
    {
        // Arrange
        var plan = _context.SubscriptionPlans.First(p => p.IsActive);
        plan.Price = 149.99m;

        // Act
        await _repository.UpdateAsync(plan);

        // Assert
        var updated = await _context.SubscriptionPlans.FindAsync(plan.Id);
        Assert.Equal(149.99m, updated.Price);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeactivatePlan()
    {
        // Arrange
        var plan = _context.SubscriptionPlans.First(p => p.IsActive);

        // Act
        var result = await _repository.DeleteAsync(plan.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.SubscriptionPlans.FindAsync(plan.Id);
        Assert.False(deleted.IsActive);
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

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var plan = _context.SubscriptionPlans.First();

        // Act
        var result = await _repository.ExistsAsync(plan.Id);

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
    public async Task GetCountAsync_ShouldReturnOnlyActivePlanCount()
    {
        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    #endregion
}
