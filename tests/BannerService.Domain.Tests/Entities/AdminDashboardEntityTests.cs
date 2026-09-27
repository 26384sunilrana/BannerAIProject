namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;

public class AdminDashboardEntityTests
{
    #region AdminDashboard Construction Tests

    [Fact]
    public void CreateAdminDashboard_ShouldInitializeProperties()
    {
        // Act
        var dashboard = new AdminDashboard();

        // Assert
        Assert.NotEqual(Guid.Empty, dashboard.Id);
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
        var dashboard = new AdminDashboard();
        var newSummary = new DashboardSummary { TotalRevenue = 5000m };
        var originalUpdateTime = dashboard.LastUpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        dashboard.UpdateSummary(newSummary);

        // Assert
        Assert.Equal(newSummary.TotalRevenue, dashboard.CurrentSummary.TotalRevenue);
        Assert.True(dashboard.LastUpdatedAt > originalUpdateTime);
    }

    #endregion

    #region NeedsRefresh Tests

    [Fact]
    public void NeedsRefresh_WithRecentUpdate_ShouldReturnFalse()
    {
        // Arrange
        var dashboard = new AdminDashboard();

        // Act
        var result = dashboard.NeedsRefresh(refreshIntervalMinutes: 5);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NeedsRefresh_WithOldUpdate_ShouldReturnTrue()
    {
        // Arrange
        var dashboard = new AdminDashboard();
        // Manually set LastUpdatedAt to old time
        var oldTime = DateTime.UtcNow.AddMinutes(-10);
        var reflectionProperty = dashboard.GetType().GetProperty("LastUpdatedAt");
        reflectionProperty?.SetValue(dashboard, oldTime);

        // Act
        var result = dashboard.NeedsRefresh(refreshIntervalMinutes: 5);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region DashboardMetricSnapshot Tests

    [Fact]
    public void CreateMetricSnapshot_ShouldInitializeProperties()
    {
        // Act
        var snapshot = new DashboardMetricSnapshot
        {
            MetricType = "Revenue",
            MetricValue = 1000m
        };

        // Assert
        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal("Revenue", snapshot.MetricType);
        Assert.Equal(1000m, snapshot.MetricValue);
        Assert.NotEqual(DateTime.MinValue, snapshot.SnapshotDate);
    }

    [Fact]
    public void CreateMetricSnapshot_ShouldHaveEmptyMetadata()
    {
        // Act
        var snapshot = new DashboardMetricSnapshot();

        // Assert
        Assert.NotNull(snapshot.MetadataJson);
        Assert.Empty(snapshot.MetadataJson);
    }

    [Fact]
    public void CreateFromTrend_ShouldPopulateFromTrend()
    {
        // Arrange
        var trend = new DashboardTrend
        {
            MetricName = "ActiveUsers",
            CurrentValue = 100m,
            PreviousValue = 80m,
            PercentageChange = 25m,
            Trend = "up",
            MeasuredAt = DateTime.UtcNow
        };

        // Act
        var snapshot = DashboardMetricSnapshot.CreateFromTrend(trend);

        // Assert
        Assert.Equal("ActiveUsers", snapshot.MetricType);
        Assert.Equal(100m, snapshot.MetricValue);
        Assert.NotNull(snapshot.MetadataJson);
        Assert.True(snapshot.MetadataJson.ContainsKey("PreviousValue"));
        Assert.True(snapshot.MetadataJson.ContainsKey("PercentageChange"));
    }

    #endregion

    #region AdminDashboardAlert Tests

    [Fact]
    public void CreateAlert_ShouldInitializeProperties()
    {
        // Act
        var alert = new AdminDashboardAlert
        {
            AlertType = "LowInventory",
            Message = "Inventory below threshold",
            Severity = AlertSeverity.Warning
        };

        // Assert
        Assert.NotEqual(Guid.Empty, alert.Id);
        Assert.Equal("LowInventory", alert.AlertType);
        Assert.Equal("Inventory below threshold", alert.Message);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.False(alert.IsResolved);
        Assert.NotEqual(DateTime.MinValue, alert.CreatedAt);
        Assert.Null(alert.ResolvedAt);
    }

    [Fact]
    public void CreateWarning_ShouldSetWarningSeverity()
    {
        // Act
        var alert = AdminDashboardAlert.CreateWarning("TestAlert", "Test message");

        // Assert
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.Equal("TestAlert", alert.AlertType);
    }

    [Fact]
    public void CreateCritical_ShouldSetCriticalSeverity()
    {
        // Act
        var alert = AdminDashboardAlert.CreateCritical("CriticalAlert", "Critical message");

        // Assert
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
    }

    [Fact]
    public void Resolve_ShouldMarkAsResolved()
    {
        // Arrange
        var alert = new AdminDashboardAlert
        {
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
        var alert = new AdminDashboardAlert { AlertType = "Test", Message = "Test" };
        var beforeResolve = DateTime.UtcNow;

        // Act
        alert.Resolve();

        // Assert
        Assert.NotNull(alert.ResolvedAt);
        Assert.True(alert.ResolvedAt >= beforeResolve);
    }

    #endregion

    #region AlertSeverity Tests

    [Fact]
    public void AlertSeverity_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)AlertSeverity.Info);
        Assert.Equal(1, (int)AlertSeverity.Warning);
        Assert.Equal(2, (int)AlertSeverity.Critical);
    }

    #endregion
}
