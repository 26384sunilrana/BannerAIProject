namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class ShopOwnerDashboardEntityTests
{
    private readonly Guid _testShopId = Guid.NewGuid();

    #region ShopOwnerDashboard Construction Tests

    [Fact]
    public void CreateShopOwnerDashboard_ShouldInitializeProperties()
    {
        // Act
        var dashboard = new ShopOwnerDashboard { ShopId = _testShopId };

        // Assert
        Assert.NotEqual(Guid.Empty, dashboard.Id);
        Assert.Equal(_testShopId, dashboard.ShopId);
        Assert.NotNull(dashboard.CurrentSummary);
        Assert.NotEqual(DateTime.MinValue, dashboard.CreatedAt);
        Assert.NotEqual(DateTime.MinValue, dashboard.LastUpdatedAt);
    }

    #endregion

    #region UpdateSummary Tests

    [Fact]
    public void UpdateSummary_WithValidSummary_ShouldUpdate()
    {
        // Arrange
        var dashboard = new ShopOwnerDashboard { ShopId = _testShopId };
        var newSummary = new ShopDashboardSummary { TotalRevenue = 3000m };
        var originalUpdateTime = dashboard.LastUpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        dashboard.UpdateSummary(newSummary);

        // Assert
        Assert.Equal(newSummary.TotalRevenue, dashboard.CurrentSummary.TotalRevenue);
        Assert.True(dashboard.LastUpdatedAt > originalUpdateTime);
    }

    [Fact]
    public void UpdateSummary_ShouldUpdateTimestamp()
    {
        // Arrange
        var dashboard = new ShopOwnerDashboard { ShopId = _testShopId };
        var originalTime = dashboard.LastUpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        dashboard.UpdateSummary(new ShopDashboardSummary());

        // Assert
        Assert.True(dashboard.LastUpdatedAt > originalTime);
    }

    #endregion

    #region NeedsRefresh Tests

    [Fact]
    public void NeedsRefresh_WithRecentUpdate_ShouldReturnFalse()
    {
        // Arrange
        var dashboard = new ShopOwnerDashboard { ShopId = _testShopId };

        // Act
        var result = dashboard.NeedsRefresh(refreshIntervalMinutes: 5);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NeedsRefresh_WithOldUpdate_ShouldReturnTrue()
    {
        // Arrange
        var dashboard = new ShopOwnerDashboard { ShopId = _testShopId };
        var oldTime = DateTime.UtcNow.AddMinutes(-10);
        var reflectionProperty = dashboard.GetType().GetProperty("LastUpdatedAt");
        reflectionProperty?.SetValue(dashboard, oldTime);

        // Act
        var result = dashboard.NeedsRefresh(refreshIntervalMinutes: 5);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void NeedsRefresh_WithDefaultInterval_ShouldUse5Minutes()
    {
        // Arrange
        var dashboard = new ShopOwnerDashboard { ShopId = _testShopId };

        // Act
        var result = dashboard.NeedsRefresh(); // Should use default 5 minutes

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ShopDashboardMetricSnapshot Tests

    [Fact]
    public void CreateMetricSnapshot_ShouldInitializeProperties()
    {
        // Act
        var snapshot = new ShopDashboardMetricSnapshot
        {
            ShopId = _testShopId,
            MetricType = "Orders",
            MetricValue = 50m
        };

        // Assert
        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(_testShopId, snapshot.ShopId);
        Assert.Equal("Orders", snapshot.MetricType);
        Assert.Equal(50m, snapshot.MetricValue);
        Assert.NotEqual(DateTime.MinValue, snapshot.SnapshotDate);
    }

    [Fact]
    public void CreateMetricSnapshot_ShouldHaveEmptyMetadata()
    {
        // Act
        var snapshot = new ShopDashboardMetricSnapshot { ShopId = _testShopId };

        // Assert
        Assert.NotNull(snapshot.MetadataJson);
        Assert.Empty(snapshot.MetadataJson);
    }

    #endregion

    #region ShopDashboardAlert Tests

    [Fact]
    public void CreateAlert_ShouldInitializeProperties()
    {
        // Act
        var alert = new ShopDashboardAlert
        {
            ShopId = _testShopId,
            AlertType = "PaymentFailed",
            Message = "Payment processing failed",
            Severity = AlertSeverity.Critical
        };

        // Assert
        Assert.NotEqual(Guid.Empty, alert.Id);
        Assert.Equal(_testShopId, alert.ShopId);
        Assert.Equal("PaymentFailed", alert.AlertType);
        Assert.Equal("Payment processing failed", alert.Message);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.False(alert.IsResolved);
        Assert.NotEqual(DateTime.MinValue, alert.CreatedAt);
        Assert.Null(alert.ResolvedAt);
    }

    [Fact]
    public void CreateWarning_ShouldSetWarningSeverity()
    {
        // Act
        var alert = ShopDashboardAlert.CreateWarning(_testShopId, "LowStock", "Stock running low");

        // Assert
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.Equal(_testShopId, alert.ShopId);
        Assert.Equal("LowStock", alert.AlertType);
    }

    [Fact]
    public void CreateCritical_ShouldSetCriticalSeverity()
    {
        // Act
        var alert = ShopDashboardAlert.CreateCritical(_testShopId, "SystemDown", "System is down");

        // Assert
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Equal(_testShopId, alert.ShopId);
    }

    [Fact]
    public void Resolve_ShouldMarkAsResolved()
    {
        // Arrange
        var alert = new ShopDashboardAlert
        {
            ShopId = _testShopId,
            AlertType = "Test",
            Message = "Test",
            IsResolved = false
        };

        // Act
        alert.Resolve();

        // Assert
        Assert.True(alert.IsResolved);
        Assert.NotNull(alert.ResolvedAt);
    }

    [Fact]
    public void Resolve_ShouldSetResolvedAtTimestamp()
    {
        // Arrange
        var alert = new ShopDashboardAlert
        {
            ShopId = _testShopId,
            AlertType = "Test",
            Message = "Test"
        };
        var beforeResolve = DateTime.UtcNow;

        // Act
        alert.Resolve();

        // Assert
        Assert.NotNull(alert.ResolvedAt);
        Assert.True(alert.ResolvedAt >= beforeResolve);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Dashboard_WithMultipleUpdates_ShouldTrackLatestTimestamp()
    {
        // Arrange
        var dashboard = new ShopOwnerDashboard { ShopId = _testShopId };
        var timestamp1 = dashboard.LastUpdatedAt;

        System.Threading.Thread.Sleep(20);
        dashboard.UpdateSummary(new ShopDashboardSummary { TotalRevenue = 1000m });
        var timestamp2 = dashboard.LastUpdatedAt;

        System.Threading.Thread.Sleep(20);
        dashboard.UpdateSummary(new ShopDashboardSummary { TotalRevenue = 2000m });
        var timestamp3 = dashboard.LastUpdatedAt;

        // Assert
        Assert.True(timestamp1 < timestamp2 && timestamp2 < timestamp3);
    }

    #endregion
}
