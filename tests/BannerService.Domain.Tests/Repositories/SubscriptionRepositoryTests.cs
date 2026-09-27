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

public class SubscriptionRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private SubscriptionRepository _repository;
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _planId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new SubscriptionRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var plan = new SubscriptionPlan
        {
            Id = _planId,
            Name = "Basic",
            Price = 99.99m,
            IsActive = true
        };

        _context.SubscriptionPlans.Add(plan);

        var subscription1 = new Subscription
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            PlanId = _planId,
            Status = SubscriptionStatus.Active,
            BillingPeriod = BillingPeriod.Monthly,
            StartDate = DateTime.UtcNow.AddDays(-30),
            RenewalDate = DateTime.UtcNow.AddDays(30),
            CurrentPrice = 99.99m,
            CreatedAt = DateTime.UtcNow
        };

        var subscription2 = new Subscription
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            PlanId = _planId,
            Status = SubscriptionStatus.Trial,
            BillingPeriod = BillingPeriod.Monthly,
            StartDate = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddDays(14),
            TrialEndDate = DateTime.UtcNow.AddDays(14),
            CurrentPrice = 0,
            CreatedAt = DateTime.UtcNow
        };

        var cancelledSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            PlanId = _planId,
            Status = SubscriptionStatus.Cancelled,
            BillingPeriod = BillingPeriod.Monthly,
            StartDate = DateTime.UtcNow.AddDays(-60),
            RenewalDate = DateTime.UtcNow.AddDays(-30),
            CancellationDate = DateTime.UtcNow,
            CurrentPrice = 99.99m,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.AddRange(subscription1, subscription2, cancelledSubscription);
        await _context.SaveChangesAsync();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnSubscription()
    {
        // Arrange
        var subscription = _context.Subscriptions.First(s => s.Status == SubscriptionStatus.Active);

        // Act
        var result = await _repository.GetByIdAsync(subscription.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(subscription.Id, result.Id);
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
    public async Task GetByShopIdAsync_WithValidShopId_ShouldReturnSubscription()
    {
        // Act
        var result = await _repository.GetByShopIdAsync(_shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_shopId, result.ShopId);
    }

    [Fact]
    public async Task GetByShopIdAsync_ShouldNotReturnCancelledSubscription()
    {
        // Act
        var result = await _repository.GetByShopIdAsync(_shopId);

        // Assert
        Assert.NotEqual(SubscriptionStatus.Cancelled, result?.Status);
    }

    [Fact]
    public async Task GetByShopIdAsync_WithInvalidShopId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByShopIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetActiveByShopIdAsync Tests

    [Fact]
    public async Task GetActiveByShopIdAsync_ShouldReturnOnlyActiveSubscriptions()
    {
        // Act
        var results = await _repository.GetActiveByShopIdAsync(_shopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal(SubscriptionStatus.Active, s.Status));
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithStatus_ShouldReturnSubscriptionsWithStatus()
    {
        // Act
        var results = await _repository.GetByStatusAsync(SubscriptionStatus.Active);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal(SubscriptionStatus.Active, s.Status));
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldBeOrderedByRenewalDate()
    {
        // Act
        var results = await _repository.GetByStatusAsync(SubscriptionStatus.Active);

        // Assert
        if (results.Count > 1)
        {
            var dates = results.Select(s => s.RenewalDate).ToList();
            var sortedDates = dates.OrderBy(d => d).ToList();
            Assert.Equal(sortedDates, dates);
        }
    }

    #endregion

    #region GetByPlanIdAsync Tests

    [Fact]
    public async Task GetByPlanIdAsync_WithValidPlanId_ShouldReturnSubscriptions()
    {
        // Act
        var results = await _repository.GetByPlanIdAsync(_planId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal(_planId, s.PlanId));
    }

    #endregion

    #region GetExpiringTodayAsync Tests

    [Fact]
    public async Task GetExpiringTodayAsync_WithMatchingSubscription_ShouldReturnSubscription()
    {
        // Arrange
        var todaySubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            PlanId = _planId,
            Status = SubscriptionStatus.Active,
            BillingPeriod = BillingPeriod.Monthly,
            StartDate = DateTime.UtcNow.AddDays(-30),
            RenewalDate = DateTime.UtcNow.Date.AddHours(12),
            CurrentPrice = 99.99m,
            CreatedAt = DateTime.UtcNow
        };
        _context.Subscriptions.Add(todaySubscription);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetExpiringTodayAsync();

        // Assert
        Assert.Contains(results, s => s.Id == todaySubscription.Id);
    }

    #endregion

    #region GetRenewingSoonAsync Tests

    [Fact]
    public async Task GetRenewingSoonAsync_ShouldReturnSubscriptionsRenewingWithinThreshold()
    {
        // Act
        var results = await _repository.GetRenewingSoonAsync(daysThreshold: 60);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.True(s.DaysUntilRenewal <= 60 && s.DaysUntilRenewal > 0));
    }

    [Fact]
    public async Task GetRenewingSoonAsync_ShouldBeOrderedByRenewalDate()
    {
        // Act
        var results = await _repository.GetRenewingSoonAsync(daysThreshold: 60);

        // Assert
        if (results.Count > 1)
        {
            var dates = results.Select(s => s.RenewalDate).ToList();
            var sortedDates = dates.OrderBy(d => d).ToList();
            Assert.Equal(sortedDates, dates);
        }
    }

    #endregion

    #region GetUnpaidAsync Tests

    [Fact]
    public async Task GetUnpaidAsync_ShouldReturnUnpaidSubscriptions()
    {
        // Act
        var results = await _repository.GetUnpaidAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s =>
            Assert.True(s.Status == SubscriptionStatus.Issued || s.Status == SubscriptionStatus.Overdue)
        );
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidSubscription_ShouldAddToDatabase()
    {
        // Arrange
        var newSubscription = new Subscription
        {
            ShopId = Guid.NewGuid(),
            PlanId = _planId,
            Status = SubscriptionStatus.Active,
            BillingPeriod = BillingPeriod.Monthly,
            StartDate = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddMonths(1),
            CurrentPrice = 99.99m
        };

        // Act
        var result = await _repository.CreateAsync(newSubscription);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.Subscriptions.FindAsync(result.Id);
        Assert.NotNull(saved);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidSubscription_ShouldUpdateDatabase()
    {
        // Arrange
        var subscription = _context.Subscriptions.First(s => s.Status == SubscriptionStatus.Active);
        subscription.Status = SubscriptionStatus.Suspended;

        // Act
        await _repository.UpdateAsync(subscription);

        // Assert
        var updated = await _context.Subscriptions.FindAsync(subscription.Id);
        Assert.Equal(SubscriptionStatus.Suspended, updated.Status);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldCancelSubscription()
    {
        // Arrange
        var subscription = _context.Subscriptions.First(s => s.Status == SubscriptionStatus.Active);

        // Act
        var result = await _repository.DeleteAsync(subscription.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Subscriptions.FindAsync(subscription.Id);
        Assert.Equal(SubscriptionStatus.Cancelled, deleted.Status);
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
        var subscription = _context.Subscriptions.First();

        // Act
        var result = await _repository.ExistsAsync(subscription.Id);

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
        var count = await _repository.GetCountByStatusAsync(SubscriptionStatus.Active);

        // Assert
        Assert.True(count > 0);
    }

    #endregion
}
