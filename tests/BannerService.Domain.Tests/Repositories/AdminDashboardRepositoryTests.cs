namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class AdminDashboardRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private AdminDashboardRepository _repository;

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new AdminDashboardRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var dashboard = new AdminDashboard
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Set<AdminDashboard>().Add(dashboard);

        var snapshot1 = new DashboardMetricSnapshot
        {
            MetricType = "TotalRevenue",
            MetricValue = 1000m,
            SnapshotDate = DateTime.UtcNow.AddDays(-2)
        };

        var snapshot2 = new DashboardMetricSnapshot
        {
            MetricType = "TotalRevenue",
            MetricValue = 1500m,
            SnapshotDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.Set<DashboardMetricSnapshot>().AddRange(snapshot1, snapshot2);

        var alert1 = AdminDashboardAlert.CreateWarning("LowInventory", "Inventory below threshold");
        var alert2 = AdminDashboardAlert.CreateCritical("SystemError", "Critical system error detected");

        _context.Set<AdminDashboardAlert>().AddRange(alert1, alert2);

        await _context.SaveChangesAsync();
    }

    #region GetCurrentAsync Tests

    [Fact]
    public async Task GetCurrentAsync_WithExistingDashboard_ShouldReturnLatest()
    {
        // Act
        var result = await _repository.GetCurrentAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task GetCurrentAsync_WithoutDashboard_ShouldReturnNull()
    {
        // Arrange
        var emptyContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);
        var emptyRepository = new AdminDashboardRepository(emptyContext);

        // Act
        var result = await emptyRepository.GetCurrentAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldReturnMostRecentlyUpdated()
    {
        // Arrange
        var newDashboard = new AdminDashboard { Id = Guid.NewGuid() };
        System.Threading.Thread.Sleep(10);
        _context.Set<AdminDashboard>().Add(newDashboard);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCurrentAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newDashboard.Id, result.Id);
    }

    #endregion

    #region CreateOrUpdateAsync Tests

    [Fact]
    public async Task CreateOrUpdateAsync_WithNewDashboard_ShouldCreate()
    {
        // Arrange
        var newDashboard = new AdminDashboard { Id = Guid.NewGuid() };

        // Act
        var result = await _repository.CreateOrUpdateAsync(newDashboard);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<AdminDashboard>().FindAsync(newDashboard.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithExistingDashboard_ShouldUpdate()
    {
        // Arrange
        var existing = await _repository.GetCurrentAsync();
        var updatedSummary = new DashboardSummary { TotalRevenue = 5000m };
        var updateDashboard = new AdminDashboard { CurrentSummary = updatedSummary };

        // Act
        var result = await _repository.CreateOrUpdateAsync(updateDashboard);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedSummary.TotalRevenue, result.CurrentSummary.TotalRevenue);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        var dashboard = new AdminDashboard { Id = Guid.NewGuid() };

        // Act
        var result = await _repository.CreateOrUpdateAsync(dashboard);

        // Assert
        Assert.NotEqual(DateTime.MinValue, result.LastUpdatedAt);
    }

    #endregion

    #region GetMetricSnapshotsAsync Tests

    [Fact]
    public async Task GetMetricSnapshotsAsync_WithValidMetricType_ShouldReturnSnapshots()
    {
        // Act
        var results = await _repository.GetMetricSnapshotsAsync("TotalRevenue");

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal("TotalRevenue", s.MetricType));
    }

    [Fact]
    public async Task GetMetricSnapshotsAsync_WithInvalidMetricType_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetMetricSnapshotsAsync("NonExistentMetric");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetMetricSnapshotsAsync_ShouldFilterByDays()
    {
        // Act
        var results = await _repository.GetMetricSnapshotsAsync("TotalRevenue", days: 1);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s =>
        {
            var daysDiff = DateTime.UtcNow.Subtract(s.SnapshotDate).TotalDays;
            Assert.True(daysDiff <= 1);
        });
    }

    [Fact]
    public async Task GetMetricSnapshotsAsync_ShouldReturnOrderedByDate()
    {
        // Act
        var results = await _repository.GetMetricSnapshotsAsync("TotalRevenue");

        // Assert
        Assert.True(results.SequenceEqual(results.OrderBy(s => s.SnapshotDate)));
    }

    #endregion

    #region CreateSnapshotAsync Tests

    [Fact]
    public async Task CreateSnapshotAsync_WithValidSnapshot_ShouldAdd()
    {
        // Arrange
        var snapshot = new DashboardMetricSnapshot
        {
            MetricType = "ActiveUsers",
            MetricValue = 100m
        };

        // Act
        var result = await _repository.CreateSnapshotAsync(snapshot);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<DashboardMetricSnapshot>().FindAsync(snapshot.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateSnapshotAsync_ShouldSetSnapshotDate()
    {
        // Arrange
        var snapshot = new DashboardMetricSnapshot { MetricType = "Test" };

        // Act
        var result = await _repository.CreateSnapshotAsync(snapshot);

        // Assert
        Assert.NotEqual(DateTime.MinValue, result.SnapshotDate);
    }

    #endregion

    #region GetActiveAlertsAsync Tests

    [Fact]
    public async Task GetActiveAlertsAsync_ShouldReturnUnresolvedAlerts()
    {
        // Act
        var results = await _repository.GetActiveAlertsAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, a => Assert.False(a.IsResolved));
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ShouldOrderByCreatedDate()
    {
        // Act
        var results = await _repository.GetActiveAlertsAsync();

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(a => a.CreatedAt)));
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ShouldNotIncludeResolved()
    {
        // Arrange
        var alert = await _context.Set<AdminDashboardAlert>().FirstAsync();
        alert.Resolve();
        _context.Set<AdminDashboardAlert>().Update(alert);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetActiveAlertsAsync();

        // Assert
        Assert.DoesNotContain(alert, results);
    }

    #endregion

    #region CreateAlertAsync Tests

    [Fact]
    public async Task CreateAlertAsync_WithValidAlert_ShouldAdd()
    {
        // Arrange
        var alert = AdminDashboardAlert.CreateWarning("TestAlert", "Test message");

        // Act
        var result = await _repository.CreateAlertAsync(alert);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<AdminDashboardAlert>().FindAsync(alert.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateAlertAsync_ShouldSetCreatedAt()
    {
        // Arrange
        var alert = new AdminDashboardAlert { AlertType = "Test", Message = "Test" };

        // Act
        var result = await _repository.CreateAlertAsync(alert);

        // Assert
        Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
    }

    #endregion

    #region ResolveAlertAsync Tests

    [Fact]
    public async Task ResolveAlertAsync_WithValidId_ShouldResolve()
    {
        // Arrange
        var alert = await _context.Set<AdminDashboardAlert>().FirstAsync();

        // Act
        var result = await _repository.ResolveAlertAsync(alert.Id);

        // Assert
        Assert.True(result);
        var updated = await _context.Set<AdminDashboardAlert>().FindAsync(alert.Id);
        Assert.True(updated.IsResolved);
    }

    [Fact]
    public async Task ResolveAlertAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ResolveAlertAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ResolveAlertAsync_ShouldSetResolvedAt()
    {
        // Arrange
        var alert = await _context.Set<AdminDashboardAlert>().FirstAsync();

        // Act
        await _repository.ResolveAlertAsync(alert.Id);

        // Assert
        var updated = await _context.Set<AdminDashboardAlert>().FindAsync(alert.Id);
        Assert.NotNull(updated.ResolvedAt);
    }

    #endregion

    #region GetAlertsAsync Tests

    [Fact]
    public async Task GetAlertsAsync_WithValidDateRange_ShouldReturnAlerts()
    {
        // Act
        var results = await _repository.GetAlertsAsync(DateTime.UtcNow.AddDays(-2));

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetAlertsAsync_WithFutureDateRange_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetAlertsAsync(DateTime.UtcNow.AddDays(1));

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAlertsAsync_ShouldRespectLimit()
    {
        // Act
        var results = await _repository.GetAlertsAsync(DateTime.UtcNow.AddDays(-10), limit: 1);

        // Assert
        Assert.Single(results);
    }

    #endregion
}
