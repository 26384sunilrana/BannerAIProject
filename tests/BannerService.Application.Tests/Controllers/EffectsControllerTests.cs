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
using BannerService.Application.Dto;
using BannerService.Application.Services;

public class EffectsControllerTests
{
    private readonly Mock<IEffectService> _mockEffectService;
    private readonly Mock<ILogger<EffectsController>> _mockLogger;
    private readonly EffectsController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testBannerId;
    private readonly Guid _testComponentId;
    private readonly Guid _testEffectId;

    public EffectsControllerTests()
    {
        _mockEffectService = new Mock<IEffectService>();
        _mockLogger = new Mock<ILogger<EffectsController>>();
        _testShopId = Guid.NewGuid();
        _testBannerId = Guid.NewGuid();
        _testComponentId = Guid.NewGuid();
        _testEffectId = Guid.NewGuid();

        _controller = new EffectsController(_mockEffectService.Object, _mockLogger.Object);

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

    #region ApplyEffect Tests

    [Fact]
    public async Task ApplyEffect_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new ApplyEffectRequestDto
        {
            EffectType = 1,
            Parameters = new Dictionary<string, object> { { "intensity", 0.5 } }
        };

        var effect = new EffectDto
        {
            Id = _testEffectId,
            ComponentId = _testComponentId,
            EffectType = request.EffectType
        };

        _mockEffectService.Setup(s => s.ApplyEffectAsync(
            _testBannerId, _testComponentId, _testShopId, request.EffectType, request.Parameters))
            .ReturnsAsync(effect);

        // Act
        var result = await _controller.ApplyEffect(_testBannerId, _testComponentId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ApplyEffect_WithEmptyBannerId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new ApplyEffectRequestDto
        {
            EffectType = 1,
            Parameters = new Dictionary<string, object> { { "intensity", 0.5 } }
        };

        // Act
        var result = await _controller.ApplyEffect(Guid.Empty, _testComponentId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ApplyEffect_WithInvalidEffectType_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new ApplyEffectRequestDto
        {
            EffectType = 10, // Invalid
            Parameters = new Dictionary<string, object> { { "intensity", 0.5 } }
        };

        // Act
        var result = await _controller.ApplyEffect(_testBannerId, _testComponentId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ApplyEffect_WithNoParameters_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new ApplyEffectRequestDto
        {
            EffectType = 1,
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _controller.ApplyEffect(_testBannerId, _testComponentId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region ListEffects Tests

    [Fact]
    public async Task ListEffects_WithValidIds_ShouldReturnOkWithList()
    {
        // Arrange
        var effects = new List<EffectDto>
        {
            new EffectDto { Id = Guid.NewGuid(), ComponentId = _testComponentId },
            new EffectDto { Id = Guid.NewGuid(), ComponentId = _testComponentId }
        };

        _mockEffectService.Setup(s => s.GetComponentEffectsAsync(_testBannerId, _testComponentId, _testShopId))
            .ReturnsAsync(effects);

        // Act
        var result = await _controller.ListEffects(_testBannerId, _testComponentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ListEffects_WithNoEffects_ShouldReturnEmptyList()
    {
        // Arrange
        _mockEffectService.Setup(s => s.GetComponentEffectsAsync(_testBannerId, _testComponentId, _testShopId))
            .ReturnsAsync(new List<EffectDto>());

        // Act
        var result = await _controller.ListEffects(_testBannerId, _testComponentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region UpdateEffect Tests

    [Fact]
    public async Task UpdateEffect_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new UpdateEffectRequestDto
        {
            Parameters = new Dictionary<string, object> { { "intensity", 0.8 } }
        };

        var updatedEffect = new EffectDto
        {
            Id = _testEffectId,
            ComponentId = _testComponentId,
            EffectType = 1
        };

        _mockEffectService.Setup(s => s.UpdateEffectAsync(
            _testBannerId, _testComponentId, _testEffectId, request.Parameters, _testShopId))
            .ReturnsAsync(updatedEffect);

        // Act
        var result = await _controller.UpdateEffect(_testBannerId, _testComponentId, _testEffectId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateEffect_WithNoParameters_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UpdateEffectRequestDto
        {
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _controller.UpdateEffect(_testBannerId, _testComponentId, _testEffectId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region RemoveEffect Tests

    [Fact]
    public async Task RemoveEffect_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        _mockEffectService.Setup(s => s.RemoveEffectAsync(
            _testBannerId, _testComponentId, _testEffectId, _testShopId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveEffect(_testBannerId, _testComponentId, _testEffectId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveEffect_WithInvalidId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var invalidEffectId = Guid.NewGuid();
        _mockEffectService.Setup(s => s.RemoveEffectAsync(
            _testBannerId, _testComponentId, invalidEffectId, _testShopId))
            .ThrowsAsync(new KeyNotFoundException("Effect not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.RemoveEffect(_testBannerId, _testComponentId, invalidEffectId)
        );
    }

    #endregion
}
