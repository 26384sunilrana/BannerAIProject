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

public class PublishWorkflowControllerTests
{
    private readonly Mock<IPublishWorkflowService> _mockWorkflowService;
    private readonly Mock<IPublishWorkflowRepository> _mockRepository;
    private readonly Mock<ILogger<PublishWorkflowController>> _mockLogger;
    private readonly PublishWorkflowController _controller;
    private readonly Guid _testWorkflowId;
    private readonly Guid _testBannerId;
    private readonly Guid _testShopId;
    private readonly Guid _testUserId;

    public PublishWorkflowControllerTests()
    {
        _mockWorkflowService = new Mock<IPublishWorkflowService>();
        _mockRepository = new Mock<IPublishWorkflowRepository>();
        _mockLogger = new Mock<ILogger<PublishWorkflowController>>();
        _testWorkflowId = Guid.NewGuid();
        _testBannerId = Guid.NewGuid();
        _testShopId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();
        _controller = new PublishWorkflowController(_mockWorkflowService.Object, _mockRepository.Object, _mockLogger.Object);
    }

    #region InitiateWorkflow Tests

    [Fact]
    public async Task InitiateWorkflow_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new SubmitForApprovalDto { BannerId = _testBannerId };
        var workflow = new PublishWorkflow
        {
            Id = _testWorkflowId,
            BannerId = _testBannerId,
            ShopId = _testShopId,
            Status = PublishWorkflowStatus.Draft
        };

        _mockWorkflowService.Setup(s => s.InitiateWorkflowAsync(
            _testBannerId, It.IsAny<Guid>(), _testUserId, It.IsAny<string>()))
            .ReturnsAsync(workflow);

        // Act
        var result = await _controller.InitiateWorkflow(request, _testUserId, "testuser");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region SubmitForApproval Tests

    [Fact]
    public async Task SubmitForApproval_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var workflow = new PublishWorkflow
        {
            Id = _testWorkflowId,
            BannerId = _testBannerId,
            Status = PublishWorkflowStatus.Submitted
        };

        _mockWorkflowService.Setup(s => s.SubmitForApprovalAsync(
            _testWorkflowId, _testUserId, It.IsAny<string>()))
            .ReturnsAsync(workflow);

        // Act
        var result = await _controller.SubmitForApproval(_testWorkflowId, _testUserId, "testuser");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task SubmitForApproval_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidWorkflowId = Guid.NewGuid();
        _mockWorkflowService.Setup(s => s.SubmitForApprovalAsync(
            invalidWorkflowId, _testUserId, It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException("Workflow not found"));

        // Act
        var result = await _controller.SubmitForApproval(invalidWorkflowId, _testUserId, "testuser");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region ApproveWorkflow Tests

    [Fact]
    public async Task ApproveWorkflow_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new ApproveWorkflowDto { Comment = "Approved" };
        var workflow = new PublishWorkflow
        {
            Id = _testWorkflowId,
            BannerId = _testBannerId,
            Status = PublishWorkflowStatus.Approved
        };

        _mockWorkflowService.Setup(s => s.ApproveAsync(
            _testWorkflowId, _testUserId, It.IsAny<string>(), request.Comment))
            .ReturnsAsync(workflow);

        // Act
        var result = await _controller.ApproveWorkflow(_testWorkflowId, request, _testUserId, "reviewer");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ApproveWorkflow_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new ApproveWorkflowDto { Comment = "Approved" };
        var invalidWorkflowId = Guid.NewGuid();
        _mockWorkflowService.Setup(s => s.ApproveAsync(
            invalidWorkflowId, _testUserId, It.IsAny<string>(), request.Comment))
            .ThrowsAsync(new KeyNotFoundException("Workflow not found"));

        // Act
        var result = await _controller.ApproveWorkflow(invalidWorkflowId, request, _testUserId, "reviewer");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region RejectWorkflow Tests

    [Fact]
    public async Task RejectWorkflow_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new RejectWorkflowDto { Reason = "Needs changes" };
        var workflow = new PublishWorkflow
        {
            Id = _testWorkflowId,
            BannerId = _testBannerId,
            Status = PublishWorkflowStatus.Rejected
        };

        _mockWorkflowService.Setup(s => s.RejectAsync(
            _testWorkflowId, _testUserId, It.IsAny<string>(), request.Reason))
            .ReturnsAsync(workflow);

        // Act
        var result = await _controller.RejectWorkflow(_testWorkflowId, request, _testUserId, "reviewer");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region PublishWorkflow Tests

    [Fact]
    public async Task PublishWorkflow_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var workflow = new PublishWorkflow
        {
            Id = _testWorkflowId,
            BannerId = _testBannerId,
            Status = PublishWorkflowStatus.Published
        };

        _mockWorkflowService.Setup(s => s.PublishAsync(_testWorkflowId, _testUserId, It.IsAny<string>()))
            .ReturnsAsync(workflow);

        // Act
        var result = await _controller.PublishWorkflow(_testWorkflowId, _testUserId, "publisher");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetWorkflow Tests

    [Fact]
    public async Task GetWorkflow_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var workflow = new PublishWorkflow
        {
            Id = _testWorkflowId,
            BannerId = _testBannerId,
            Status = PublishWorkflowStatus.Approved
        };

        _mockWorkflowService.Setup(s => s.GetWorkflowAsync(_testWorkflowId))
            .ReturnsAsync(workflow);
        _mockRepository.Setup(r => r.GetApprovalRequestsByWorkflowAsync(_testWorkflowId))
            .ReturnsAsync(new List<ApprovalRequest>());

        // Act
        var result = await _controller.GetWorkflow(_testWorkflowId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWorkflow_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidWorkflowId = Guid.NewGuid();
        _mockWorkflowService.Setup(s => s.GetWorkflowAsync(invalidWorkflowId))
            .ReturnsAsync((PublishWorkflow)null);

        // Act
        var result = await _controller.GetWorkflow(invalidWorkflowId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetPendingWorkflows Tests

    [Fact]
    public async Task GetPendingWorkflows_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var workflows = new List<PublishWorkflow>
        {
            new PublishWorkflow { Id = Guid.NewGuid(), BannerId = _testBannerId, Status = PublishWorkflowStatus.Submitted },
            new PublishWorkflow { Id = Guid.NewGuid(), BannerId = _testBannerId, Status = PublishWorkflowStatus.Submitted }
        };

        _mockWorkflowService.Setup(s => s.GetPendingWorkflowsAsync())
            .ReturnsAsync(workflows);

        // Act
        var result = await _controller.GetPendingWorkflows();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}
