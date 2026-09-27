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
using BannerService.Domain.Entities;

public class BannersControllerTests
{
    private readonly Mock<IBannerService> _mockBannerService;
    private readonly Mock<ILogger<BannersController>> _mockLogger;
    private readonly BannersController _controller;
    private readonly Guid _testUserId;
    private readonly Guid _testShopId;
    private readonly Guid _testBannerId;

    public BannersControllerTests()
    {
        _mockBannerService = new Mock<IBannerService>();
        _mockLogger = new Mock<ILogger<BannersController>>();
        _testUserId = Guid.NewGuid();
        _testShopId = Guid.NewGuid();
        _testBannerId = Guid.NewGuid();

        _controller = new BannersController(_mockBannerService.Object, _mockLogger.Object);

        // Setup controller context with claims for shop_id and user_id
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new Claim("shop_id", _testShopId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region CreateBanner Tests

    [Fact]
    public async Task CreateBanner_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var createRequest = new CreateBannerRequestDto
        {
            Name = "New Banner",
            Description = "Banner description",
            Content = "Banner content",
            IsActive = true
        };

        var createdBanner = new BannerResponseDto
        {
            Id = _testBannerId,
            Name = createRequest.Name,
            Description = createRequest.Description,
            ShopId = _testShopId,
            UserId = _testUserId
        };

        _mockBannerService.Setup(s => s.CreateBannerAsync(_testShopId, _testUserId, It.IsAny<CreateBannerRequestDto>()))
            .ReturnsAsync(createdBanner);

        // Act
        var result = await _controller.CreateBanner(createRequest);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(BannersController.GetBanner), createdResult.ActionName);
        _mockBannerService.Verify(s => s.CreateBannerAsync(_testShopId, _testUserId, It.IsAny<CreateBannerRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateBanner_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var createRequest = new CreateBannerRequestDto
        {
            Name = "",  // Invalid: empty name
            Description = "Description",
            Content = "Content",
            IsActive = true
        };

        // Act
        var result = await _controller.CreateBanner(createRequest);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetBanner Tests

    [Fact]
    public async Task GetBanner_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var banner = new BannerResponseDto
        {
            Id = _testBannerId,
            Name = "Test Banner",
            ShopId = _testShopId,
            UserId = _testUserId
        };

        _mockBannerService.Setup(s => s.GetBannerAsync(_testBannerId, _testShopId))
            .ReturnsAsync(banner);

        // Act
        var result = await _controller.GetBanner(_testBannerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetBanner_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidBannerId = Guid.NewGuid();
        _mockBannerService.Setup(s => s.GetBannerAsync(invalidBannerId, _testShopId))
            .ThrowsAsync(new InvalidOperationException("Banner not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetBanner(invalidBannerId)
        );
    }

    #endregion

    #region ListBanners Tests

    [Fact]
    public async Task ListBanners_WithValidPagination_ShouldReturnOk()
    {
        // Arrange
        var banners = new List<BannerResponseDto>
        {
            new BannerResponseDto { Id = Guid.NewGuid(), Name = "Banner 1", ShopId = _testShopId },
            new BannerResponseDto { Id = Guid.NewGuid(), Name = "Banner 2", ShopId = _testShopId }
        };

        _mockBannerService.Setup(s => s.ListBannersAsync(_testShopId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(banners);

        // Act
        var result = await _controller.ListBanners(1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ListBanners_WithPageZero_ShouldUseDefaultPage()
    {
        // Arrange
        var banners = new List<BannerResponseDto>();
        _mockBannerService.Setup(s => s.ListBannersAsync(_testShopId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(banners);

        // Act
        var result = await _controller.ListBanners(0, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ListBanners_WithNoBanners_ShouldReturnEmptyList()
    {
        // Arrange
        _mockBannerService.Setup(s => s.ListBannersAsync(_testShopId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<BannerResponseDto>());

        // Act
        var result = await _controller.ListBanners(1, 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region UpdateBanner Tests

    [Fact]
    public async Task UpdateBanner_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var updateRequest = new CreateBannerRequestDto
        {
            Name = "Updated Banner",
            Description = "Updated description",
            Content = "Updated content",
            IsActive = true
        };

        var updatedBanner = new BannerResponseDto
        {
            Id = _testBannerId,
            Name = updateRequest.Name,
            ShopId = _testShopId,
            UserId = _testUserId
        };

        _mockBannerService.Setup(s => s.UpdateBannerAsync(_testBannerId, _testShopId, It.IsAny<CreateBannerRequestDto>()))
            .ReturnsAsync(updatedBanner);

        // Act
        var result = await _controller.UpdateBanner(_testBannerId, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateBanner_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidBannerId = Guid.NewGuid();
        var updateRequest = new CreateBannerRequestDto
        {
            Name = "Updated",
            Description = "Description",
            Content = "Content",
            IsActive = true
        };

        _mockBannerService.Setup(s => s.UpdateBannerAsync(invalidBannerId, _testShopId, It.IsAny<CreateBannerRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Banner not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.UpdateBanner(invalidBannerId, updateRequest)
        );
    }

    #endregion

    #region DeleteBanner Tests

    [Fact]
    public async Task DeleteBanner_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        _mockBannerService.Setup(s => s.DeleteBannerAsync(_testBannerId, _testShopId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteBanner(_testBannerId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteBanner_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidBannerId = Guid.NewGuid();
        _mockBannerService.Setup(s => s.DeleteBannerAsync(invalidBannerId, _testShopId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteBanner(invalidBannerId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion
}
