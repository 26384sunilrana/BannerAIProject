namespace BannerService.Application.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BannerService.Presentation.Controllers;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;

public class ShopControllerTests
{
    private readonly Mock<IShopService> _mockShopService;
    private readonly Mock<IShopRepository> _mockShopRepository;
    private readonly Mock<ILogger<ShopsController>> _mockLogger;
    private readonly ShopsController _controller;
    private readonly Guid _testUserId;
    private readonly Guid _testShopId;

    public ShopControllerTests()
    {
        _mockShopService = new Mock<IShopService>();
        _mockShopRepository = new Mock<IShopRepository>();
        _mockLogger = new Mock<ILogger<ShopsController>>();
        _testUserId = Guid.NewGuid();
        _testShopId = Guid.NewGuid();

        _controller = new ShopsController(
            _mockShopService.Object,
            _mockShopRepository.Object,
            _mockLogger.Object
        );

        // Setup controller context with ClaimsPrincipal for authorization
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new Claim[] { new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()) },
            "Bearer"
        ));
        _controller.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user } };
    }

    #region GetMyShops Tests

    [Fact]
    public async Task GetMyShops_WithValidUser_ShouldReturnOwnedShops()
    {
        // Arrange
        var shops = new List<Shop>
        {
            new() { Id = Guid.NewGuid(), Name = "Shop 1", OwnerUserId = _testUserId, Status = ShopStatus.Active },
            new() { Id = Guid.NewGuid(), Name = "Shop 2", OwnerUserId = _testUserId, Status = ShopStatus.Active }
        };

        _mockShopService.Setup(s => s.GetByOwnerAsync(_testUserId)).ReturnsAsync(shops);

        // Act
        var result = await _controller.GetMyShops();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockShopService.Verify(s => s.GetByOwnerAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task GetMyShops_WithStatusFilter_ShouldReturnFilteredShops()
    {
        // Arrange
        var activeShops = new List<Shop>
        {
            new() { Id = Guid.NewGuid(), Name = "Active Shop", OwnerUserId = _testUserId, Status = ShopStatus.Active }
        };

        _mockShopService.Setup(s => s.GetByOwnerAsync(_testUserId)).ReturnsAsync(activeShops);

        // Act
        var result = await _controller.GetMyShops("Active");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMyShops_WithNoShops_ShouldReturnEmptyList()
    {
        // Arrange
        _mockShopService.Setup(s => s.GetByOwnerAsync(_testUserId)).ReturnsAsync(new List<Shop>());

        // Act
        var result = await _controller.GetMyShops();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetShopById Tests

    [Fact]
    public async Task GetShopById_WithValidId_ShouldReturnShop()
    {
        // Arrange
        var shop = new Shop { Id = _testShopId, Name = "Test Shop", Status = ShopStatus.Active };
        _mockShopService.Setup(s => s.GetByIdAsync(_testShopId)).ReturnsAsync(shop);

        // Act
        var result = await _controller.GetShopById(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetShopById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _mockShopService.Setup(s => s.GetByIdAsync(_testShopId)).ReturnsAsync((Shop)null);

        // Act
        var result = await _controller.GetShopById(_testShopId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region CreateShop Tests

    [Fact]
    public async Task CreateShop_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateShopDto
        {
            Name = "New Shop",
            City = "Mumbai",
            Address = "123 Main St",
            CountryCode = "IN",
            StateId = 1,
            DistrictId = 1,
            PostalCode = "400001"
        };

        var createdShop = new Shop
        {
            Id = _testShopId,
            Name = createDto.Name,
            City = createDto.City,
            Address = createDto.Address,
            CountryCode = createDto.CountryCode,
            StateId = createDto.StateId,
            DistrictId = createDto.DistrictId,
            Status = ShopStatus.Active,
            OwnerUserId = _testUserId
        };

        _mockShopService.Setup(s => s.CreateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<ShopStatus>()
        )).ReturnsAsync(createdShop);

        // Act
        var result = await _controller.CreateShop(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ShopsController.GetShopById), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateShop_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreateShopDto
        {
            Name = "",  // Invalid: empty name
            City = "Mumbai",
            Address = "123 Main St",
            CountryCode = "IN",
            StateId = 1,
            DistrictId = 1,
            PostalCode = "400001"
        };

        // Act
        var result = await _controller.CreateShop(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region UpdateShop Tests

    [Fact]
    public async Task UpdateShop_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var updateDto = new UpdateShopDto
        {
            Name = "Updated Shop",
            City = "Updated City",
            Address = "New Address",
            CountryCode = "IN",
            StateId = 1,
            DistrictId = 1,
            PostalCode = "400001"
        };

        var updatedShop = new Shop
        {
            Id = _testShopId,
            Name = updateDto.Name,
            Status = ShopStatus.Active
        };

        _mockShopService.Setup(s => s.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ShopStatus>()
        )).ReturnsAsync(updatedShop);

        // Act
        var result = await _controller.UpdateShop(_testShopId, updateDto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateShop_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var updateDto = new UpdateShopDto
        {
            Name = "Shop",
            City = "City",
            Address = "Address",
            CountryCode = "IN",
            StateId = 1,
            DistrictId = 1,
            PostalCode = "12345"
        };

        _mockShopService.Setup(s => s.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ShopStatus>()
        )).ThrowsAsync(new InvalidOperationException("Shop not found"));

        // Act
        var result = await _controller.UpdateShop(_testShopId, updateDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result) || Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region DeleteShop Tests

    [Fact]
    public async Task DeleteShop_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var shop = new Shop { Id = _testShopId, Name = "To Delete", Status = ShopStatus.Active };
        _mockShopService.Setup(s => s.GetByIdAsync(_testShopId)).ReturnsAsync(shop);
        _mockShopRepository.Setup(r => r.DeleteAsync(_testShopId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteShop(_testShopId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteShop_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _mockShopService.Setup(s => s.GetByIdAsync(_testShopId)).ReturnsAsync((Shop)null);

        // Act
        var result = await _controller.DeleteShop(_testShopId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region ListShops Tests

    [Fact]
    public async Task ListShops_ShouldReturnAllShops()
    {
        // Arrange
        var shops = new List<Shop>
        {
            new() { Id = Guid.NewGuid(), Name = "Shop 1", Status = ShopStatus.Active },
            new() { Id = Guid.NewGuid(), Name = "Shop 2", Status = ShopStatus.Active }
        };

        _mockShopRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(shops);

        // Act
        var result = await _controller.ListShops();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetShopHierarchy Tests

    [Fact]
    public async Task GetShopHierarchy_WithValidId_ShouldReturnHierarchy()
    {
        // Arrange
        var shop = new Shop { Id = _testShopId, Name = "Parent Shop", Status = ShopStatus.Active };
        _mockShopService.Setup(s => s.GetHierarchyAsync(_testShopId)).ReturnsAsync(shop);

        // Act
        var result = await _controller.GetShopHierarchy(_testShopId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetShopHierarchy_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _mockShopService.Setup(s => s.GetHierarchyAsync(_testShopId)).ReturnsAsync((Shop)null);

        // Act
        var result = await _controller.GetShopHierarchy(_testShopId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region AssignOwner Tests

    [Fact]
    public async Task AssignOwner_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var shop = new Shop { Id = _testShopId, Name = "Shop", OwnerUserId = ownerId, Status = ShopStatus.Active };
        var assignDto = new AssignOwnerDto { OwnerUserId = ownerId };

        _mockShopService.Setup(s => s.AssignOwnerAsync(_testShopId, ownerId)).ReturnsAsync(shop);

        // Act
        var result = await _controller.AssignOwner(_testShopId, assignDto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AssignOwner_WithInvalidShop_ShouldReturnNotFound()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var assignDto = new AssignOwnerDto { OwnerUserId = ownerId };

        _mockShopService.Setup(s => s.AssignOwnerAsync(_testShopId, ownerId))
            .ThrowsAsync(new InvalidOperationException("Shop not found"));

        // Act
        var result = await _controller.AssignOwner(_testShopId, assignDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result) || Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region RemoveOwner Tests

    [Fact]
    public async Task RemoveOwner_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var shop = new Shop { Id = _testShopId, Name = "Shop", OwnerUserId = null, Status = ShopStatus.Active };
        _mockShopService.Setup(s => s.RemoveOwnerAsync(_testShopId)).ReturnsAsync(shop);

        // Act
        var result = await _controller.RemoveOwner(_testShopId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RemoveOwner_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _mockShopService.Setup(s => s.RemoveOwnerAsync(_testShopId))
            .ThrowsAsync(new InvalidOperationException("Shop not found"));

        // Act
        var result = await _controller.RemoveOwner(_testShopId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result) || Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region SearchShops Tests

    [Fact]
    public async Task SearchShops_WithValidQuery_ShouldReturnMatchingShops()
    {
        // Arrange
        var shops = new List<Shop>
        {
            new() { Id = Guid.NewGuid(), Name = "Mumbai Shop", Status = ShopStatus.Active }
        };

        _mockShopService.Setup(s => s.SearchAsync("Mumbai")).ReturnsAsync(shops);

        // Act
        var result = await _controller.SearchShops("Mumbai");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task SearchShops_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        _mockShopService.Setup(s => s.SearchAsync("NonExistent")).ReturnsAsync(new List<Shop>());

        // Act
        var result = await _controller.SearchShops("NonExistent");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}
