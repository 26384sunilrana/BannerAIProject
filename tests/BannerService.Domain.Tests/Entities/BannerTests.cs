namespace BannerService.Domain.Tests.Entities;

using Xunit;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;

public class BannerTests
{
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void CreateBanner_WithValidMetadata_ShouldSucceed()
    {
        // Arrange & Act
        var banner = new Banner(_shopId, _userId, "Summer Sale", "Promotional banner", 1200, 600);

        // Assert
        Assert.NotEqual(Guid.Empty, banner.Id);
        Assert.Equal(_shopId, banner.ShopId);
        Assert.Equal(_userId, banner.UserId);
        Assert.Equal("Summer Sale", banner.Name);
        Assert.False(banner.IsPublished);
        Assert.Empty(banner.Components);
    }

    [Fact]
    public void AddComponent_WithValidComponent_ShouldSucceed()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Test", 1200, 600);
        var component = new Component(
            banner.Id,
            ComponentType.Text,
            new Position(10, 20),
            new Size(100, 50),
            1,
            "{\"content\": \"Hello\"}");

        // Act
        banner.AddComponent(component);

        // Assert
        Assert.Single(banner.Components);
        Assert.Contains(component, banner.Components);
    }

    [Fact]
    public void AddComponent_WithDuplicateZIndex_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Test", 1200, 600);
        var comp1 = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        var comp2 = new Component(banner.Id, ComponentType.Image, new Position(0, 0), new Size(100, 50), 1, "{}");

        banner.AddComponent(comp1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => banner.AddComponent(comp2));
    }

    [Fact]
    public void AddComponent_ExceedingLimit_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Test", 1200, 600);
        for (int i = 0; i < 50; i++)
        {
            var comp = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), i, "{}");
            banner.AddComponent(comp);
        }

        var excess = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 50, "{}");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => banner.AddComponent(excess));
    }

    [Fact]
    public void RemoveComponent_WithValidComponent_ShouldSucceed()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Test", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        banner.AddComponent(component);

        // Act
        banner.RemoveComponent(component.Id);

        // Assert
        Assert.Empty(banner.Components);
    }

    [Fact]
    public void RemoveComponent_NonExistent_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Test", 1200, 600);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => banner.RemoveComponent(Guid.NewGuid()));
    }

    [Fact]
    public void UpdateComponent_WithValidData_ShouldSucceed()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Test", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        banner.AddComponent(component);

        // Act
        banner.UpdateComponent(component.Id, new Position(10, 20), new Size(200, 100), 2, "{\"updated\": true}");

        // Assert
        var updated = banner.Components.First(c => c.Id == component.Id);
        Assert.Equal(10, updated.PositionX);
        Assert.Equal(20, updated.PositionY);
        Assert.Equal(200, updated.SizeWidth);
        Assert.Equal(100, updated.SizeHeight);
        Assert.Equal(2, updated.ZIndex);
    }

    [Fact]
    public void Publish_ShouldSetIsPublishedTrue()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Test", 1200, 600);

        // Act
        banner.Publish();

        // Assert
        Assert.True(banner.IsPublished);
    }

    [Fact]
    public void Unpublish_ShouldSetIsPublishedFalse()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Test", 1200, 600);
        banner.Publish();

        // Act
        banner.Unpublish();

        // Assert
        Assert.False(banner.IsPublished);
    }
}
