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

public class AdvertisementControllerTests
{
    private readonly Mock<IAdvertisementService> _mockAdService;
    private readonly Mock<IAdvertisementRepository> _mockRepository;
    private readonly Mock<ILogger<AdvertisementController>> _mockLogger;
    private readonly AdvertisementController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testUserId;
    private readonly Guid _testAdId;

    public AdvertisementControllerTests()
    {
        _mockAdService = new Mock<IAdvertisementService>();
        _mockRepository = new Mock<IAdvertisementRepository>();
        _mockLogger = new Mock<ILogger<AdvertisementController>>();
        _testShopId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();
        _testAdId = Guid.NewGuid();
        _controller = new AdvertisementController(_mockAdService.Object, _mockRepository.Object, _mockLogger.Object);
    }

    #region CreateAd Tests

    [Fact]
    public async Task CreateAd_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new CreateAdvertisementDto
        {
            Title = "Test Ad",
            Description = "Test Description",
            Type = "Display"
        };

        var advertisement = new Advertisement
        {
            Id = _testAdId,
            ShopId = _testShopId,
            Title = request.Title,
            Type = AdType.Display
        };

        _mockAdService.Setup(s => s.CreateAdAsync(_testShopId, request.Title, request.Description, AdType.Display, _testUserId))
            .ReturnsAsync(advertisement);

        // Act
        var result = await _controller.CreateAd(_testShopId, request, _testUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CreateAd_WithInvalidType_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateAdvertisementDto
        {
            Title = "Test Ad",
            Description = "Test Description",
            Type = "InvalidType"
        };

        // Act
        var result = await _controller.CreateAd(_testShopId, request, _testUserId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetAd Tests

    [Fact]
    public async Task GetAd_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var advertisement = new Advertisement
        {
            Id = _testAdId,
            ShopId = _testShopId,
            Title = "Test Ad"
        };

        _mockAdService.Setup(s => s.GetAdAsync(_testAdId))
            .ReturnsAsync(advertisement);

        // Act
        var result = await _controller.GetAd(_testAdId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAd_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidAdId = Guid.NewGuid();
        _mockAdService.Setup(s => s.GetAdAsync(invalidAdId))
            .ThrowsAsync(new KeyNotFoundException("Advertisement not found"));

        // Act
        var result = await _controller.GetAd(invalidAdId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetShopAds Tests

    [Fact]
    public async Task GetShopAds_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var ads = new List<Advertisement>
        {
            new Advertisement { Id = Guid.NewGuid(), ShopId = _testShopId, Title = "Ad 1" },
            new Advertisement { Id = Guid.NewGuid(), ShopId = _testShopId, Title = "Ad 2" }
        };

        _mockAdService.Setup(s => s.GetShopAdsAsync(_testShopId))
            .ReturnsAsync(ads);

        // Act
        var result = await _controller.GetShopAds(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetShopAds_WithNoAds_ShouldReturnEmptyList()
    {
        // Arrange
        _mockAdService.Setup(s => s.GetShopAdsAsync(_testShopId))
            .ReturnsAsync(new List<Advertisement>());

        // Act
        var result = await _controller.GetShopAds(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region UpdateAd Tests

    [Fact]
    public async Task UpdateAd_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new UpdateAdvertisementDto
        {
            Title = "Updated Ad",
            Description = "Updated Description"
        };

        var advertisement = new Advertisement
        {
            Id = _testAdId,
            ShopId = _testShopId,
            Title = request.Title
        };

        _mockAdService.Setup(s => s.UpdateAdAsync(_testAdId, request.Title, request.Description, _testUserId))
            .ReturnsAsync(advertisement);

        // Act
        var result = await _controller.UpdateAd(_testAdId, request, _testUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UpdateAd_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidAdId = Guid.NewGuid();
        var request = new UpdateAdvertisementDto
        {
            Title = "Updated Ad",
            Description = "Updated Description"
        };

        _mockAdService.Setup(s => s.UpdateAdAsync(invalidAdId, request.Title, request.Description, _testUserId))
            .ThrowsAsync(new KeyNotFoundException("Advertisement not found"));

        // Act
        var result = await _controller.UpdateAd(invalidAdId, request, _testUserId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region ActivateAd Tests

    [Fact]
    public async Task ActivateAd_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var advertisement = new Advertisement
        {
            Id = _testAdId,
            ShopId = _testShopId,
            Title = "Test Ad"
        };

        _mockAdService.Setup(s => s.ActivateAdAsync(_testAdId, _testUserId))
            .ReturnsAsync(advertisement);

        // Act
        var result = await _controller.ActivateAd(_testAdId, _testUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region DeleteAd Tests

    [Fact]
    public async Task DeleteAd_WithValidId_ShouldReturnOk()
    {
        // Arrange
        _mockAdService.Setup(s => s.DeleteAdAsync(_testAdId, _testUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteAd(_testAdId, _testUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeleteAd_WithInvalidId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidAdId = Guid.NewGuid();
        _mockAdService.Setup(s => s.DeleteAdAsync(invalidAdId, _testUserId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteAd(invalidAdId, _testUserId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion
}
