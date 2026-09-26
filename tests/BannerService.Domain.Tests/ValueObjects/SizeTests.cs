namespace BannerService.Domain.Tests.ValueObjects;

using Xunit;
using BannerService.Domain.ValueObjects;

public class SizeTests
{
    [Fact]
    public void CreateSize_WithValidDimensions_ShouldSucceed()
    {
        // Act
        var size = new Size(800, 600);

        // Assert
        Assert.Equal(800, size.Width);
        Assert.Equal(600, size.Height);
    }

    [Fact]
    public void CreateSize_WithZeroDimensions_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Size(0, 600));
        Assert.Throws<ArgumentException>(() => new Size(800, 0));
    }

    [Fact]
    public void CreateSize_WithNegativeDimensions_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Size(-100, 600));
    }

    [Fact]
    public void CreateSize_ExceedingMaximum_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Size(5001, 600));
        Assert.Throws<ArgumentException>(() => new Size(800, 5001));
    }

    [Fact]
    public void SizeEquality_SameValues_ShouldBeEqual()
    {
        // Act
        var size1 = new Size(800, 600);
        var size2 = new Size(800, 600);

        // Assert
        Assert.Equal(size1, size2);
    }
}
