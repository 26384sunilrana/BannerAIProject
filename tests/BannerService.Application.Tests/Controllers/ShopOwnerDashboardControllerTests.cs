namespace BannerService.Application.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BannerService.Presentation.Controllers;
using Application.Services;
using Application.DTOs;
using Domain.Interfaces;
using Domain.Entities;

public class ShopOwnerDashboardControllerTests
{
    private readonly Mock<IShopOwnerDashboardService> _mockDashboardService;
    private readonly Mock<IShopOwnerDashboardRepository> _mockRepository;
    private readonly Mock<ILogger<ShopOwnerDashboardController>> _mockLogger;
    private readonly ShopOwnerDashboardController _controller;
    private readonly Guid _testShopId;

    public ShopOwnerDashboardControllerTests()
    {
        _mockDashboardService = new Mock<IShopOwnerDashboardService>();
        _mockRepository = new Mock<IShopOwnerDashboardRepository>();
        _mockLogger = new Mock<ILogger<ShopOwnerDashboardController>>();
        _testShopId = Guid.NewGuid();
        _controller = new ShopOwnerDashboardController(_mockDashboardService.Object, _mockRepository.Object, _mockLogger.Object);
    }

    #region GetOverview Tests

    [Fact]
    public async Task GetOverview_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var summary = new ShopDashboardMetrics
        {
            GeneratedAt = DateTime.UtcNow,
            SubscriptionMetrics = new ShopSubscriptionMetrics(),
            RevenueMetrics = new ShopRevenueMetrics(),
            BannerMetrics = new ShopBannerMetrics(),
            UserMetrics = new ShopUserMetrics()
        };

        _mockDashboardService.Setup(s => s.GetDashboardSummaryAsync(_testShopId, false))
            .ReturnsAsync(summary);
        _mockRepository.Setup(r => r.GetActiveAlertsAsync(_testShopId))
            .ReturnsAsync(new List<ShopDashboardAlert>());
        _mockDashboardService.Setup(s => s.GetTopBannersAsync(_testShopId, 5))
            .ReturnsAsync(new List<TopBannerByPerformance>());
        _mockDashboardService.Setup(s => s.GetTrendsAsync(_testShopId, 30))
            .ReturnsAsync(new List<ShopDashboardTrend>());

        // Act
        var result = await _controller.GetOverview(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetSummary Tests

    [Fact]
    public async Task GetSummary_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var summary = new ShopDashboardMetrics
        {
            GeneratedAt = DateTime.UtcNow,
            SubscriptionMetrics = new ShopSubscriptionMetrics(),
            RevenueMetrics = new ShopRevenueMetrics(),
            BannerMetrics = new ShopBannerMetrics(),
            UserMetrics = new ShopUserMetrics()
        };

        _mockDashboardService.Setup(s => s.GetDashboardSummaryAsync(_testShopId))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetSummary(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetSubscriptionMetrics Tests

    [Fact]
    public async Task GetSubscriptionMetrics_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var metrics = new ShopSubscriptionMetrics();

        _mockDashboardService.Setup(s => s.GetSubscriptionMetricsAsync(_testShopId))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetSubscriptionMetrics(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetRevenueMetrics Tests

    [Fact]
    public async Task GetRevenueMetrics_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var metrics = new ShopRevenueMetrics();

        _mockDashboardService.Setup(s => s.GetRevenueMetricsAsync(_testShopId))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetRevenueMetrics(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetBannerMetrics Tests

    [Fact]
    public async Task GetBannerMetrics_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var metrics = new ShopBannerMetrics();

        _mockDashboardService.Setup(s => s.GetBannerMetricsAsync(_testShopId))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetBannerMetrics(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetTrends Tests

    [Fact]
    public async Task GetTrends_WithValidDays_ShouldReturnOk()
    {
        // Arrange
        _mockDashboardService.Setup(s => s.GetTrendsAsync(_testShopId, 30))
            .ReturnsAsync(new List<ShopDashboardTrend>());

        // Act
        var result = await _controller.GetTrends(_testShopId, 30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetTrends_WithZeroDays_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetTrends(_testShopId, 0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetTrends_WithDaysGreaterThan365_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetTrends(_testShopId, 366);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetTopBanners Tests

    [Fact]
    public async Task GetTopBanners_WithValidLimit_ShouldReturnOk()
    {
        // Arrange
        _mockDashboardService.Setup(s => s.GetTopBannersAsync(_testShopId, 10))
            .ReturnsAsync(new List<TopBannerByPerformance>());

        // Act
        var result = await _controller.GetTopBanners(_testShopId, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetTopBanners_WithLimitGreaterThan100_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetTopBanners(_testShopId, 101);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetAlerts Tests

    [Fact]
    public async Task GetAlerts_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var alerts = new List<ShopDashboardAlert>
        {
            new ShopDashboardAlert { Id = Guid.NewGuid(), Message = "Test Alert" }
        };

        _mockRepository.Setup(r => r.GetActiveAlertsAsync(_testShopId))
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetAlerts(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region ResolveAlert Tests

    [Fact]
    public async Task ResolveAlert_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        _mockRepository.Setup(r => r.ResolveAlertAsync(alertId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ResolveAlert(_testShopId, alertId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ResolveAlert_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidAlertId = Guid.NewGuid();
        _mockRepository.Setup(r => r.ResolveAlertAsync(invalidAlertId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ResolveAlert(_testShopId, invalidAlertId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
