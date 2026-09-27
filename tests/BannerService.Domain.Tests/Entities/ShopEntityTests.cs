namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class ShopEntityTests
{
    #region Constructor Tests

    [Fact]
    public void CreateShop_WithDefaults_ShouldInitializeProperties()
    {
        // Act
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop",
            Status = ShopStatus.Active,
            CreatedByUserId = Guid.NewGuid()
        };

        // Assert
        Assert.Equal("Test Shop", shop.Name);
        Assert.Equal(ShopStatus.Active, shop.Status);
        Assert.NotEqual(Guid.Empty, shop.Id);
    }

    #endregion

    #region UpdateBasicInfo Tests

    [Fact]
    public void UpdateBasicInfo_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var shop = new Shop { Name = "Old Name", Description = "Old Desc" };
        var originalTime = shop.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(10);
        shop.UpdateBasicInfo("New Name", "New Desc");

        // Assert
        Assert.Equal("New Name", shop.Name);
        Assert.Equal("New Desc", shop.Description);
        Assert.True(shop.UpdatedAt > originalTime);
    }

    #endregion

    #region UpdateLocation Tests

    [Fact]
    public void UpdateLocation_ShouldUpdateAllLocationFields()
    {
        // Arrange
        var shop = new Shop();

        // Act
        shop.UpdateLocation("123 Main St", "New York", "US", 1, 2, "10001", 40.7128, -74.0060);

        // Assert
        Assert.Equal("123 Main St", shop.Address);
        Assert.Equal("New York", shop.City);
        Assert.Equal("US", shop.CountryCode);
        Assert.Equal(1, shop.StateId);
        Assert.Equal(2, shop.DistrictId);
        Assert.Equal("10001", shop.PostalCode);
        Assert.Equal(40.7128, shop.Latitude);
        Assert.Equal(-74.0060, shop.Longitude);
    }

    [Fact]
    public void UpdateLocation_ShouldUpdateModificationTime()
    {
        // Arrange
        var shop = new Shop();
        var originalTime = shop.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(10);
        shop.UpdateLocation("Address", "City", null, null, null, null, null, null);

        // Assert
        Assert.True(shop.UpdatedAt > originalTime);
    }

    #endregion

    #region UpdateContactInfo Tests

    [Fact]
    public void UpdateContactInfo_ShouldUpdatePhoneAndWebsite()
    {
        // Arrange
        var shop = new Shop();

        // Act
        shop.UpdateContactInfo("1234567890", "https://example.com");

        // Assert
        Assert.Equal("1234567890", shop.PhoneNumber);
        Assert.Equal("https://example.com", shop.Website);
    }

    #endregion

    #region SetStatus Tests

    [Fact]
    public void SetStatus_ShouldChangeStatus()
    {
        // Arrange
        var shop = new Shop { Status = ShopStatus.Active };

        // Act
        shop.SetStatus(ShopStatus.Inactive);

        // Assert
        Assert.Equal(ShopStatus.Inactive, shop.Status);
    }

    [Fact]
    public void SetStatus_ShouldUpdateModificationTime()
    {
        // Arrange
        var shop = new Shop();
        var originalTime = shop.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(10);
        shop.SetStatus(ShopStatus.Archived);

        // Assert
        Assert.True(shop.UpdatedAt > originalTime);
    }

    #endregion

    #region AssignOwner Tests

    [Fact]
    public void AssignOwner_ShouldSetOwnerUserId()
    {
        // Arrange
        var shop = new Shop();
        var ownerId = Guid.NewGuid();

        // Act
        shop.AssignOwner(ownerId);

        // Assert
        Assert.Equal(ownerId, shop.OwnerUserId);
    }

    #endregion

    #region RemoveOwner Tests

    [Fact]
    public void RemoveOwner_ShouldSetOwnerUserIdToNull()
    {
        // Arrange
        var shop = new Shop { OwnerUserId = Guid.NewGuid() };

        // Act
        shop.RemoveOwner();

        // Assert
        Assert.Null(shop.OwnerUserId);
    }

    #endregion

    #region IsArchived Property Tests

    [Fact]
    public void IsArchived_WithArchivedStatus_ShouldReturnTrue()
    {
        // Arrange
        var shop = new Shop { Status = ShopStatus.Archived };

        // Act
        var result = shop.IsArchived;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsArchived_WithActiveStatus_ShouldReturnFalse()
    {
        // Arrange
        var shop = new Shop { Status = ShopStatus.Active };

        // Act
        var result = shop.IsArchived;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsActive Property Tests

    [Fact]
    public void IsActive_WithActiveStatus_ShouldReturnTrue()
    {
        // Arrange
        var shop = new Shop { Status = ShopStatus.Active };

        // Act
        var result = shop.IsActive;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsActive_WithInactiveStatus_ShouldReturnFalse()
    {
        // Arrange
        var shop = new Shop { Status = ShopStatus.Inactive };

        // Act
        var result = shop.IsActive;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsInactive Property Tests

    [Fact]
    public void IsInactive_WithInactiveStatus_ShouldReturnTrue()
    {
        // Arrange
        var shop = new Shop { Status = ShopStatus.Inactive };

        // Act
        var result = shop.IsInactive;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInactive_WithActiveStatus_ShouldReturnFalse()
    {
        // Arrange
        var shop = new Shop { Status = ShopStatus.Active };

        // Act
        var result = shop.IsInactive;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Hierarchy Tests

    [Fact]
    public void CreateShopHierarchy_WithParentChild_ShouldMaintainRelationship()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var shop = new Shop { ParentShopId = parentId };

        // Act
        var hasParent = shop.ParentShopId.HasValue;

        // Assert
        Assert.True(hasParent);
        Assert.Equal(parentId, shop.ParentShopId);
    }

    [Fact]
    public void ChildShops_ShouldBeInitializedAsEmptyCollection()
    {
        // Arrange
        var shop = new Shop();

        // Act
        var hasChildren = shop.ChildShops != null;

        // Assert
        Assert.True(hasChildren);
        Assert.Empty(shop.ChildShops);
    }

    #endregion
}
