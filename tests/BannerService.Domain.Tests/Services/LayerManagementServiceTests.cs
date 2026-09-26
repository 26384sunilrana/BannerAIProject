namespace BannerService.Domain.Tests.Services;

using Xunit;
using BannerService.Domain.Entities;
using BannerService.Domain.Services;
using BannerService.Domain.ValueObjects;

public class LayerManagementServiceTests
{
    private readonly LayerManagementService _service = new();
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void ReorderComponent_WithValidZIndex_ShouldSucceed()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var comp1 = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        var comp2 = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 5, "{}");
        banner.AddComponent(comp1);
        banner.AddComponent(comp2);

        // Act
        var layerOrder = _service.ReorderComponent(banner, comp1.Id, 10);

        // Assert
        Assert.Equal(1, layerOrder.OldZIndex);
        Assert.Equal(10, layerOrder.NewZIndex);
        Assert.Single(banner.Components.Where(c => c.ZIndex == 10));
    }

    [Fact]
    public void ReorderComponent_WithOccupiedZIndex_ShouldSwap()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var comp1 = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        var comp2 = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 5, "{}");
        banner.AddComponent(comp1);
        banner.AddComponent(comp2);

        // Act - Move comp1 to comp2's z-index
        _service.ReorderComponent(banner, comp1.Id, 5);

        // Assert - Components should have swapped z-indexes
        Assert.True(banner.Components.Any(c => c.Id == comp1.Id && c.ZIndex == 5));
        Assert.True(banner.Components.Any(c => c.Id == comp2.Id && c.ZIndex == 1));
    }

    [Fact]
    public void MoveForward_ShouldIncrementZIndex()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 10, "{}");
        banner.AddComponent(component);

        // Act
        var layerOrder = _service.MoveForward(banner, component.Id);

        // Assert
        Assert.Equal(10, layerOrder.OldZIndex);
        Assert.True(layerOrder.NewZIndex > 10);
    }

    [Fact]
    public void MoveForward_WhenAtFront_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 100, "{}");
        banner.AddComponent(component);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _service.MoveForward(banner, component.Id));
    }

    [Fact]
    public void MoveBackward_ShouldDecrementZIndex()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 50, "{}");
        banner.AddComponent(component);

        // Act
        var layerOrder = _service.MoveBackward(banner, component.Id);

        // Assert
        Assert.Equal(50, layerOrder.OldZIndex);
        Assert.True(layerOrder.NewZIndex < 50);
    }

    [Fact]
    public void MoveBackward_WhenAtBack_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 0, "{}");
        banner.AddComponent(component);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _service.MoveBackward(banner, component.Id));
    }

    [Fact]
    public void SendToFront_ShouldSetZIndexTo100()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 50, "{}");
        banner.AddComponent(component);

        // Act
        var layerOrder = _service.SendToFront(banner, component.Id);

        // Assert
        Assert.Equal(50, layerOrder.OldZIndex);
        Assert.Equal(100, layerOrder.NewZIndex);
    }

    [Fact]
    public void SendToBack_ShouldSetZIndexTo0()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 50, "{}");
        banner.AddComponent(component);

        // Act
        var layerOrder = _service.SendToBack(banner, component.Id);

        // Assert
        Assert.Equal(50, layerOrder.OldZIndex);
        Assert.Equal(0, layerOrder.NewZIndex);
    }
}
