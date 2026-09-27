namespace BannerService.Application.Tests.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using BannerService.Application.Services;
using BannerService.Application.Validators;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;

public class ShopServiceTests
{
    private readonly Mock<IShopRepository> _mockShopRepository;
    private readonly Mock<IAddressRepository> _mockAddressRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<ShopService>> _mockLogger;
    private readonly ShopService _service;
    private readonly Guid _testUserId;

    public ShopServiceTests()
    {
        _mockShopRepository = new Mock<IShopRepository>();
        _mockAddressRepository = new Mock<IAddressRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<ShopService>>();

        _testUserId = Guid.NewGuid();

        _service = new ShopService(
            _mockShopRepository.Object,
            _mockAddressRepository.Object,
            _mockUserRepository.Object,
            _mockLogger.Object
        );
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDataAndCascadingAddress_ShouldSucceed()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var countryCode = "IN";
        var stateId = 1;
        var districtId = 1;

        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "New Shop",
            City = "Mumbai",
            Address = "123 Main St",
            CountryCode = countryCode,
            StateId = stateId,
            DistrictId = districtId,
            PostalCode = "400001",
            OwnerUserId = ownerId,
            Status = ShopStatus.Active
        };

        var country = new Country { ISOCode = countryCode, Name = "India", IsActive = true };
        var state = new State { Id = stateId, CountryCode = countryCode, Name = "Maharashtra", IsActive = true };
        var district = new District { Id = districtId, StateId = stateId, Name = "Mumbai", IsActive = true };

        _mockAddressRepository.Setup(r => r.GetCountryAsync(countryCode)).ReturnsAsync(country);
        _mockAddressRepository.Setup(r => r.GetStateAsync(stateId)).ReturnsAsync(state);
        _mockAddressRepository.Setup(r => r.GetDistrictAsync(districtId)).ReturnsAsync(district);
        _mockUserRepository.Setup(r => r.GetByIdAsync(ownerId)).ReturnsAsync(new User { Id = ownerId });
        _mockShopRepository.Setup(r => r.CreateAsync(It.IsAny<Shop>())).ReturnsAsync(shop);

        // Act
        var result = await _service.CreateAsync(
            "New Shop",
            "Shop description",
            "123 Main St",
            "Mumbai",
            countryCode,
            stateId,
            districtId,
            "400001",
            null,
            null,
            ownerId,
            null,
            ShopStatus.Active
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Shop", result.Name);
        Assert.Equal(countryCode, result.CountryCode);
        Assert.Equal(stateId, result.StateId);
        Assert.Equal(districtId, result.DistrictId);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCountryCode_ShouldThrow()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        _mockAddressRepository.Setup(r => r.GetCountryAsync("XX")).ReturnsAsync((Country)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(
                "Shop",
                "Desc",
                "Address",
                "City",
                "XX",
                1,
                1,
                "12345",
                null,
                null,
                ownerId,
                null,
                ShopStatus.Active
            )
        );
    }

    [Fact]
    public async Task CreateAsync_WithInvalidStateId_ShouldThrow()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var country = new Country { ISOCode = "IN", Name = "India", IsActive = true };
        _mockAddressRepository.Setup(r => r.GetCountryAsync("IN")).ReturnsAsync(country);
        _mockAddressRepository.Setup(r => r.GetStateAsync(9999)).ReturnsAsync((State)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(
                "Shop",
                "Desc",
                "Address",
                "City",
                "IN",
                9999,
                1,
                "12345",
                null,
                null,
                ownerId,
                null,
                ShopStatus.Active
            )
        );
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentOwner_ShouldThrow()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var country = new Country { ISOCode = "IN", Name = "India", IsActive = true };
        var state = new State { Id = 1, CountryCode = "IN", Name = "Maharashtra", IsActive = true };
        var district = new District { Id = 1, StateId = 1, Name = "Mumbai", IsActive = true };

        _mockAddressRepository.Setup(r => r.GetCountryAsync("IN")).ReturnsAsync(country);
        _mockAddressRepository.Setup(r => r.GetStateAsync(1)).ReturnsAsync(state);
        _mockAddressRepository.Setup(r => r.GetDistrictAsync(1)).ReturnsAsync(district);
        _mockUserRepository.Setup(r => r.GetByIdAsync(ownerId)).ReturnsAsync((User)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(
                "Shop",
                "Desc",
                "Address",
                "City",
                "IN",
                1,
                1,
                "12345",
                null,
                null,
                ownerId,
                null,
                ShopStatus.Active
            )
        );
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateShop()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var shop = new Shop
        {
            Id = shopId,
            Name = "Original",
            CountryCode = "IN",
            StateId = 1,
            DistrictId = 1,
            Status = ShopStatus.Active
        };

