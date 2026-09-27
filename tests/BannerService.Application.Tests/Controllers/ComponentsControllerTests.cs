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
using Application.Services;

public class ComponentsControllerTests
{
    private readonly Mock<IBannerService> _mockBannerService;
    private readonly Mock<ILogger<ComponentsController>> _mockLogger;
    private readonly ComponentsController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testBannerId;
    private readonly Guid _testComponentId;

    public ComponentsControllerTests()
    {
        _mockBannerService = new Mock<IBannerService>();
        _mockLogger = new Mock<ILogger<ComponentsController>>();
        _testShopId = Guid.NewGuid();
        _testBannerId = Guid.NewGuid();
        _testComponentId = Guid.NewGuid();

        _controller = new ComponentsController(_mockBannerService.Object, _mockLogger.Object);

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

    #region AddComponent Tests

    [Fact]
    public async Task AddComponent_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = new AddComponentRequestDto
        {
            Type = "text",
            Content = "Test content",
            X = 10,
            Y = 20,
            Width = 200,
            Height = 100
        };

        var component = new ComponentResponseDto
        {
            Id = _testComponentId,
            BannerId = _testBannerId,
            Type = request.Type,
            Content = request.Content
        };

        _mockBannerService.Setup(s => s.AddComponentAsync(_testBannerId, _testShopId, request))
            .ReturnsAsync(component);

        // Act
        var result = await _controller.AddComponent(_testBannerId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ComponentsController.UpdateComponent), createdResult.ActionName);
    }

    [Fact]
    public async Task AddComponent_WithNullRequest_ShouldThrowException()
    {
        // Arrange
        AddComponentRequestDto request = null;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _controller.AddComponent(_testBannerId, request));
    }

    #endregion

    #region UpdateComponent Tests

    [Fact]
    public async Task UpdateComponent_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new AddComponentRequestDto
        {
            Type = "image",
            Content = "Updated content",
            X = 15,
            Y = 25,
            Width = 250,
            Height = 150
        };

        var updatedComponent = new ComponentResponseDto
        {
            Id = _testComponentId,
            BannerId = _testBannerId,
            Type = request.Type,
            Content = request.Content
        };

        _mockBannerService.Setup(s => s.UpdateComponentAsync(_testBannerId, _testComponentId, _testShopId, request))
            .ReturnsAsync(updatedComponent);

        // Act
        var result = await _controller.UpdateComponent(_testBannerId, _testComponentId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateComponent_WithInvalidComponentId_ShouldThrowException()
    {
        // Arrange
        var request = new AddComponentRequestDto
        {
            Type = "text",
            Content = "Content",
            X = 10,
            Y = 20,
            Width = 200,
            Height = 100
        };

        var invalidComponentId = Guid.NewGuid();
        _mockBannerService.Setup(s => s.UpdateComponentAsync(_testBannerId, invalidComponentId, _testShopId, request))
            .ThrowsAsync(new KeyNotFoundException("Component not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.UpdateComponent(_testBannerId, invalidComponentId, request)
        );
    }

    #endregion

    #region RemoveComponent Tests

    [Fact]
    public async Task RemoveComponent_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        _mockBannerService.Setup(s => s.RemoveComponentAsync(_testBannerId, _testComponentId, _testShopId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveComponent(_testBannerId, _testComponentId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveComponent_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidComponentId = Guid.NewGuid();
        _mockBannerService.Setup(s => s.RemoveComponentAsync(_testBannerId, invalidComponentId, _testShopId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RemoveComponent(_testBannerId, invalidComponentId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
