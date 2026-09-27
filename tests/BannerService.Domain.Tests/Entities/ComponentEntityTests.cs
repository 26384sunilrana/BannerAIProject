namespace BannerService.Domain.Tests.Entities;

using System;
using System.Linq;
using Xunit;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;

public class ComponentEntityTests
{
    #region Constructor Tests

    [Fact]
    public void CreateComponent_WithValidData_ShouldInitializeProperties()
    {
        // Act
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(10, 20),
            new Size(100, 50),
            1,
            "{\"text\": \"Hello\"}"
        );

        // Assert
        Assert.NotEqual(Guid.Empty, component.Id);
        Assert.Equal(ComponentType.Text, component.Type);
        Assert.Equal(10, component.PositionX);
        Assert.Equal(20, component.PositionY);
        Assert.Equal(100, component.SizeWidth);
        Assert.Equal(50, component.SizeHeight);
        Assert.Equal(1, component.ZIndex);
    }

    [Fact]
    public void CreateComponent_ShouldHaveEmptyEffectsList()
    {
        // Act
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Image,
            new Position(0, 0),
            new Size(200, 200),
            1,
            "{}"
        );

        // Assert
        Assert.NotNull(component.Effects);
        Assert.Empty(component.Effects);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldChangeAllProperties()
    {
        // Arrange
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 100),
            1,
            "{}"
        );
        var originalTime = component.UpdatedAt;

        // Act
        System.Threading.Thread.Sleep(10);
        component.Update(
            new Position(50, 50),
            new Size(200, 200),
            2,
            "{\"updated\": true}"
        );

        // Assert
        Assert.Equal(50, component.PositionX);
        Assert.Equal(50, component.PositionY);
        Assert.Equal(200, component.SizeWidth);
        Assert.Equal(200, component.SizeHeight);
        Assert.Equal(2, component.ZIndex);
        Assert.Equal("{\"updated\": true}", component.PropertiesJson);
        Assert.True(component.UpdatedAt > originalTime);
    }

    #endregion

    #region AddEffect Tests

    [Fact]
    public void AddEffect_WithValidEffect_ShouldAddToCollection()
    {
        // Arrange
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 100),
            1,
            "{}"
        );
        var effect = new Effect
        {
            Id = Guid.NewGuid(),
            Name = "Fade",
            Duration = 1000,
            Delay = 0
        };

        // Act
        component.AddEffect(effect);

        // Assert
        Assert.Single(component.Effects);
        Assert.Contains(effect, component.Effects);
        Assert.Equal(component.Id, effect.ComponentId);
    }

    [Fact]
    public void AddEffect_WithMultipleEffects_ShouldAddAll()
    {
        // Arrange
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 100),
            1,
            "{}"
        );
        var effects = new[]
        {
            new Effect { Id = Guid.NewGuid(), Name = "Fade", Duration = 1000 },
            new Effect { Id = Guid.NewGuid(), Name = "Slide", Duration = 500 },
            new Effect { Id = Guid.NewGuid(), Name = "Bounce", Duration = 800 }
        };

        // Act
        foreach (var effect in effects)
        {
            component.AddEffect(effect);
        }

        // Assert
        Assert.Equal(3, component.Effects.Count);
    }

    [Fact]
    public void AddEffect_WithNullEffect_ShouldThrowException()
    {
        // Arrange
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 100),
            1,
            "{}"
        );

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => component.AddEffect(null));
    }

    #endregion

    #region RemoveEffect Tests

    [Fact]
    public void RemoveEffect_WithExistingEffectId_ShouldRemoveEffect()
    {
        // Arrange
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 100),
            1,
            "{}"
        );
        var effectId = Guid.NewGuid();
        var effect = new Effect
        {
            Id = effectId,
            Name = "Fade",
            Duration = 1000
        };
        component.AddEffect(effect);

        // Act
        component.RemoveEffect(effectId);

        // Assert
        Assert.Empty(component.Effects);
    }

    [Fact]
    public void RemoveEffect_WithNonexistentEffectId_ShouldNotThrow()
    {
        // Arrange
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 100),
            1,
            "{}"
        );

        // Act & Assert - should not throw
        component.RemoveEffect(Guid.NewGuid());
        Assert.Empty(component.Effects);
    }

    #endregion

    #region ComponentType Tests

    [Fact]
    public void ComponentType_Text_ShouldEqual1()
    {
        // Assert
        Assert.Equal(1, (int)ComponentType.Text);
    }

    [Fact]
    public void ComponentType_Image_ShouldEqual2()
    {
        // Assert
        Assert.Equal(2, (int)ComponentType.Image);
    }

    [Fact]
    public void ComponentType_Video_ShouldEqual3()
    {
        // Assert
        Assert.Equal(3, (int)ComponentType.Video);
    }

    [Fact]
    public void ComponentType_Graphics_ShouldEqual4()
    {
        // Assert
        Assert.Equal(4, (int)ComponentType.Graphics);
    }

    #endregion

    #region Position and Size Tests

    [Fact]
    public void Component_WithPosition_ShouldStoreXAndY()
    {
        // Arrange
        var position = new Position(25, 75);

        // Act
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            position,
            new Size(100, 100),
            1,
            "{}"
        );

        // Assert
        Assert.Equal(25, component.PositionX);
        Assert.Equal(75, component.PositionY);
    }

    [Fact]
    public void Component_WithSize_ShouldStoreWidthAndHeight()
    {
        // Arrange
        var size = new Size(300, 250);

        // Act
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Image,
            new Position(0, 0),
            size,
            1,
            "{}"
        );

        // Assert
        Assert.Equal(300, component.SizeWidth);
        Assert.Equal(250, component.SizeHeight);
    }

    #endregion

    #region PropertiesJson Tests

    [Fact]
    public void PropertiesJson_ShouldStoreComplexJson()
    {
        // Arrange
        var jsonProps = "{\"text\": \"Hello World\", \"fontSize\": 24, \"color\": \"#FF0000\"}";

        // Act
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 50),
            1,
            jsonProps
        );

        // Assert
        Assert.Equal(jsonProps, component.PropertiesJson);
    }

    [Fact]
    public void PropertiesJson_ShouldBeUpdateable()
    {
        // Arrange
        var component = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 50),
            1,
            "{}"
        );
        var newJson = "{\"updated\": true}";

        // Act
        component.Update(new Position(0, 0), new Size(100, 50), 1, newJson);

        // Assert
        Assert.Equal(newJson, component.PropertiesJson);
    }

    #endregion

    #region ZIndex Tests

    [Fact]
    public void ZIndex_ShouldDetermineLayerOrder()
    {
        // Arrange
        var component1 = new Component(
            Guid.NewGuid(),
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 100),
            1,
            "{}"
        );
        var component2 = new Component(
            Guid.NewGuid(),
            ComponentType.Image,
            new Position(0, 0),
            new Size(100, 100),
            2,
            "{}"
        );

        // Act & Assert
        Assert.True(component1.ZIndex < component2.ZIndex);
    }

    #endregion
}