        var country = new Country { ISOCode = "IN", Name = "India", IsActive = true };
        var state = new State { Id = 1, CountryCode = "IN", Name = "Maharashtra", IsActive = true };
        var district = new District { Id = 1, StateId = 1, Name = "Mumbai", IsActive = true };

        _mockAddressRepository.Setup(r => r.GetCountryAsync("IN")).ReturnsAsync(country);
        _mockAddressRepository.Setup(r => r.GetStateAsync(1)).ReturnsAsync(state);
        _mockAddressRepository.Setup(r => r.GetDistrictAsync(1)).ReturnsAsync(district);
        _mockShopRepository.Setup(r => r.UpdateAsync(It.IsAny<Shop>())).ReturnsAsync(shop);

        // Act
        var result = await _service.UpdateAsync(
            shopId,
            "Updated Name",
            "New Desc",
            "New Address",
            "New City",
            "IN",
            1,
            1,
            "12345",
            null,
            null,
            ShopStatus.Active
        );

        // Assert
        Assert.NotNull(result);
        _mockShopRepository.Verify(r => r.UpdateAsync(It.IsAny<Shop>()), Times.Once);
    }

    #endregion

    #region GetByOwnerAsync Tests

    [Fact]
    public async Task GetByOwnerAsync_WithValidOwnerId_ShouldReturnOwnedShops()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var shops = new List<Shop>
        {
            new() { Id = Guid.NewGuid(), Name = "Shop 1", OwnerUserId = ownerId, Status = ShopStatus.Active },
            new() { Id = Guid.NewGuid(), Name = "Shop 2", OwnerUserId = ownerId, Status = ShopStatus.Active }
        };

        _mockShopRepository.Setup(r => r.GetByOwnerAsync(ownerId)).ReturnsAsync(shops);

        // Act
        var result = await _service.GetByOwnerAsync(ownerId);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(ownerId, s.OwnerUserId));
    }

    [Fact]
    public async Task GetByOwnerAsync_WithNoShops_ShouldReturnEmptyList()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        _mockShopRepository.Setup(r => r.GetByOwnerAsync(ownerId)).ReturnsAsync(new List<Shop>());

        // Act
        var result = await _service.GetByOwnerAsync(ownerId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ArchiveAsync Tests

    [Fact]
    public async Task ArchiveAsync_WithActiveShop_ShouldChangeStatusToArchived()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var shop = new Shop
        {
            Id = shopId,
            Name = "Shop To Archive",
            CountryCode = "IN",
            StateId = 1,
            DistrictId = 1,
            Status = ShopStatus.Active
        };

        var country = new Country { ISOCode = "IN", Name = "India", IsActive = true };
        var state = new State { Id = 1, CountryCode = "IN", Name = "Maharashtra", IsActive = true };
        var district = new District { Id = 1, StateId = 1, Name = "Mumbai", IsActive = true };

        _mockShopRepository.Setup(r => r.GetByIdAsync(shopId)).ReturnsAsync(shop);
        _mockAddressRepository.Setup(r => r.GetCountryAsync("IN")).ReturnsAsync(country);
        _mockAddressRepository.Setup(r => r.GetStateAsync(1)).ReturnsAsync(state);
        _mockAddressRepository.Setup(r => r.GetDistrictAsync(1)).ReturnsAsync(district);
        _mockShopRepository.Setup(r => r.UpdateAsync(It.IsAny<Shop>())).ReturnsAsync(shop);

        // Act
        var result = await _service.ArchiveAsync(shopId);

        // Assert
        Assert.NotNull(result);
        _mockShopRepository.Verify(r => r.UpdateAsync(It.IsAny<Shop>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_WithInvalidId_ShouldThrow()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        _mockShopRepository.Setup(r => r.GetByIdAsync(shopId)).ReturnsAsync((Shop)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ArchiveAsync(shopId)
        );
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnShop()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var shop = new Shop { Id = shopId, Name = "Test Shop", Status = ShopStatus.Active };
        _mockShopRepository.Setup(r => r.GetByIdAsync(shopId)).ReturnsAsync(shop);

        // Act
        var result = await _service.GetByIdAsync(shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Shop", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        _mockShopRepository.Setup(r => r.GetByIdAsync(shopId)).ReturnsAsync((Shop)null);

        // Act
        var result = await _service.GetByIdAsync(shopId);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
