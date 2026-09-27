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
using Domain.ValueObjects;

public class AdminDashboardControllerTests
{
    private readonly Mock<IDashboardService> _mockDashboardService;
    private readonly Mock<IAdminDashboardRepository> _mockRepository;
    private readonly Mock<ILogger<AdminDashboardController>> _mockLogger;
    private readonly AdminDashboardController _controller;

    public AdminDashboardControllerTests()
    {
        _mockDashboardService = new Mock<IDashboardService>();
        _mockRepository = new Mock<IAdminDashboardRepository>();
        _mockLogger = new Mock<ILogger<AdminDashboardController>>();
        _controller = new AdminDashboardController(_mockDashboardService.Object, _mockRepository.Object, _mockLogger.Object);
    }

    #region GetOverview Tests

    [Fact]
    public async Task GetOverview_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var summary = new DashboardSummary
        {
            GeneratedAt = DateTime.UtcNow,
            SubscriptionMetrics = new SubscriptionMetrics(),
            RevenueMetrics = new RevenueMetrics(),
            ShopMetrics = new ShopMetrics(),
            UserMetrics = new UserMetrics(),
            BannerMetrics = new BannerMetrics(),
            SystemHealth = new SystemHealthMetrics()
        };

        _mockDashboardService.Setup(s => s.GetDashboardSummaryAsync(false))
            .ReturnsAsync(summary);
        _mockRepository.Setup(r => r.GetActiveAlertsAsync())
            .ReturnsAsync(new List<AdminDashboardAlert>());
        _mockDashboardService.Setup(s => s.GetTopShopsByRevenueAsync(5))
            .ReturnsAsync(new List<TopShopByRevenue>());
        _mockDashboardService.Setup(s => s.GetTrendsAsync(30))
            .ReturnsAsync(new List<DashboardTrend>());

        // Act
        var result = await _controller.GetOverview();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetSummary Tests

    [Fact]
    public async Task GetSummary_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var summary = new DashboardSummary
        {
            GeneratedAt = DateTime.UtcNow,
            SubscriptionMetrics = new SubscriptionMetrics(),
            RevenueMetrics = new RevenueMetrics(),
            ShopMetrics = new ShopMetrics(),
            UserMetrics = new UserMetrics(),
            BannerMetrics = new BannerMetrics(),
            SystemHealth = new SystemHealthMetrics()
        };

        _mockDashboardService.Setup(s => s.GetDashboardSummaryAsync())
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetSummary();

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
        _mockDashboardService.Setup(s => s.GetTrendsAsync(30))
            .ReturnsAsync(new List<DashboardTrend>());

        // Act
        var result = await _controller.GetTrends(30);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetTrends_WithZeroDays_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetTrends(0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetTrends_WithDaysGreaterThan365_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetTrends(366);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetTopShops Tests

    [Fact]
    public async Task GetTopShops_WithValidLimit_ShouldReturnOk()
    {
        // Arrange
        _mockDashboardService.Setup(s => s.GetTopShopsByRevenueAsync(10))
            .ReturnsAsync(new List<TopShopByRevenue>());

        // Act
        var result = await _controller.GetTopShops(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetTopShops_WithLimitExceeding100_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetTopShops(101);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetTopShops_WithZeroLimit_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetTopShops(0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetAlerts Tests

    [Fact]
    public async Task GetAlerts_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var alerts = new List<AdminDashboardAlert>
        {
            new AdminDashboardAlert { Id = Guid.NewGuid(), Message = "Test Alert" }
        };

        _mockRepository.Setup(r => r.GetActiveAlertsAsync())
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetAlerts();

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
        var result = await _controller.ResolveAlert(alertId);

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
        var result = await _controller.ResolveAlert(invalidAlertId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
