namespace BannerService.Domain.Tests.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;

public class DashboardReportEntityTests
{
    private readonly Guid _testShopId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    #region Construction Tests

    [Fact]
    public void CreateReport_ShouldInitializeProperties()
    {
        // Act
        var report = new DashboardReport
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Type = ReportType.Revenue,
            Title = "Revenue Report",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, report.Id);
        Assert.Equal(_testShopId, report.ShopId);
        Assert.Equal(_testUserId, report.CreatedByUserId);
        Assert.Equal(ReportType.Revenue, report.Type);
        Assert.Equal("Revenue Report", report.Title);
        Assert.Equal(ReportStatus.Draft, report.Status);
        Assert.NotEqual(DateTime.MinValue, report.CreatedAt);
    }

    [Fact]
    public void CreateReport_ShouldHaveEmptyMetrics()
    {
        // Act
        var report = new DashboardReport { ShopId = _testShopId };

        // Assert
        Assert.NotNull(report.Metrics);
        Assert.Empty(report.Metrics);
    }

    [Fact]
    public void CreateReport_ShouldHaveEmptyComparisonData()
    {
        // Act
        var report = new DashboardReport { ShopId = _testShopId };

        // Assert
        Assert.NotNull(report.ComparisonData);
        Assert.Empty(report.ComparisonData);
    }

    [Fact]
    public void CreateReport_ShouldHaveEmptyTrendData()
    {
        // Act
        var report = new DashboardReport { ShopId = _testShopId };

        // Assert
        Assert.NotNull(report.TrendData);
        Assert.Empty(report.TrendData);
    }

    #endregion

    #region Generate Tests

    [Fact]
    public void Generate_ShouldSetGeneratedStatus()
    {
        // Arrange
        var report = new DashboardReport
        {
            ShopId = _testShopId,
            Status = ReportStatus.Draft
        };

        // Act
        report.Generate();

        // Assert
        Assert.Equal(ReportStatus.Generated, report.Status);
    }

    [Fact]
    public void Generate_ShouldSetGeneratedAtTimestamp()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        var beforeGenerate = DateTime.UtcNow;

        // Act
        report.Generate();

        // Assert
        Assert.NotNull(report.GeneratedAt);
        Assert.True(report.GeneratedAt >= beforeGenerate);
    }

    [Fact]
    public void Generate_ShouldUpdateModificationTime()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        var originalUpdateTime = report.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        report.Generate();

        // Assert
        Assert.True(report.UpdatedAt > originalUpdateTime);
    }

    #endregion

    #region Export Tests

    [Fact]
    public void Export_WithGeneratedStatus_ShouldSetExported()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        report.Generate();

        // Act
        report.Export("PDF", "https://example.com/report.pdf");

        // Assert
        Assert.Equal(ReportStatus.Exported, report.Status);
        Assert.Equal("PDF", report.ExportFormat);
        Assert.Equal("https://example.com/report.pdf", report.ExportUrl);
        Assert.NotNull(report.ExportedAt);
    }

    [Fact]
    public void Export_WithNonGeneratedStatus_ShouldThrowException()
    {
        // Arrange
        var report = new DashboardReport
        {
            ShopId = _testShopId,
            Status = ReportStatus.Draft
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            report.Export("PDF", "https://example.com/report.pdf")
        );
    }

    [Fact]
    public void Export_ShouldSetExportedAtTimestamp()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        report.Generate();
        var beforeExport = DateTime.UtcNow;

        // Act
        report.Export("Excel", "https://example.com/report.xlsx");

        // Assert
        Assert.NotNull(report.ExportedAt);
        Assert.True(report.ExportedAt >= beforeExport);
    }

    #endregion

    #region Archive Tests

    [Fact]
    public void Archive_ShouldSetArchivedStatus()
    {
        // Arrange
        var report = new DashboardReport
        {
            ShopId = _testShopId,
            Status = ReportStatus.Generated
        };

        // Act
        report.Archive();

        // Assert
        Assert.Equal(ReportStatus.Archived, report.Status);
    }

    [Fact]
    public void Archive_ShouldUpdateModificationTime()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        var originalUpdateTime = report.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        report.Archive();

        // Assert
        Assert.True(report.UpdatedAt > originalUpdateTime);
    }

    #endregion

    #region Metrics Calculation Tests

    [Fact]
    public void GetAverageMetricValue_WithMetrics_ShouldCalculateAverage()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        var metric1 = new ReportMetric { MetricName = "Revenue", CurrentValue = 100m };
        var metric2 = new ReportMetric { MetricName = "Orders", CurrentValue = 200m };
        report.Metrics.AddRange(new[] { metric1, metric2 });

        // Act
        var average = report.GetAverageMetricValue();

        // Assert
        Assert.Equal(150m, average);
    }

    [Fact]
    public void GetAverageMetricValue_WithoutMetrics_ShouldReturnZero()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };

        // Act
        var average = report.GetAverageMetricValue();

        // Assert
        Assert.Equal(0m, average);
    }

    [Fact]
    public void GetTotalMetricValue_ShouldSumAllMetrics()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        report.Metrics.Add(new ReportMetric { MetricName = "Revenue", CurrentValue = 500m });
        report.Metrics.Add(new ReportMetric { MetricName = "Orders", CurrentValue = 300m });

        // Act
        var total = report.GetTotalMetricValue();

        // Assert
        Assert.Equal(800m, total);
    }

    [Fact]
    public void GetMetricsByTrend_ShouldReturnFilteredMetrics()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        var upMetric = new ReportMetric { MetricName = "Revenue", CurrentValue = 200m, PreviousValue = 100m };
        var downMetric = new ReportMetric { MetricName = "Expenses", CurrentValue = 50m, PreviousValue = 100m };
        report.Metrics.AddRange(new[] { upMetric, downMetric });

        // Act
        var upMetrics = report.GetMetricsByTrend(TrendDirection.Up);

        // Assert
        Assert.NotEmpty(upMetrics);
        Assert.All(upMetrics, m => Assert.Equal(TrendDirection.Up, m.GetTrend()));
    }

    #endregion

    #region Expiration Tests

    [Fact]
    public void IsExpired_WithOldReport_ShouldReturnTrue()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };
        var oldDate = DateTime.UtcNow.AddDays(-200);
        var reflectionProperty = report.GetType().GetProperty("CreatedAt");
        reflectionProperty?.SetValue(report, oldDate);

        // Act
        var result = report.IsExpired(retentionDays: 90);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpired_WithRecentReport_ShouldReturnFalse()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };

        // Act
        var result = report.IsExpired(retentionDays: 90);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpired_WithDefaultRetentionDays_ShouldUse90Days()
    {
        // Arrange
        var report = new DashboardReport { ShopId = _testShopId };

        // Act
        var result = report.IsExpired(); // Should use default 90 days

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ReportType Tests

    [Fact]
    public void ReportType_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ReportType.Summary);
        Assert.Equal(1, (int)ReportType.Subscription);
        Assert.Equal(2, (int)ReportType.Revenue);
        Assert.Equal(3, (int)ReportType.Banner);
        Assert.Equal(4, (int)ReportType.Advertisement);
        Assert.Equal(5, (int)ReportType.Engagement);
        Assert.Equal(6, (int)ReportType.Performance);
        Assert.Equal(7, (int)ReportType.Comparison);
    }

    #endregion

    #region ReportStatus Tests

    [Fact]
    public void ReportStatus_ShouldHaveExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ReportStatus.Draft);
        Assert.Equal(1, (int)ReportStatus.Generated);
        Assert.Equal(2, (int)ReportStatus.Exported);
        Assert.Equal(3, (int)ReportStatus.Archived);
    }

    #endregion
}
