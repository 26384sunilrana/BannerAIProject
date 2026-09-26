namespace BannerService.Domain.Tests.ValueObjects;

using Xunit;
using BannerService.Domain.ValueObjects;

public class PositionTests
{
    [Fact]
    public void CreatePosition_WithValidCoordinates_ShouldSucceed()
    {
        // Act
        var position = new Position(100, 200);

        // Assert
        Assert.Equal(100, position.X);
        Assert.Equal(200, position.Y);
    }

    [Fact]
    public void CreatePosition_WithNegativeCoordinates_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Position(-1, 100));
        Assert.Throws<ArgumentException>(() => new Position(100, -1));
    }

    [Fact]
    public void PositionEquality_SameValues_ShouldBeEqual()
    {
        // Act
        var pos1 = new Position(100, 200);
        var pos2 = new Position(100, 200);

        // Assert
        Assert.Equal(pos1, pos2);
    }

    [Fact]
    public void PositionEquality_DifferentValues_ShouldNotBeEqual()
    {
        // Act
        var pos1 = new Position(100, 200);
        var pos2 = new Position(100, 201);

        // Assert
        Assert.NotEqual(pos1, pos2);
    }
}
