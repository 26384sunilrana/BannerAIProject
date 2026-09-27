namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class SubscriptionPlanEntityTests
{
    #region Constructor Tests

    [Fact]
    public void CreateSubscriptionPlan_WithValidData_ShouldInitializeProperties()
    {
        // Act
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Premium",
            Description = "Premium plan",
            Price = 199.99m,
            IsActive = true,
            DisplayOrder = 2
        };

        // Assert
        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal("Premium", plan.Name);
        Assert.Equal(199.99m, plan.Price);
        Assert.True(plan.IsActive);
    }

    [Fact]
    public void CreateSubscriptionPlan_ShouldHaveEmptyFeaturesList()
    {
        // Act
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Basic",
            Price = 99.99m
        };

        // Assert
        Assert.NotNull(plan.Features);
        Assert.Empty(plan.Features);
    }

    #endregion

    #region Features Collection Tests

    [Fact]
    public void Features_ShouldSupportAddingFeatures()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Basic",
            Price = 99.99m,
            Features = new List<string> { "Feature 1", "Feature 2", "Feature 3" }
        };

        // Act & Assert
        Assert.Equal(3, plan.Features.Count);
        Assert.Contains("Feature 1", plan.Features);
    }

    #endregion

    #region Price and Billing Tests

    [Fact]
    public void Price_ShouldAcceptDecimalValues()
    {
        // Act
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise",
            Price = 9999.99m
        };

        // Assert
        Assert.Equal(9999.99m, plan.Price);
    }

    [Fact]
    public void BillingPeriod_ShouldBeStorable()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Annual",
            Price = 1199.99m,
            BillingPeriod = BillingPeriod.Annually
        };

        // Act & Assert
        Assert.Equal(BillingPeriod.Annually, plan.BillingPeriod);
    }

    #endregion

    #region Status Tests

    [Fact]
    public void IsActive_WhenTrue_ShouldIndicateActive()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Active Plan",
            Price = 99.99m,
            IsActive = true
        };

        // Act & Assert
        Assert.True(plan.IsActive);
    }

    [Fact]
    public void IsActive_WhenFalse_ShouldIndicateInactive()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Plan",
            Price = 99.99m,
            IsActive = false
        };

        // Act & Assert
        Assert.False(plan.IsActive);
    }

    #endregion

    #region Display Order Tests

    [Fact]
    public void DisplayOrder_ShouldDetermineDisplaySequence()
    {
        // Arrange
        var basicPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Basic",
            Price = 99.99m,
            DisplayOrder = 1
        };
        var premiumPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Premium",
            Price = 199.99m,
            DisplayOrder = 2
        };

        // Act & Assert
        Assert.True(basicPlan.DisplayOrder < premiumPlan.DisplayOrder);
    }

    #endregion

    #region Description Tests

    [Fact]
    public void Description_ShouldBeOptional()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Simple",
            Price = 99.99m,
            Description = null
        };

        // Act & Assert
        Assert.Null(plan.Description);
    }

    [Fact]
    public void Description_ShouldStoreDetailedText()
    {
        // Arrange
        var longDescription = "This is a comprehensive description of what's included in the plan";
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Detailed",
            Price = 99.99m,
            Description = longDescription
        };

        // Act & Assert
        Assert.Equal(longDescription, plan.Description);
    }

    #endregion
}
