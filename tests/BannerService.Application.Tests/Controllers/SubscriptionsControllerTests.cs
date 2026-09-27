namespace BannerService.Application.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BannerService.Presentation.Controllers;
using Application.Services;
using Application.DTOs;
using Domain.Entities;

public class SubscriptionsControllerTests
{
    private readonly Mock<SubscriptionService> _mockSubscriptionService;
    private readonly Mock<RenewalService> _mockRenewalService;
    private readonly Mock<BillingService> _mockBillingService;
    private readonly Mock<ILogger<SubscriptionsController>> _mockLogger;
    private readonly SubscriptionsController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testSubscriptionId;
    private readonly Guid _testPlanId;
    private readonly Guid _testUserId;

    public SubscriptionsControllerTests()
    {
        _mockSubscriptionService = new Mock<SubscriptionService>(null, null, null, null);
        _mockRenewalService = new Mock<RenewalService>(null, null, null, null);
        _mockBillingService = new Mock<BillingService>(null, null, null, null);
        _mockLogger = new Mock<ILogger<SubscriptionsController>>();
        _testShopId = Guid.NewGuid();
        _testSubscriptionId = Guid.NewGuid();
        _testPlanId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();

        _controller = new SubscriptionsController(
            _mockSubscriptionService.Object,
            _mockRenewalService.Object,
            _mockBillingService.Object,
            _mockLogger.Object);

        // Setup controller context with user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region GetPlans Tests

    [Fact]
    public async Task GetPlans_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var plans = new List<SubscriptionPlan>
        {
            new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Basic", MonthlyPrice = 10m },
            new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Premium", MonthlyPrice = 20m }
        };

        _mockSubscriptionService.Setup(s => s.GetAllPlansAsync())
            .ReturnsAsync(plans);

        // Act
        var result = await _controller.GetPlans();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetPlans_WithNoPlans_ShouldReturnEmptyList()
    {
        // Arrange
        _mockSubscriptionService.Setup(s => s.GetAllPlansAsync())
            .ReturnsAsync(new List<SubscriptionPlan>());

        // Act
        var result = await _controller.GetPlans();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetPlan Tests

    [Fact]
    public async Task GetPlan_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var plan = new SubscriptionPlan
        {
            Id = _testPlanId,
            Name = "Basic",
            MonthlyPrice = 10m
        };

        _mockSubscriptionService.Setup(s => s.GetPlanByIdAsync(_testPlanId))
            .ReturnsAsync(plan);

        // Act
        var result = await _controller.GetPlan(_testPlanId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetPlan_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidPlanId = Guid.NewGuid();
        _mockSubscriptionService.Setup(s => s.GetPlanByIdAsync(invalidPlanId))
            .ReturnsAsync((SubscriptionPlan)null);

        // Act
        var result = await _controller.GetPlan(invalidPlanId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetShopSubscription Tests

    [Fact]
    public async Task GetShopSubscription_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = _testSubscriptionId,
            ShopId = _testShopId,
            PlanId = _testPlanId,
            Status = "Active"
        };

        _mockSubscriptionService.Setup(s => s.GetCurrentSubscriptionAsync(_testShopId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.GetShopSubscription(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetShopSubscription_WithNoSubscription_ShouldReturnNotFound()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        _mockSubscriptionService.Setup(s => s.GetCurrentSubscriptionAsync(shopId))
            .ReturnsAsync((Subscription)null);

        // Act
        var result = await _controller.GetShopSubscription(shopId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region CreateSubscription Tests

    [Fact]
    public async Task CreateSubscription_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = new CreateSubscriptionDto
        {
            ShopId = _testShopId,
            PlanId = _testPlanId,
            BillingPeriod = "monthly",
            PaymentMethodId = "pm_123",
            TrialDays = 7
        };

        var subscription = new Subscription
        {
            Id = _testSubscriptionId,
            ShopId = _testShopId,
            PlanId = _testPlanId,
            Status = "Active"
        };

        _mockSubscriptionService.Setup(s => s.CreateSubscriptionAsync(
            request.ShopId, request.PlanId, request.BillingPeriod,
            request.PaymentMethodId, request.TrialDays, _testUserId))
            .ReturnsAsync((true, "Subscription created", subscription));

        // Act
        var result = await _controller.CreateSubscription(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(SubscriptionsController.GetShopSubscription), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateSubscriptionDto
        {
            ShopId = Guid.Empty,
            PlanId = _testPlanId,
            BillingPeriod = "monthly",
            PaymentMethodId = "pm_123"
        };

        _mockSubscriptionService.Setup(s => s.CreateSubscriptionAsync(
            request.ShopId, request.PlanId, request.BillingPeriod,
            request.PaymentMethodId, 0, _testUserId))
            .ReturnsAsync((false, "Invalid subscription data", null));

        // Act
        var result = await _controller.CreateSubscription(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region UpgradePlan Tests

    [Fact]
    public async Task UpgradePlan_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var newPlanId = Guid.NewGuid();
        var request = new ChangePlanDto { NewPlanId = newPlanId };
        var subscription = new Subscription
        {
            Id = _testSubscriptionId,
            ShopId = _testShopId,
            PlanId = newPlanId,
            Status = "Active"
        };

        _mockSubscriptionService.Setup(s => s.UpgradePlanAsync(_testSubscriptionId, newPlanId))
            .ReturnsAsync((true, "Plan upgraded successfully"));
        _mockSubscriptionService.Setup(s => s.GetSubscriptionByIdAsync(_testSubscriptionId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.UpgradePlan(_testSubscriptionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region CancelSubscription Tests

    [Fact]
    public async Task CancelSubscription_WithValidId_ShouldReturnOk()
    {
        // Arrange
        _mockSubscriptionService.Setup(s => s.CancelSubscriptionAsync(_testSubscriptionId))
            .ReturnsAsync((true, "Subscription cancelled"));

        // Act
        var result = await _controller.CancelSubscription(_testSubscriptionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CancelSubscription_WithInvalidId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidSubscriptionId = Guid.NewGuid();
        _mockSubscriptionService.Setup(s => s.CancelSubscriptionAsync(invalidSubscriptionId))
            .ReturnsAsync((false, "Subscription not found"));

        // Act
        var result = await _controller.CancelSubscription(invalidSubscriptionId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region RenewSubscription Tests

    [Fact]
    public async Task RenewSubscription_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = _testSubscriptionId,
            ShopId = _testShopId,
            Status = "Active"
        };

        _mockRenewalService.Setup(s => s.ProcessRenewalAsync(_testSubscriptionId))
            .ReturnsAsync((true, "Subscription renewed"));
        _mockSubscriptionService.Setup(s => s.GetSubscriptionByIdAsync(_testSubscriptionId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.RenewSubscription(_testSubscriptionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region DowngradePlan Tests

    [Fact]
    public async Task DowngradePlan_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var newPlanId = Guid.NewGuid();
        var request = new ChangePlanDto { NewPlanId = newPlanId };
        var subscription = new Subscription
        {
            Id = _testSubscriptionId,
            ShopId = _testShopId,
            PlanId = newPlanId,
            Status = "Active"
        };

        _mockSubscriptionService.Setup(s => s.DowngradePlanAsync(_testSubscriptionId, newPlanId))
            .ReturnsAsync((true, "Plan downgraded"));
        _mockSubscriptionService.Setup(s => s.GetSubscriptionByIdAsync(_testSubscriptionId))
            .ReturnsAsync(subscription);

        // Act
        var result = await _controller.DowngradePlan(_testSubscriptionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}
