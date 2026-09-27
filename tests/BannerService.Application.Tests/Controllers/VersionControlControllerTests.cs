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

public class VersionControlControllerTests
{
    private readonly Mock<IVersionControlService> _mockVersionControlService;
    private readonly Mock<ILogger<VersionControlController>> _mockLogger;
    private readonly VersionControlController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testUserId;
    private readonly Guid _testBannerId;

    public VersionControlControllerTests()
    {
        _mockVersionControlService = new Mock<IVersionControlService>();
        _mockLogger = new Mock<ILogger<VersionControlController>>();
        _testShopId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();
        _testBannerId = Guid.NewGuid();

        _controller = new VersionControlController(_mockVersionControlService.Object, _mockLogger.Object);

        // Setup controller context with claims
        var claims = new List<Claim>
        {
            new Claim("shop_id", _testShopId.ToString()),
            new Claim("sub", _testUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region ListVersions Tests

    [Fact]
    public async Task ListVersions_WithValidBannerId_ShouldReturnOkWithVersions()
    {
        // Arrange
        var versions = new List<BannerVersionDto>
        {
            new BannerVersionDto { BannerId = _testBannerId, VersionNumber = 1 },
            new BannerVersionDto { BannerId = _testBannerId, VersionNumber = 2 }
        };

        _mockVersionControlService.Setup(s => s.ListVersionsAsync(_testBannerId, _testShopId))
            .ReturnsAsync(versions);

        // Act
        var result = await _controller.ListVersions(_testBannerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ListVersions_WithNoVersions_ShouldReturnEmptyList()
    {
        // Arrange
        _mockVersionControlService.Setup(s => s.ListVersionsAsync(_testBannerId, _testShopId))
            .ReturnsAsync(new List<BannerVersionDto>());

        // Act
        var result = await _controller.ListVersions(_testBannerId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetVersion Tests

    [Fact]
    public async Task GetVersion_WithValidVersionNumber_ShouldReturnOk()
    {
        // Arrange
        var version = new BannerVersionDetailDto
        {
            BannerId = _testBannerId,
            VersionNumber = 1,
            Content = "Test content"
        };

        _mockVersionControlService.Setup(s => s.GetVersionDetailAsync(_testBannerId, 1, _testShopId))
            .ReturnsAsync(version);

        // Act
        var result = await _controller.GetVersion(_testBannerId, 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetVersion_WithZeroVersionNumber_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetVersion(_testBannerId, 0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetVersion_WithNegativeVersionNumber_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetVersion(_testBannerId, -1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetVersion_WithNonExistentVersion_ShouldReturnNotFound()
    {
        // Arrange
        _mockVersionControlService.Setup(s => s.GetVersionDetailAsync(_testBannerId, 999, _testShopId))
            .ReturnsAsync((BannerVersionDetailDto)null);

        // Act
        var result = await _controller.GetVersion(_testBannerId, 999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region RestoreVersion Tests

    [Fact]
    public async Task RestoreVersion_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var banner = new BannerResponseDto
        {
            Id = _testBannerId,
            Name = "Restored Banner",
            ShopId = _testShopId
        };

        _mockVersionControlService.Setup(s => s.RestoreVersionAsync(_testBannerId, 2, _testShopId, _testUserId))
            .ReturnsAsync(new BannerEntity { Id = _testBannerId });

        // Act
        var result = await _controller.RestoreVersion(_testBannerId, 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task RestoreVersion_WithZeroVersionNumber_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.RestoreVersion(_testBannerId, 0);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RestoreVersion_WithNonExistentVersion_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockVersionControlService.Setup(s => s.RestoreVersionAsync(_testBannerId, 999, _testShopId, _testUserId))
            .ThrowsAsync(new KeyNotFoundException("Version not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.RestoreVersion(_testBannerId, 999)
        );
    }

    #endregion
}

public class BannerEntity
{
    public Guid Id { get; set; }
}
