namespace BannerService.Application.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BannerService.Presentation.Controllers;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Application.Dto;

public class SubscriptionPlanControllerTests
{
    private readonly Mock<ISubscriptionPlanRepository> _mockPlanRepository;
    private readonly Mock<ISubscriptionRepository> _mockSubscriptionRepository;
    private readonly Mock<ILogger<SubscriptionPlanController>> _mockLogger;
    private readonly SubscriptionPlanController _controller;

    public SubscriptionPlanControllerTests()
    {
        _mockPlanRepository = new Mock<ISubscriptionPlanRepository>();
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockLogger = new Mock<ILogger<SubscriptionPlanController>>();

        _controller = new SubscriptionPlanController(
            _mockPlanRepository.Object,
            _mockSubscriptionRepository.Object,
            _mockLogger.Object
        );
    }

    #region GetAllPlans Tests

    [Fact]
    public async Task GetAllPlans_WithValidRequest_ShouldReturnOkWithPlans()
    {
        // Arrange
        var plans = new List<SubscriptionPlan>
        {
            new() { Id = Guid.NewGuid(), Name = "Basic", MonthlyPrice = 9.99m, AnnualPrice = 99.99m, Status = SubscriptionPlanStatus.Active },
            new() { Id = Guid.NewGuid(), Name = "Premium", MonthlyPrice = 29.99m, AnnualPrice = 299.99m, Status = SubscriptionPlanStatus.Active }
        };

        _mockPlanRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(plans);

        // Act
        var result = await _controller.GetAllPlans();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockPlanRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllPlans_WithNoPlans_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        _mockPlanRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<SubscriptionPlan>());

        // Act
        var result = await _controller.GetAllPlans();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetPlanById Tests

    [Fact]
    public async Task GetPlanById_WithValidId_ShouldReturnOkWithPlan()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Professional",
            MonthlyPrice = 49.99m,
            AnnualPrice = 499.99m,
            Status = SubscriptionPlanStatus.Active
        };

        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(plan);

        // Act
        var result = await _controller.GetPlanById(planId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockPlanRepository.Verify(r => r.GetByIdAsync(planId), Times.Once);
    }

    [Fact]
    public async Task GetPlanById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();
        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync((SubscriptionPlan)null);

        // Act
        var result = await _controller.GetPlanById(planId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region CreatePlan Tests

    [Fact]
    public async Task CreatePlan_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var createDto = new CreatePlanDto
        {
            Name = "Starter",
            Description = "Starter plan",
            MonthlyPrice = 4.99m,
            AnnualPrice = 49.99m
        };

        var createdPlan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            MonthlyPrice = createDto.MonthlyPrice,
            AnnualPrice = createDto.AnnualPrice,
            Status = SubscriptionPlanStatus.Active
        };

        _mockPlanRepository.Setup(r => r.CreateAsync(It.IsAny<SubscriptionPlan>())).ReturnsAsync(createdPlan);

        // Act
        var result = await _controller.CreatePlan(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(SubscriptionPlanController.GetPlanById), createdResult.ActionName);
        _mockPlanRepository.Verify(r => r.CreateAsync(It.IsAny<SubscriptionPlan>()), Times.Once);
    }

    [Fact]
    public async Task CreatePlan_WithInvalidPrice_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreatePlanDto
        {
            Name = "Invalid",
            Description = "Invalid plan",
            MonthlyPrice = 100m,
            AnnualPrice = 50m  // Annual < Monthly is invalid
        };

        // Act
        var result = await _controller.CreatePlan(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region UpdatePlan Tests

    [Fact]
    public async Task UpdatePlan_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var updateDto = new UpdatePlanDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 19.99m,
            AnnualPrice = 199.99m
        };

        var existingPlan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Old Name",
            MonthlyPrice = 9.99m,
            AnnualPrice = 99.99m,
            Status = SubscriptionPlanStatus.Active
        };

        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(existingPlan);
        _mockPlanRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>())).ReturnsAsync(existingPlan);

        // Act
        var result = await _controller.UpdatePlan(planId, updateDto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockPlanRepository.Verify(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePlan_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var updateDto = new UpdatePlanDto { Name = "Updated", MonthlyPrice = 19.99m, AnnualPrice = 199.99m };

        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync((SubscriptionPlan)null);

        // Act
        var result = await _controller.UpdatePlan(planId, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region DeletePlan Tests

    [Fact]
    public async Task DeletePlan_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan { Id = planId, Name = "ToDelete", Status = SubscriptionPlanStatus.Active };

        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(plan);
        _mockPlanRepository.Setup(r => r.DeleteAsync(planId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePlan(planId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockPlanRepository.Verify(r => r.DeleteAsync(planId), Times.Once);
    }

    [Fact]
    public async Task DeletePlan_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();
        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync((SubscriptionPlan)null);

        // Act
        var result = await _controller.DeletePlan(planId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region DeactivatePlan Tests

    [Fact]
    public async Task DeactivatePlan_WithActivePlan_ShouldReturnOk()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var activePlan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Active Plan",
            MonthlyPrice = 29.99m,
            AnnualPrice = 299.99m,
            Status = SubscriptionPlanStatus.Active
        };

        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(activePlan);
        _mockPlanRepository.Setup(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>())).ReturnsAsync(activePlan);

        // Act
        var result = await _controller.DeactivatePlan(planId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockPlanRepository.Verify(r => r.UpdateAsync(It.IsAny<SubscriptionPlan>()), Times.Once);
    }

    [Fact]
    public async Task DeactivatePlan_WithInactivePlan_ShouldReturnBadRequest()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var inactivePlan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Inactive Plan",
            Status = SubscriptionPlanStatus.Inactive
        };

        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(inactivePlan);

        // Act
        var result = await _controller.DeactivatePlan(planId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetPlanSubscriptions Tests

    [Fact]
    public async Task GetPlanSubscriptions_WithValidPlanId_ShouldReturnOkWithSubscriptions()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var subscriptions = new List<Subscription>
        {
            new() { Id = Guid.NewGuid(), PlanId = planId, Status = SubscriptionStatus.Active },
            new() { Id = Guid.NewGuid(), PlanId = planId, Status = SubscriptionStatus.Active }
        };

        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(new SubscriptionPlan { Id = planId });
        _mockSubscriptionRepository.Setup(r => r.GetByPlanIdAsync(planId)).ReturnsAsync(subscriptions);

        // Act
        var result = await _controller.GetPlanSubscriptions(planId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetPlanSubscriptions_WithInvalidPlanId_ShouldReturnNotFound()
    {
        // Arrange
        var planId = Guid.NewGuid();
        _mockPlanRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync((SubscriptionPlan)null);

        // Act
        var result = await _controller.GetPlanSubscriptions(planId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
