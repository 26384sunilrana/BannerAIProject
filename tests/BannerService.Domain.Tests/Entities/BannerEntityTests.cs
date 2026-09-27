namespace BannerService.Domain.Tests.Entities;

using System;
using System.Linq;
using Xunit;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;

public class BannerEntityTests
{
    private readonly Guid _testShopId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    #region Banner Construction Tests

    [Fact]
    public void CreateBanner_WithValidData_ShouldInitializeProperties()
    {
        // Act
        var banner = new Banner(_testShopId, _testUserId, "Test Banner", "Description", 1920, 1080);

        // Assert
        Assert.NotEqual(Guid.Empty, banner.Id);
        Assert.Equal(_testShopId, banner.ShopId);
        Assert.Equal(_testUserId, banner.UserId);
        Assert.Equal("Test Banner", banner.Name);
        Assert.Equal("Description", banner.Description);
        Assert.Equal(1920, banner.Width);
        Assert.Equal(1080, banner.Height);
        Assert.False(banner.IsPublished);
        Assert.NotEqual(DateTime.MinValue, banner.CreatedAt);
        Assert.NotEqual(DateTime.MinValue, banner.UpdatedAt);
    }

    [Fact]
    public void CreateBanner_ShouldHaveEmptyComponentsList()
    {
        // Act
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);

        // Assert
        Assert.Empty(banner.Components);
    }

    #endregion

    #region AddComponent Tests

    [Fact]
    public void AddComponent_WithValidComponent_ShouldAddToCollection()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var position = new Position(10, 20);
        var size = new Size(100, 50);
        var component = new Component(Guid.NewGuid(), position, size, 1, "{}");

        // Act
        banner.AddComponent(component);

        // Assert
        Assert.Single(banner.Components);
        Assert.Contains(component, banner.Components);
    }

    [Fact]
    public void AddComponent_WithMultipleComponents_ShouldAddAll()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var components = new[]
        {
            new Component(Guid.NewGuid(), new Position(0, 0), new Size(100, 100), 1, "{}"),
            new Component(Guid.NewGuid(), new Position(10, 10), new Size(100, 100), 2, "{}"),
            new Component(Guid.NewGuid(), new Position(20, 20), new Size(100, 100), 3, "{}")
        };

        // Act
        foreach (var component in components)
            banner.AddComponent(component);

        // Assert
        Assert.Equal(3, banner.Components.Count);
    }

    [Fact]
    public void AddComponent_WithDuplicateZIndex_ShouldThrowException()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var component1 = new Component(Guid.NewGuid(), new Position(0, 0), new Size(100, 100), 1, "{}");
        var component2 = new Component(Guid.NewGuid(), new Position(10, 10), new Size(100, 100), 1, "{}");

        banner.AddComponent(component1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => banner.AddComponent(component2));
    }

    [Fact]
    public void AddComponent_Exceeding50Components_ShouldThrowException()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);

        // Add 50 components
        for (int i = 0; i < 50; i++)
        {
            var component = new Component(Guid.NewGuid(), new Position(i, i), new Size(10, 10), i + 1, "{}");
            banner.AddComponent(component);
        }

        // Act & Assert
        var extraComponent = new Component(Guid.NewGuid(), new Position(50, 50), new Size(10, 10), 51, "{}");
        Assert.Throws<InvalidOperationException>(() => banner.AddComponent(extraComponent));
    }

    [Fact]
    public void AddComponent_ShouldUpdateModificationTime()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var originalUpdateTime = banner.UpdatedAt;
        System.Threading.Thread.Sleep(10);  // Ensure time difference

        var component = new Component(Guid.NewGuid(), new Position(0, 0), new Size(100, 100), 1, "{}");

        // Act
        banner.AddComponent(component);

        // Assert
        Assert.True(banner.UpdatedAt > originalUpdateTime);
    }

    #endregion

    #region RemoveComponent Tests

    [Fact]
    public void RemoveComponent_WithValidId_ShouldRemoveFromCollection()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var component = new Component(Guid.NewGuid(), new Position(0, 0), new Size(100, 100), 1, "{}");
        banner.AddComponent(component);

        // Act
        banner.RemoveComponent(component.Id);

        // Assert
        Assert.Empty(banner.Components);
    }

    [Fact]
    public void RemoveComponent_WithNonexistentId_ShouldThrowException()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var nonexistentId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => banner.RemoveComponent(nonexistentId));
    }

    [Fact]
    public void RemoveComponent_ShouldUpdateModificationTime()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var component = new Component(Guid.NewGuid(), new Position(0, 0), new Size(100, 100), 1, "{}");
        banner.AddComponent(component);
        var originalUpdateTime = banner.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        banner.RemoveComponent(component.Id);

        // Assert
        Assert.True(banner.UpdatedAt > originalUpdateTime);
    }

    #endregion

    #region UpdateComponent Tests

    [Fact]
    public void UpdateComponent_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var component = new Component(Guid.NewGuid(), new Position(0, 0), new Size(100, 100), 1, "{}");
        banner.AddComponent(component);

        var newPosition = new Position(50, 50);
        var newSize = new Size(200, 200);
        var newZIndex = 2;

        // Act
        banner.UpdateComponent(component.Id, newPosition, newSize, newZIndex, "{}");

        // Assert
        var updated = banner.Components.First();
        Assert.Equal(newZIndex, updated.ZIndex);
    }

    [Fact]
    public void UpdateComponent_WithDuplicateZIndex_ShouldThrowException()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var component1 = new Component(Guid.NewGuid(), new Position(0, 0), new Size(100, 100), 1, "{}");
        var component2 = new Component(Guid.NewGuid(), new Position(10, 10), new Size(100, 100), 2, "{}");
        banner.AddComponent(component1);
        banner.AddComponent(component2);

        // Act & Assert - Try to give component1 the same ZIndex as component2
        var position = new Position(0, 0);
        var size = new Size(100, 100);
        Assert.Throws<InvalidOperationException>(() =>
            banner.UpdateComponent(component1.Id, position, size, 2, "{}")
        );
    }

    [Fact]
    public void UpdateComponent_WithNonexistentId_ShouldThrowException()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var nonexistentId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            banner.UpdateComponent(nonexistentId, new Position(0, 0), new Size(100, 100), 1, "{}")
        );
    }

    #endregion

    #region Publish/Unpublish Tests

    [Fact]
    public void Publish_WhenUnpublished_ShouldSetIsPublishedTrue()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        Assert.False(banner.IsPublished);

        // Act
        banner.Publish();

        // Assert
        Assert.True(banner.IsPublished);
    }

    [Fact]
    public void Unpublish_WhenPublished_ShouldSetIsPublishedFalse()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        banner.Publish();
        Assert.True(banner.IsPublished);

        // Act
        banner.Unpublish();

        // Assert
        Assert.False(banner.IsPublished);
    }

    [Fact]
    public void Publish_ShouldUpdateModificationTime()
    {
        // Arrange
        var banner = new Banner(_testShopId, _testUserId, "Test", "Desc", 800, 600);
        var originalUpdateTime = banner.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        banner.Publish();

        // Assert
        Assert.True(banner.UpdatedAt > originalUpdateTime);
    }

    #endregion
}
