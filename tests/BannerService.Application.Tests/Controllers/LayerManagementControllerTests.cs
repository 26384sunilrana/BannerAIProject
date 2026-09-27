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
using Application.Dto;
using Domain.Services;
using Domain.Interfaces;
using Domain.Entities;

public class LayerManagementControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<LayerManagementService> _mockLayerService;
    private readonly Mock<ILogger<LayerManagementController>> _mockLogger;
    private readonly LayerManagementController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testBannerId;
    private readonly Guid _testComponentId;

    public LayerManagementControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLayerService = new Mock<LayerManagementService>(null, null, null);
        _mockLogger = new Mock<ILogger<LayerManagementController>>();
        _testShopId = Guid.NewGuid();
        _testBannerId = Guid.NewGuid();
        _testComponentId = Guid.NewGuid();

        _controller = new LayerManagementController(_mockUnitOfWork.Object, _mockLayerService.Object, _mockLogger.Object);

        // Setup controller context with shop_id claim
        var claims = new List<Claim>
        {
            new Claim("shop_id", _testShopId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region ReorderComponent Tests

    [Fact]
    public async Task ReorderComponent_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new ReorderComponentRequestDto { NewZIndex = 5 };
        var banner = new Banner { Id = _testBannerId, ShopId = _testShopId };
        var layerOrder = new LayerOrderResult { ComponentId = _testComponentId, OldZIndex = 2, NewZIndex = 5 };

        _mockUnitOfWork.Setup(u => u.BannerRepository.GetByIdAsync(_testBannerId, _testShopId))
            .ReturnsAsync(banner);
        _mockLayerService.Setup(s => s.ReorderComponent(banner, _testComponentId, 5))
            .Returns(layerOrder);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.ReorderComponent(_testBannerId, _testComponentId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ReorderComponent_WithNonExistentBanner_ShouldReturnNotFound()
    {
        // Arrange
        var request = new ReorderComponentRequestDto { NewZIndex = 5 };
        _mockUnitOfWork.Setup(u => u.BannerRepository.GetByIdAsync(_testBannerId, _testShopId))
            .ReturnsAsync((Banner)null);

        // Act
        var result = await _controller.ReorderComponent(_testBannerId, _testComponentId, request);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region MoveForward Tests

    [Fact]
    public async Task MoveForward_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var banner = new Banner { Id = _testBannerId, ShopId = _testShopId };
        var layerOrder = new LayerOrderResult { ComponentId = _testComponentId, OldZIndex = 2, NewZIndex = 3 };

        _mockUnitOfWork.Setup(u => u.BannerRepository.GetByIdAsync(_testBannerId, _testShopId))
            .ReturnsAsync(banner);
        _mockLayerService.Setup(s => s.MoveForward(banner, _testComponentId))
            .Returns(layerOrder);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.MoveForward(_testBannerId, _testComponentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task MoveForward_WithInvalidBanner_ShouldReturnNotFound()
    {
        // Arrange
        _mockUnitOfWork.Setup(u => u.BannerRepository.GetByIdAsync(_testBannerId, _testShopId))
            .ReturnsAsync((Banner)null);

        // Act
        var result = await _controller.MoveForward(_testBannerId, _testComponentId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region MoveBackward Tests

    [Fact]
    public async Task MoveBackward_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var banner = new Banner { Id = _testBannerId, ShopId = _testShopId };
        var layerOrder = new LayerOrderResult { ComponentId = _testComponentId, OldZIndex = 3, NewZIndex = 2 };

        _mockUnitOfWork.Setup(u => u.BannerRepository.GetByIdAsync(_testBannerId, _testShopId))
            .ReturnsAsync(banner);
        _mockLayerService.Setup(s => s.MoveBackward(banner, _testComponentId))
            .Returns(layerOrder);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.MoveBackward(_testBannerId, _testComponentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region SendToFront Tests

    [Fact]
    public async Task SendToFront_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var banner = new Banner { Id = _testBannerId, ShopId = _testShopId };
        var layerOrder = new LayerOrderResult { ComponentId = _testComponentId, OldZIndex = 2, NewZIndex = 10 };

        _mockUnitOfWork.Setup(u => u.BannerRepository.GetByIdAsync(_testBannerId, _testShopId))
            .ReturnsAsync(banner);
        _mockLayerService.Setup(s => s.SendToFront(banner, _testComponentId))
            .Returns(layerOrder);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.SendToFront(_testBannerId, _testComponentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region SendToBack Tests

    [Fact]
    public async Task SendToBack_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var banner = new Banner { Id = _testBannerId, ShopId = _testShopId };
        var layerOrder = new LayerOrderResult { ComponentId = _testComponentId, OldZIndex = 8, NewZIndex = 1 };

        _mockUnitOfWork.Setup(u => u.BannerRepository.GetByIdAsync(_testBannerId, _testShopId))
            .ReturnsAsync(banner);
        _mockLayerService.Setup(s => s.SendToBack(banner, _testComponentId))
            .Returns(layerOrder);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _controller.SendToBack(_testBannerId, _testComponentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task SendToBack_WithInvalidBanner_ShouldReturnNotFound()
    {
        // Arrange
        _mockUnitOfWork.Setup(u => u.BannerRepository.GetByIdAsync(_testBannerId, _testShopId))
            .ReturnsAsync((Banner)null);

        // Act
        var result = await _controller.SendToBack(_testBannerId, _testComponentId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}

public class LayerOrderResult
{
    public Guid ComponentId { get; set; }
    public int OldZIndex { get; set; }
    public int NewZIndex { get; set; }
}
