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

public class ShopOwnerDashboardRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private ShopOwnerDashboardRepository _repository;
    private readonly Guid _testShopId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new ShopOwnerDashboardRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var dashboard = new ShopOwnerDashboard
        {
            Id = Guid.NewGuid(),
            ShopId = _testShopId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Set<ShopOwnerDashboard>().Add(dashboard);

        var snapshot1 = new ShopDashboardMetricSnapshot
        {
            ShopId = _testShopId,
            MetricType = "Revenue",
            MetricValue = 2000m,
            SnapshotDate = DateTime.UtcNow.AddDays(-2)
        };

        var snapshot2 = new ShopDashboardMetricSnapshot
        {
            ShopId = _testShopId,
            MetricType = "Revenue",
            MetricValue = 2500m,
            SnapshotDate = DateTime.UtcNow.AddDays(-1)
        };

        _context.Set<ShopDashboardMetricSnapshot>().AddRange(snapshot1, snapshot2);

        var alert1 = new ShopDashboardAlert
        {
            ShopId = _testShopId,
            AlertType = "LowInventory",
            Message = "Inventory below threshold",
            Severity = AlertSeverity.Warning,
            IsResolved = false
        };

        var alert2 = new ShopDashboardAlert
        {
            ShopId = _testShopId,
            AlertType = "PaymentFailed",
            Message = "Recent payment failed",
            Severity = AlertSeverity.Critical,
            IsResolved = false
        };

        _context.Set<ShopDashboardAlert>().AddRange(alert1, alert2);

        await _context.SaveChangesAsync();
    }

    #region GetCurrentAsync Tests

    [Fact]
    public async Task GetCurrentAsync_WithValidShopId_ShouldReturnLatestDashboard()
    {
        // Act
        var result = await _repository.GetCurrentAsync(_testShopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testShopId, result.ShopId);
    }

    [Fact]
    public async Task GetCurrentAsync_WithInvalidShopId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetCurrentAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldReturnMostRecentlyUpdated()
    {
        // Arrange
        var newDashboard = new ShopOwnerDashboard { ShopId = _testShopId };
        System.Threading.Thread.Sleep(10);
        _context.Set<ShopOwnerDashboard>().Add(newDashboard);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCurrentAsync(_testShopId);

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
        var newDashboard = new ShopOwnerDashboard { ShopId = Guid.NewGuid() };

        // Act
        var result = await _repository.CreateOrUpdateAsync(newDashboard);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<ShopOwnerDashboard>().FindAsync(newDashboard.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithExistingDashboard_ShouldUpdate()
    {
        // Arrange
        var existing = await _repository.GetCurrentAsync(_testShopId);
        var newSummary = new ShopDashboardSummary { TotalRevenue = 5000m };
        var updateDashboard = new ShopOwnerDashboard
        {
            ShopId = _testShopId,
            CurrentSummary = newSummary
        };

        // Act
        var result = await _repository.CreateOrUpdateAsync(updateDashboard);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newSummary.TotalRevenue, result.CurrentSummary.TotalRevenue);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        var dashboard = new ShopOwnerDashboard { ShopId = Guid.NewGuid() };

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
        var results = await _repository.GetMetricSnapshotsAsync(_testShopId, "Revenue");

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, s => Assert.Equal("Revenue", s.MetricType));
    }

    [Fact]
    public async Task GetMetricSnapshotsAsync_WithInvalidMetricType_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetMetricSnapshotsAsync(_testShopId, "NonExistent");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetMetricSnapshotsAsync_ShouldFilterByShopId()
    {
        // Act
        var results = await _repository.GetMetricSnapshotsAsync(_testShopId, "Revenue");

        // Assert
        Assert.All(results, s => Assert.Equal(_testShopId, s.ShopId));
    }

    [Fact]
    public async Task GetMetricSnapshotsAsync_ShouldFilterByDays()
    {
        // Act
        var results = await _repository.GetMetricSnapshotsAsync(_testShopId, "Revenue", days: 1);

        // Assert
        Assert.All(results, s =>
        {
            var daysDiff = DateTime.UtcNow.Subtract(s.SnapshotDate).TotalDays;
            Assert.True(daysDiff <= 1);
        });
    }

    #endregion

    #region CreateSnapshotAsync Tests

    [Fact]
    public async Task CreateSnapshotAsync_WithValidSnapshot_ShouldAdd()
    {
        // Arrange
        var snapshot = new ShopDashboardMetricSnapshot
        {
            ShopId = _testShopId,
            MetricType = "Engagement",
            MetricValue = 150m
        };

        // Act
        var result = await _repository.CreateSnapshotAsync(snapshot);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<ShopDashboardMetricSnapshot>().FindAsync(snapshot.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateSnapshotAsync_ShouldSetSnapshotDate()
    {
        // Arrange
        var snapshot = new ShopDashboardMetricSnapshot
        {
            ShopId = _testShopId,
            MetricType = "Test"
        };

        // Act
        var result = await _repository.CreateSnapshotAsync(snapshot);

        // Assert
        Assert.NotEqual(DateTime.MinValue, result.SnapshotDate);
    }

    #endregion

    #region GetActiveAlertsAsync Tests

    [Fact]
    public async Task GetActiveAlertsAsync_WithValidShopId_ShouldReturnUnresolvedAlerts()
    {
        // Act
        var results = await _repository.GetActiveAlertsAsync(_testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, a => Assert.False(a.IsResolved));
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ShouldFilterByShopId()
    {
        // Act
        var results = await _repository.GetActiveAlertsAsync(_testShopId);

        // Assert
        Assert.All(results, a => Assert.Equal(_testShopId, a.ShopId));
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ShouldOrderByCreatedDateDescending()
    {
        // Act
        var results = await _repository.GetActiveAlertsAsync(_testShopId);

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(a => a.CreatedAt)));
    }

    [Fact]
    public async Task GetActiveAlertsAsync_WithInvalidShopId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetActiveAlertsAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region CreateAlertAsync Tests

    [Fact]
    public async Task CreateAlertAsync_WithValidAlert_ShouldAdd()
    {
        // Arrange
        var alert = new ShopDashboardAlert
        {
            ShopId = _testShopId,
            AlertType = "TestAlert",
            Message = "Test message",
            Severity = AlertSeverity.Info
        };

        // Act
        var result = await _repository.CreateAlertAsync(alert);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<ShopDashboardAlert>().FindAsync(alert.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task CreateAlertAsync_ShouldSetCreatedAt()
    {
        // Arrange
        var alert = new ShopDashboardAlert
        {
            ShopId = _testShopId,
            AlertType = "Test",
            Message = "Test"
        };

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
        var alert = await _context.Set<ShopDashboardAlert>().FirstAsync();

        // Act
        var result = await _repository.ResolveAlertAsync(alert.Id);

        // Assert
        Assert.True(result);
        var updated = await _context.Set<ShopDashboardAlert>().FindAsync(alert.Id);
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
        var alert = await _context.Set<ShopDashboardAlert>().FirstAsync();

        // Act
        await _repository.ResolveAlertAsync(alert.Id);

        // Assert
        var updated = await _context.Set<ShopDashboardAlert>().FindAsync(alert.Id);
        Assert.NotNull(updated.ResolvedAt);
    }

    #endregion

    #region GetAlertsAsync Tests

    [Fact]
    public async Task GetAlertsAsync_WithValidDateRange_ShouldReturnAlerts()
    {
        // Act
        var results = await _repository.GetAlertsAsync(_testShopId, DateTime.UtcNow.AddDays(-2));

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetAlertsAsync_ShouldFilterByShopId()
    {
        // Act
        var results = await _repository.GetAlertsAsync(_testShopId, DateTime.UtcNow.AddDays(-10));

        // Assert
        Assert.All(results, a => Assert.Equal(_testShopId, a.ShopId));
    }

    [Fact]
    public async Task GetAlertsAsync_WithFutureDateRange_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetAlertsAsync(_testShopId, DateTime.UtcNow.AddDays(1));

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAlertsAsync_ShouldRespectLimit()
    {
        // Act
        var results = await _repository.GetAlertsAsync(_testShopId, DateTime.UtcNow.AddDays(-10), limit: 1);

        // Assert
        Assert.Single(results);
    }

    #endregion
}
