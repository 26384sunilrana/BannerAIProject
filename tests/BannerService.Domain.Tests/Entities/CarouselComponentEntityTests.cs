namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class CarouselComponentEntityTests
{
    private readonly Guid _testCarouselId = Guid.NewGuid();
    private readonly Guid _testComponentId = Guid.NewGuid();

    #region Construction Tests

    [Fact]
    public void CreateCarouselComponent_ShouldInitializeProperties()
    {
        // Act
        var component = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = _testComponentId,
            Order = 0
        };

        // Assert
        Assert.NotEqual(Guid.Empty, component.Id);
        Assert.Equal(_testCarouselId, component.CarouselId);
        Assert.Equal(_testComponentId, component.ComponentId);
        Assert.Equal(0, component.Order);
    }

    [Fact]
    public void CreateCarouselComponent_WithoutId_ShouldGenerateId()
    {
        // Act
        var component = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = _testComponentId
        };

        // Assert
        Assert.NotEqual(Guid.Empty, component.Id);
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public void SetOrder_ShouldUpdateOrder()
    {
        // Arrange
        var component = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = _testComponentId,
            Order = 0
        };

        // Act
        component.Order = 5;

        // Assert
        Assert.Equal(5, component.Order);
    }

    [Fact]
    public void Order_CanBeZero()
    {
        // Arrange & Act
        var component = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = _testComponentId,
            Order = 0
        };

        // Assert
        Assert.Equal(0, component.Order);
    }

    [Fact]
    public void Order_CanBeNegative()
    {
        // Arrange & Act
        var component = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = _testComponentId,
            Order = -1
        };

        // Assert
        Assert.Equal(-1, component.Order);
    }

    [Fact]
    public void Order_CanBeLarge()
    {
        // Arrange & Act
        var component = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = _testComponentId,
            Order = 10000
        };

        // Assert
        Assert.Equal(10000, component.Order);
    }

    #endregion

    #region Relationships Tests

    [Fact]
    public void Component_ShouldMaintainCarouselId()
    {
        // Arrange
        var carouselId = Guid.NewGuid();
        var component = new CarouselComponent
        {
            CarouselId = carouselId,
            ComponentId = Guid.NewGuid(),
            Order = 0
        };

        // Act & Assert
        Assert.Equal(carouselId, component.CarouselId);
    }

    [Fact]
    public void Component_ShouldMaintainComponentId()
    {
        // Arrange
        var componentId = Guid.NewGuid();
        var component = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = componentId,
            Order = 0
        };

        // Act & Assert
        Assert.Equal(componentId, component.ComponentId);
    }

    #endregion

    #region Collection Behavior Tests

    [Fact]
    public void MultipleComponents_CanHaveSameCarouselId()
    {
        // Arrange & Act
        var comp1 = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = Guid.NewGuid(),
            Order = 0
        };
        var comp2 = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = Guid.NewGuid(),
            Order = 1
        };

        // Assert
        Assert.Equal(comp1.CarouselId, comp2.CarouselId);
        Assert.NotEqual(comp1.ComponentId, comp2.ComponentId);
    }

    [Fact]
    public void EachComponent_ShouldHaveUniqueId()
    {
        // Arrange & Act
        var comp1 = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = Guid.NewGuid(),
            Order = 0
        };
        var comp2 = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = Guid.NewGuid(),
            Order = 1
        };

        // Assert
        Assert.NotEqual(comp1.Id, comp2.Id);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Component_WithSameProperties_ShouldHaveDifferentId()
    {
        // Arrange
        var comp1 = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = _testComponentId,
            Order = 0
        };
        var comp2 = new CarouselComponent
        {
            CarouselId = _testCarouselId,
            ComponentId = _testComponentId,
            Order = 0
        };

        // Act & Assert
        Assert.NotEqual(comp1.Id, comp2.Id);
    }

    #endregion
}
