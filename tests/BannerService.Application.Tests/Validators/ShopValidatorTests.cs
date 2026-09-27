namespace BannerService.Application.Tests.Validators;

using System;
using System.Threading.Tasks;
using Xunit;
using BannerService.Application.Validators;
using BannerService.Domain.Entities;

public class ShopValidatorTests
{
    private readonly ShopValidator _validator;

    public ShopValidatorTests()
    {
        _validator = new ShopValidator();
    }

    #region Name Validation Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyName_ShouldReturnError()
    {
        // Arrange
        var shop = new Shop { Name = "", City = "Mumbai", Address = "123 St", CountryCode = "IN", StateId = 1, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Shop.Name));
    }

    [Fact]
    public async Task ValidateAsync_WithValidName_ShouldPass()
    {
        // Arrange
        var shop = new Shop { Name = "Valid Shop", City = "Mumbai", Address = "123 St", CountryCode = "IN", StateId = 1, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Shop.Name));
    }

    [Fact]
    public async Task ValidateAsync_WithNameExceedingMaxLength_ShouldReturnError()
    {
        // Arrange
        var shop = new Shop
        {
            Name = new string('A', 256),  // Exceeds typical max length
            City = "Mumbai",
            Address = "123 St",
            CountryCode = "IN",
            StateId = 1,
            DistrictId = 1
        };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion

    #region City Validation Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyCity_ShouldReturnError()
    {
        // Arrange
        var shop = new Shop { Name = "Shop", City = "", Address = "123 St", CountryCode = "IN", StateId = 1, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Shop.City));
    }

    [Fact]
    public async Task ValidateAsync_WithValidCity_ShouldPass()
    {
        // Arrange
        var shop = new Shop { Name = "Shop", City = "Mumbai", Address = "123 St", CountryCode = "IN", StateId = 1, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Shop.City));
    }

    #endregion

    #region Address Validation Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyAddress_ShouldReturnError()
    {
        // Arrange
        var shop = new Shop { Name = "Shop", City = "Mumbai", Address = "", CountryCode = "IN", StateId = 1, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Shop.Address));
    }

    [Fact]
    public async Task ValidateAsync_WithValidAddress_ShouldPass()
    {
        // Arrange
        var shop = new Shop { Name = "Shop", City = "Mumbai", Address = "123 Main St", CountryCode = "IN", StateId = 1, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Shop.Address));
    }

    #endregion

    #region Country Code Validation Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyCountryCode_ShouldReturnError()
    {
        // Arrange
        var shop = new Shop { Name = "Shop", City = "Mumbai", Address = "123 St", CountryCode = "", StateId = 1, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithValidCountryCode_ShouldPass()
    {
        // Arrange
        var shop = new Shop { Name = "Shop", City = "Mumbai", Address = "123 St", CountryCode = "IN", StateId = 1, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(Shop.CountryCode));
    }

    #endregion

    #region State and District Validation Tests

    [Fact]
    public async Task ValidateAsync_WithInvalidStateId_ShouldReturnError()
    {
        // Arrange
        var shop = new Shop { Name = "Shop", City = "Mumbai", Address = "123 St", CountryCode = "IN", StateId = 0, DistrictId = 1 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidDistrictId_ShouldReturnError()
    {
        // Arrange
        var shop = new Shop { Name = "Shop", City = "Mumbai", Address = "123 St", CountryCode = "IN", StateId = 1, DistrictId = 0 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion

    #region Comprehensive Validation Tests

    [Fact]
    public async Task ValidateAsync_WithAllValidData_ShouldPass()
    {
        // Arrange
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "Complete Shop",
            City = "Mumbai",
            Address = "123 Main St, Floor 5",
            CountryCode = "IN",
            StateId = 1,
            DistrictId = 1,
            PostalCode = "400001",
            PhoneNumber = "+91-22-1234-5678",
            Website = "https://shop.example.com",
            Status = ShopStatus.Active,
            OwnerUserId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleErrors_ShouldReturnAll()
    {
        // Arrange
        var shop = new Shop { Name = "", City = "", Address = "", CountryCode = "", StateId = 0, DistrictId = 0 };

        // Act
        var result = await _validator.ValidateAsync(shop);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    #endregion
}
