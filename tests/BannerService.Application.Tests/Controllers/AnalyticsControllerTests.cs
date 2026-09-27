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

public class AnalyticsControllerTests
{
    private readonly Mock<IAnalyticsService> _mockAnalyticsService;
    private readonly Mock<IAnalyticsRepository> _mockRepository;
    private readonly Mock<ILogger<AnalyticsController>> _mockLogger;
    private readonly AnalyticsController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testUserId;
    private readonly Guid _testReportId;

    public AnalyticsControllerTests()
    {
        _mockAnalyticsService = new Mock<IAnalyticsService>();
        _mockRepository = new Mock<IAnalyticsRepository>();
        _mockLogger = new Mock<ILogger<AnalyticsController>>();
        _testShopId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();
        _testReportId = Guid.NewGuid();
        _controller = new AnalyticsController(_mockAnalyticsService.Object, _mockRepository.Object, _mockLogger.Object);
    }

    #region GenerateReport Tests

    [Fact]
    public async Task GenerateReport_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new GenerateReportDto
        {
            ReportType = "Summary",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        var report = new DashboardReport
        {
            Id = _testReportId,
            ShopId = _testShopId,
            Type = ReportType.Summary
        };

        _mockAnalyticsService.Setup(s => s.GenerateReportAsync(
            _testShopId, ReportType.Summary, request.StartDate, request.EndDate, _testUserId))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GenerateReport(_testShopId, request, _testUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GenerateReport_WithInvalidType_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new GenerateReportDto
        {
            ReportType = "InvalidType",
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        // Act
        var result = await _controller.GenerateReport(_testShopId, request, _testUserId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetReport Tests

    [Fact]
    public async Task GetReport_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var report = new DashboardReport
        {
            Id = _testReportId,
            ShopId = _testShopId,
            Type = ReportType.Summary
        };

        _mockAnalyticsService.Setup(s => s.GetReportAsync(_testReportId))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetReport(_testReportId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetReport_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidReportId = Guid.NewGuid();
        _mockAnalyticsService.Setup(s => s.GetReportAsync(invalidReportId))
            .ThrowsAsync(new KeyNotFoundException("Report not found"));

        // Act
        var result = await _controller.GetReport(invalidReportId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetShopReports Tests

    [Fact]
    public async Task GetShopReports_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var reports = new List<DashboardReport>
        {
            new DashboardReport { Id = Guid.NewGuid(), ShopId = _testShopId },
            new DashboardReport { Id = Guid.NewGuid(), ShopId = _testShopId }
        };

        _mockAnalyticsService.Setup(s => s.GetShopReportsAsync(_testShopId))
            .ReturnsAsync(reports);

        // Act
        var result = await _controller.GetShopReports(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetShopReports_WithNoReports_ShouldReturnEmptyList()
    {
        // Arrange
        _mockAnalyticsService.Setup(s => s.GetShopReportsAsync(_testShopId))
            .ReturnsAsync(new List<DashboardReport>());

        // Act
        var result = await _controller.GetShopReports(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region RecordEvent Tests

    [Fact]
    public async Task RecordEvent_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new RecordEventDto
        {
            EventType = "Click",
            UserId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            ResourceType = "Banner"
        };

        _mockAnalyticsService.Setup(s => s.RecordEventAsync(
            _testShopId, EventType.Click, request.UserId, request.ResourceId, request.ResourceType, It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RecordEvent(_testShopId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task RecordEvent_WithInvalidType_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RecordEventDto
        {
            EventType = "InvalidType",
            UserId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            ResourceType = "Banner"
        };

        // Act
        var result = await _controller.RecordEvent(_testShopId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetEvents Tests

    [Fact]
    public async Task GetEvents_WithValidDates_ShouldReturnOk()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var events = new List<AnalyticsEvent>
        {
            new AnalyticsEvent { Id = Guid.NewGuid(), ShopId = _testShopId },
            new AnalyticsEvent { Id = Guid.NewGuid(), ShopId = _testShopId }
        };

        _mockAnalyticsService.Setup(s => s.GetEventsAsync(_testShopId, startDate, endDate))
            .ReturnsAsync(events);

        // Act
        var result = await _controller.GetEvents(_testShopId, startDate, endDate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region DeleteReport Tests

    [Fact]
    public async Task DeleteReport_WithValidId_ShouldReturnOk()
    {
        // Arrange
        _mockAnalyticsService.Setup(s => s.DeleteReportAsync(_testReportId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteReport(_testReportId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeleteReport_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidReportId = Guid.NewGuid();
        _mockAnalyticsService.Setup(s => s.DeleteReportAsync(invalidReportId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteReport(invalidReportId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion
}
