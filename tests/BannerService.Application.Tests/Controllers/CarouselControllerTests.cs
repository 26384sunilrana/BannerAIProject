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

public class CarouselControllerTests
{
    private readonly Mock<ICarouselService> _mockCarouselService;
    private readonly Mock<ILogger<CarouselController>> _mockLogger;
    private readonly CarouselController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testBannerId;
    private readonly Guid _testCarouselId;

    public CarouselControllerTests()
    {
        _mockCarouselService = new Mock<ICarouselService>();
        _mockLogger = new Mock<ILogger<CarouselController>>();
        _testShopId = Guid.NewGuid();
        _testBannerId = Guid.NewGuid();
        _testCarouselId = Guid.NewGuid();

        _controller = new CarouselController(_mockCarouselService.Object, _mockLogger.Object);

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

    #region CreateCarousel Tests

    [Fact]
    public async Task CreateCarousel_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = new CreateCarouselRequestDto
        {
            ComponentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            IntervalMs = 5000,
            TransitionDuration = 500,
            TransitionType = 1
        };

        var carousel = new CarouselDto
        {
            Id = _testCarouselId,
            BannerId = _testBannerId,
            IntervalMs = request.IntervalMs,
            TransitionDuration = request.TransitionDuration
        };

        _mockCarouselService.Setup(s => s.CreateCarouselAsync(
            _testBannerId, _testShopId, request.IntervalMs, request.TransitionDuration,
            request.TransitionType, request.ComponentIds))
            .ReturnsAsync(carousel);

        // Act
        var result = await _controller.CreateCarousel(_testBannerId, request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(CarouselController.GetCarousel), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateCarousel_WithInvalidBannerId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateCarouselRequestDto
        {
            ComponentIds = new List<Guid> { Guid.NewGuid() },
            IntervalMs = 5000,
            TransitionDuration = 500,
            TransitionType = 1
        };

        // Act
        var result = await _controller.CreateCarousel(Guid.Empty, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateCarousel_WithFewerThanTwoComponents_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateCarouselRequestDto
        {
            ComponentIds = new List<Guid> { Guid.NewGuid() },
            IntervalMs = 5000,
            TransitionDuration = 500,
            TransitionType = 1
        };

        // Act
        var result = await _controller.CreateCarousel(_testBannerId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetCarousel Tests

    [Fact]
    public async Task GetCarousel_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var carousel = new CarouselDto
        {
            Id = _testCarouselId,
            BannerId = _testBannerId,
            IntervalMs = 5000,
            TransitionDuration = 500
        };

        _mockCarouselService.Setup(s => s.GetCarouselAsync(_testCarouselId, _testShopId))
            .ReturnsAsync(carousel);

        // Act
        var result = await _controller.GetCarousel(_testBannerId, _testCarouselId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetCarousel_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidCarouselId = Guid.NewGuid();
        _mockCarouselService.Setup(s => s.GetCarouselAsync(invalidCarouselId, _testShopId))
            .ReturnsAsync((CarouselDto)null);

        // Act
        var result = await _controller.GetCarousel(_testBannerId, invalidCarouselId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region ListCarousels Tests

    [Fact]
    public async Task ListCarousels_WithValidBannerId_ShouldReturnOkWithList()
    {
        // Arrange
        var carousels = new List<CarouselDto>
        {
            new CarouselDto { Id = Guid.NewGuid(), BannerId = _testBannerId },
            new CarouselDto { Id = Guid.NewGuid(), BannerId = _testBannerId }
        };

        _mockCarouselService.Setup(s => s.GetBannerCarouselsAsync(_testBannerId, _testShopId))
            .ReturnsAsync(carousels);

        // Act
        var result = await _controller.ListCarousels(_testBannerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ListCarousels_WithNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        _mockCarouselService.Setup(s => s.GetBannerCarouselsAsync(_testBannerId, _testShopId))
            .ReturnsAsync(new List<CarouselDto>());

        // Act
        var result = await _controller.ListCarousels(_testBannerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region UpdateCarousel Tests

    [Fact]
    public async Task UpdateCarousel_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new CreateCarouselRequestDto
        {
            ComponentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            IntervalMs = 3000,
            TransitionDuration = 400,
            TransitionType = 2
        };

        var updatedCarousel = new CarouselDto
        {
            Id = _testCarouselId,
            BannerId = _testBannerId,
            IntervalMs = request.IntervalMs
        };

        _mockCarouselService.Setup(s => s.UpdateCarouselAsync(
            _testCarouselId, _testShopId, request.IntervalMs, request.TransitionDuration,
            request.TransitionType, request.ComponentIds))
            .ReturnsAsync(updatedCarousel);

        // Act
        var result = await _controller.UpdateCarousel(_testBannerId, _testCarouselId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateCarousel_WithInvalidComponentCount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateCarouselRequestDto
        {
            ComponentIds = new List<Guid> { Guid.NewGuid() },
            IntervalMs = 5000,
            TransitionDuration = 500,
            TransitionType = 1
        };

        // Act
        var result = await _controller.UpdateCarousel(_testBannerId, _testCarouselId, request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region DeleteCarousel Tests

    [Fact]
    public async Task DeleteCarousel_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        _mockCarouselService.Setup(s => s.DeleteCarouselAsync(_testCarouselId, _testShopId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCarousel(_testBannerId, _testCarouselId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion
}
