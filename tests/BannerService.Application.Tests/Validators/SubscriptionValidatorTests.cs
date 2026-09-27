namespace BannerService.Application.Tests.Validators;

using System;
using System.Threading.Tasks;
using Xunit;
using BannerService.Application.Validators;
using BannerService.Domain.Entities;

public class SubscriptionValidatorTests
{
    private readonly SubscriptionValidator _validator;

    public SubscriptionValidatorTests()
    {
        _validator = new SubscriptionValidator();
    }

    #region Plan ID Validation Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyPlanId_ShouldReturnError()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.Empty,
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Subscription.PlanId));
    }

    [Fact]
    public async Task ValidateAsync_WithValidPlanId_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Subscription.PlanId));
    }

    #endregion

    #region User ID Validation Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyUserId_ShouldReturnError()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.Empty,
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Subscription.UserId));
    }

    [Fact]
    public async Task ValidateAsync_WithValidUserId_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Subscription.UserId));
    }

    #endregion

    #region Start Date Validation Tests

    [Fact]
    public async Task ValidateAsync_WithFutureStartDate_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow.AddDays(1),
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Subscription.StartDate));
    }

    [Fact]
    public async Task ValidateAsync_WithCurrentStartDate_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Subscription.StartDate));
    }

    #endregion

    #region End Date Validation Tests

    [Fact]
    public async Task ValidateAsync_WithEndDateBeforeStartDate_ShouldReturnError()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = startDate,
            EndDate = startDate.AddDays(-1),
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithEndDateAfterStartDate_ShouldPass()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = startDate,
            EndDate = startDate.AddDays(30),
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Billing Cycle Validation Tests

    [Fact]
    public async Task ValidateAsync_WithValidBillingCycle_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            BillingCycle = "Monthly",
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Subscription.BillingCycle));
    }

    [Fact]
    public async Task ValidateAsync_WithMonthlyBillingCycle_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            BillingCycle = "Monthly",
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithAnnualBillingCycle_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            BillingCycle = "Annual",
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Status Validation Tests

    [Fact]
    public async Task ValidateAsync_WithActiveStatus_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Subscription.Status));
    }

    [Fact]
    public async Task ValidateAsync_WithInactiveStatus_ShouldPass()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            Status = SubscriptionStatus.Inactive
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Comprehensive Validation Tests

    [Fact]
    public async Task ValidateAsync_WithAllValidData_ShouldPass()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StartDate = startDate,
            EndDate = startDate.AddMonths(1),
            BillingCycle = "Monthly",
            Status = SubscriptionStatus.Active,
            AutoRenew = true,
            Notes = "Test subscription"
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleErrors_ShouldReturnAll()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var subscription = new Subscription
        {
            PlanId = Guid.Empty,
            UserId = Guid.Empty,
            StartDate = startDate,
            EndDate = startDate.AddDays(-1),
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = await _validator.ValidateAsync(subscription);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    #endregion
}
