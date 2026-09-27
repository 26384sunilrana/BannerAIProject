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

public class AnalyticsRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private AnalyticsRepository _repository;
    private readonly Guid _testShopId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new AnalyticsRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var report1 = new DashboardReport
        {
            ShopId = _testShopId,
            Type = ReportType.Revenue,
            Status = ReportStatus.Generated,
            Title = "Revenue Report",
            Data = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var report2 = new DashboardReport
        {
            ShopId = _testShopId,
            Type = ReportType.Performance,
            Status = ReportStatus.Generated,
            Title = "Performance Report",
            Data = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Set<DashboardReport>().AddRange(report1, report2);

        var evt1 = new AnalyticsEvent
        {
            ShopId = _testShopId,
            UserId = _testUserId,
            EventType = EventType.BannerCreated,
            ResourceId = Guid.NewGuid(),
            ResourceType = "Banner",
            OccurredAt = DateTime.UtcNow.AddDays(-1)
        };

        var evt2 = new AnalyticsEvent
        {
            ShopId = _testShopId,
            UserId = _testUserId,
            EventType = EventType.BannerPublished,
            ResourceId = Guid.NewGuid(),
            ResourceType = "Banner",
            OccurredAt = DateTime.UtcNow
        };

        _context.Set<AnalyticsEvent>().AddRange(evt1, evt2);

        await _context.SaveChangesAsync();
    }

    #region DashboardReport Tests

    [Fact]
    public async Task GetReportByIdAsync_WithValidId_ShouldReturnReport()
    {
        // Arrange
        var report = _context.Set<DashboardReport>().First();

        // Act
        var result = await _repository.GetReportByIdAsync(report.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(report.Id, result.Id);
    }

    [Fact]
    public async Task GetReportByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetReportByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetShopReportsAsync_WithValidShopId_ShouldReturnReports()
    {
        // Act
        var results = await _repository.GetShopReportsAsync(_testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(_testShopId, r.ShopId));
    }

    [Fact]
    public async Task GetShopReportsAsync_WithInvalidShopId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetShopReportsAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetReportsByTypeAsync_WithValidType_ShouldReturnReports()
    {
        // Act
        var results = await _repository.GetReportsByTypeAsync(ReportType.Revenue);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(ReportType.Revenue, r.Type));
    }

    [Fact]
    public async Task GetReportsByDateRangeAsync_WithValidRange_ShouldReturnReports()
    {
        // Act
        var results = await _repository.GetReportsByDateRangeAsync(
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow
        );

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetActiveReportsAsync_ShouldReturnNonArchivedReports()
    {
        // Act
        var results = await _repository.GetActiveReportsAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotEqual(ReportStatus.Archived, r.Status));
    }

    [Fact]
    public async Task CreateReportAsync_WithValidReport_ShouldAdd()
    {
        // Arrange
        var report = new DashboardReport
        {
            ShopId = _testShopId,
            Type = ReportType.Engagement,
            Status = ReportStatus.Generated,
            Title = "New Report",
            Data = "{}"
        };

        // Act
        var result = await _repository.CreateReportAsync(report);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<DashboardReport>().FindAsync(report.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task UpdateReportAsync_WithValidReport_ShouldUpdate()
    {
        // Arrange
        var report = _context.Set<DashboardReport>().First();
        report.Title = "Updated Title";

        // Act
        await _repository.UpdateReportAsync(report);

        // Assert
        var updated = await _context.Set<DashboardReport>().FindAsync(report.Id);
        Assert.Equal("Updated Title", updated.Title);
    }

    [Fact]
    public async Task DeleteReportAsync_WithValidId_ShouldRemove()
    {
        // Arrange
        var report = _context.Set<DashboardReport>().First();

        // Act
        var result = await _repository.DeleteReportAsync(report.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Set<DashboardReport>().FindAsync(report.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteReportAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteReportAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetReportsByStatusAsync_WithValidStatus_ShouldReturnReports()
    {
        // Act
        var results = await _repository.GetReportsByStatusAsync(ReportStatus.Generated);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(ReportStatus.Generated, r.Status));
    }

    [Fact]
    public async Task GetExpiringReportsAsync_ShouldReturnOldArchivedReports()
    {
        // Arrange
        var oldReport = new DashboardReport
        {
            ShopId = _testShopId,
            Type = ReportType.Revenue,
            Status = ReportStatus.Archived,
            Title = "Old Report",
            Data = "{}",
            CreatedAt = DateTime.UtcNow.AddDays(-200)
        };

        _context.Set<DashboardReport>().Add(oldReport);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetExpiringReportsAsync(retentionDays: 90);

        // Assert
        Assert.NotEmpty(results);
    }

    #endregion

    #region AnalyticsEvent Tests

    [Fact]
    public async Task CreateEventAsync_WithValidEvent_ShouldAdd()
    {
        // Arrange
        var evt = new AnalyticsEvent
        {
            ShopId = _testShopId,
            UserId = _testUserId,
            EventType = EventType.BannerDeleted,
            ResourceId = Guid.NewGuid(),
            ResourceType = "Banner"
        };

        // Act
        var result = await _repository.CreateEventAsync(evt);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<AnalyticsEvent>().FindAsync(evt.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetEventsByShopAsync_WithValidShopId_ShouldReturnEvents()
    {
        // Act
        var results = await _repository.GetEventsByShopAsync(_testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, e => Assert.Equal(_testShopId, e.ShopId));
    }

    [Fact]
    public async Task GetEventsByShopAsync_WithDateRange_ShouldFilterByDate()
    {
        // Act
        var results = await _repository.GetEventsByShopAsync(
            _testShopId,
            startDate: DateTime.UtcNow.AddDays(-1),
            endDate: DateTime.UtcNow
        );

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetEventsByTypeAsync_WithValidType_ShouldReturnEvents()
    {
        // Act
        var results = await _repository.GetEventsByTypeAsync(EventType.BannerCreated);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, e => Assert.Equal(EventType.BannerCreated, e.EventType));
    }

    [Fact]
    public async Task GetEventsByResourceAsync_WithValidResource_ShouldReturnEvents()
    {
        // Arrange
        var evt = _context.Set<AnalyticsEvent>().First();

        // Act
        var results = await _repository.GetEventsByResourceAsync(evt.ResourceId, evt.ResourceType);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetEventsByUserAsync_WithValidUserId_ShouldReturnEvents()
    {
        // Act
        var results = await _repository.GetEventsByUserAsync(_testUserId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, e => Assert.Equal(_testUserId, e.UserId));
    }

    [Fact]
    public async Task GetEventsByDateRangeAsync_WithValidRange_ShouldReturnEvents()
    {
        // Act
        var results = await _repository.GetEventsByDateRangeAsync(
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow
        );

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetEventCountAsync_WithValidShopId_ShouldReturnCount()
    {
        // Act
        var count = await _repository.GetEventCountAsync(_testShopId);

        // Assert
        Assert.True(count > 0);
    }

    [Fact]
    public async Task GetEventCountByTypeAsync_WithValidType_ShouldReturnCount()
    {
        // Act
        var count = await _repository.GetEventCountByTypeAsync(EventType.BannerCreated);

        // Assert
        Assert.True(count > 0);
    }

    [Fact]
    public async Task DeleteOldEventsAsync_ShouldRemoveOldEvents()
    {
        // Arrange
        var oldEvent = new AnalyticsEvent
        {
            ShopId = _testShopId,
            UserId = _testUserId,
            EventType = EventType.BannerCreated,
            ResourceId = Guid.NewGuid(),
            ResourceType = "Banner",
            CreatedAt = DateTime.UtcNow.AddYears(-2)
        };

        _context.Set<AnalyticsEvent>().Add(oldEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteOldEventsAsync(daysToKeep: 365);

        // Assert
        Assert.True(result);
    }

    #endregion
}
