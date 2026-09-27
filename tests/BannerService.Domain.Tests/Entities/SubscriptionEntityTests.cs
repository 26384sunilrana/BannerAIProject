namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class SubscriptionEntityTests
{
    #region Constructor Tests

    [Fact]
    public void CreateSubscription_WithValidData_ShouldInitializeProperties()
    {
        // Act
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            BillingPeriod = BillingPeriod.Monthly,
            StartDate = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddMonths(1),
            CurrentPrice = 99.99m
        };

        // Assert
        Assert.NotEqual(Guid.Empty, subscription.Id);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(99.99m, subscription.CurrentPrice);
    }

    #endregion

    #region IsOnTrial Property Tests

    [Fact]
    public void IsOnTrial_WithActiveTrial_ShouldReturnTrue()
    {
        // Arrange
        var subscription = new Subscription
        {
            TrialEndDate = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var result = subscription.IsOnTrial;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOnTrial_WithExpiredTrial_ShouldReturnFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            TrialEndDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = subscription.IsOnTrial;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsActive Property Tests

    [Fact]
    public void IsActive_WithActiveStatus_ShouldReturnTrue()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        // Act
        var result = subscription.IsActive;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsActive_WithCancelledStatus_ShouldReturnFalse()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Cancelled };

        // Act
        var result = subscription.IsActive;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsExpired Property Tests

    [Fact]
    public void IsExpired_WithPastRenewalAndInactive_ShouldReturnTrue()
    {
        // Arrange
        var subscription = new Subscription
        {
            RenewalDate = DateTime.UtcNow.AddDays(-1),
            Status = SubscriptionStatus.Expired
        };

        // Act
        var result = subscription.IsExpired;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpired_WithActiveStatus_ShouldReturnFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            RenewalDate = DateTime.UtcNow.AddDays(-1),
            Status = SubscriptionStatus.Active
        };

        // Act
        var result = subscription.IsExpired;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsInGracePeriod Property Tests

    [Fact]
    public void IsInGracePeriod_WithGracePeriodStatus_ShouldReturnTrue()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.GracePeriod };

        // Act
        var result = subscription.IsInGracePeriod;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInGracePeriod_WithActiveStatus_ShouldReturnFalse()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        // Act
        var result = subscription.IsInGracePeriod;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DaysUntilRenewal Property Tests

    [Fact]
    public void DaysUntilRenewal_ShouldReturnCorrectDays()
    {
        // Arrange
        var subscription = new Subscription
        {
            RenewalDate = DateTime.UtcNow.AddDays(10)
        };

        // Act
        var days = subscription.DaysUntilRenewal;

        // Assert
        Assert.True(days >= 9 && days <= 11);
    }

    #endregion

    #region IsRenewingSoon Property Tests

    [Fact]
    public void IsRenewingSoon_WithinSevenDays_ShouldReturnTrue()
    {
        // Arrange
        var subscription = new Subscription
        {
            RenewalDate = DateTime.UtcNow.AddDays(5)
        };

        // Act
        var result = subscription.IsRenewingSoon;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRenewingSoon_MoreThanSevenDays_ShouldReturnFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            RenewalDate = DateTime.UtcNow.AddDays(8)
        };

        // Act
        var result = subscription.IsRenewingSoon;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region NeedsRenewalToday Property Tests

    [Fact]
    public void NeedsRenewalToday_WhenRenewalToday_ShouldReturnTrue()
    {
        // Arrange
        var subscription = new Subscription
        {
            RenewalDate = DateTime.UtcNow.Date.AddHours(12)
        };

        // Act
        var result = subscription.NeedsRenewalToday;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void NeedsRenewalToday_WhenRenewalTomorrow_ShouldReturnFalse()
    {
        // Arrange
        var subscription = new Subscription
        {
            RenewalDate = DateTime.UtcNow.AddDays(1).Date
        };

        // Act
        var result = subscription.NeedsRenewalToday;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region SetStatus Tests

    [Fact]
    public void SetStatus_ShouldChangeStatusAndUpdateTime()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Active };
        var originalTime = subscription.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(10);
        subscription.SetStatus(SubscriptionStatus.Suspended);

        // Assert
        Assert.Equal(SubscriptionStatus.Suspended, subscription.Status);
        Assert.True(subscription.UpdatedAt > originalTime);
    }

    #endregion

    #region RecordPaymentAttempt Tests

    [Fact]
    public void RecordPaymentAttempt_ShouldIncrementFailureCount()
    {
        // Arrange
        var subscription = new Subscription { PaymentFailureCount = 0 };

        // Act
        subscription.RecordPaymentAttempt();

        // Assert
        Assert.Equal(1, subscription.PaymentFailureCount);
        Assert.NotNull(subscription.LastPaymentAttempt);
    }

    #endregion

    #region ResetPaymentFailures Tests

    [Fact]
    public void ResetPaymentFailures_ShouldClearFailures()
    {
        // Arrange
        var subscription = new Subscription
        {
            PaymentFailureCount = 5,
            LastPaymentAttempt = DateTime.UtcNow
        };

        // Act
        subscription.ResetPaymentFailures();

        // Assert
        Assert.Equal(0, subscription.PaymentFailureCount);
        Assert.Null(subscription.LastPaymentAttempt);
    }

    #endregion

    #region SetRenewalDate Tests

    [Fact]
    public void SetRenewalDate_ShouldUpdateDate()
    {
        // Arrange
        var subscription = new Subscription { RenewalDate = DateTime.UtcNow };
        var newDate = DateTime.UtcNow.AddMonths(1);

        // Act
        subscription.SetRenewalDate(newDate);

        // Assert
        Assert.Equal(newDate.Date, subscription.RenewalDate.Date);
    }

    #endregion

    #region ChangePlan Tests

    [Fact]
    public void ChangePlan_ShouldUpdatePlanAndPrice()
    {
        // Arrange
        var subscription = new Subscription
        {
            PlanId = Guid.NewGuid(),
            CurrentPrice = 99.99m
        };
        var newPlanId = Guid.NewGuid();
        var newPrice = 199.99m;

        // Act
        subscription.ChangePlan(newPlanId, newPrice);

        // Assert
        Assert.Equal(newPlanId, subscription.PlanId);
        Assert.Equal(newPrice, subscription.CurrentPrice);
        Assert.NotNull(subscription.PlanChangedAt);
        Assert.NotNull(subscription.PreviousPlanId);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_ShouldSetStatusAndCancellationDate()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        // Act
        subscription.Cancel();

        // Assert
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.NotNull(subscription.CancellationDate);
    }

    #endregion

    #region MoveToGracePeriod Tests

    [Fact]
    public void MoveToGracePeriod_ShouldSetStatus()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.PaymentFailed };

        // Act
        subscription.MoveToGracePeriod();

        // Assert
        Assert.Equal(SubscriptionStatus.GracePeriod, subscription.Status);
    }

    #endregion

    #region Expire Tests

    [Fact]
    public void Expire_ShouldSetStatusToExpired()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        // Act
        subscription.Expire();

        // Assert
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
    }

    #endregion

    #region MarkAsPaymentFailed Tests

    [Fact]
    public void MarkAsPaymentFailed_ShouldSetStatusAndRecordAttempt()
    {
        // Arrange
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Active,
            PaymentFailureCount = 0
        };

        // Act
        subscription.MarkAsPaymentFailed();

        // Assert
        Assert.Equal(SubscriptionStatus.PaymentFailed, subscription.Status);
        Assert.Equal(1, subscription.PaymentFailureCount);
    }

    #endregion

    #region Suspend Tests

    [Fact]
    public void Suspend_ShouldSetStatusToSuspended()
    {
        // Arrange
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        // Act
        subscription.Suspend();

        // Assert
        Assert.Equal(SubscriptionStatus.Suspended, subscription.Status);
    }

    #endregion
}
