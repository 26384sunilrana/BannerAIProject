namespace BannerService.Domain.Tests.ValueObjects;

using Xunit;
using BannerService.Domain.ValueObjects;

public class TextComponentPropertiesTests
{
    [Fact]
    public void CreateTextProperties_WithValidValues_ShouldSucceed()
    {
        // Act
        var props = new TextComponentProperties(
            "Hello World",
            "Arial",
            16,
            "#FF0000",
            "Bold",
            "Center");

        // Assert
        Assert.Equal("Hello World", props.Content);
        Assert.Equal(16, props.FontSize);
        Assert.Equal("#FF0000", props.Color);
    }

    [Fact]
    public void CreateTextProperties_WithInvalidColor_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new TextComponentProperties("Hello", "Arial", 16, "INVALID", "Bold", "Center"));
    }

    [Fact]
    public void CreateTextProperties_WithFontSizeOutOfRange_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new TextComponentProperties("Hello", "Arial", 7, "#FF0000", "Bold", "Center"));
        Assert.Throws<ArgumentException>(() =>
            new TextComponentProperties("Hello", "Arial", 73, "#FF0000", "Bold", "Center"));
    }

    [Fact]
    public void CreateTextProperties_WithEmptyContent_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new TextComponentProperties("", "Arial", 16, "#FF0000", "Bold", "Center"));
    }

    [Fact]
    public void CreateTextProperties_WithExcessiveContent_ShouldThrow()
    {
        // Act & Assert
        var longContent = new string('a', 501);
        Assert.Throws<ArgumentException>(() =>
            new TextComponentProperties(longContent, "Arial", 16, "#FF0000", "Bold", "Center"));
    }

    [Theory]
    [InlineData("#FFF")]
    [InlineData("#FFFFFF")]
    [InlineData("#123456")]
    public void CreateTextProperties_WithValidHexColors_ShouldSucceed(string color)
    {
        // Act
        var props = new TextComponentProperties("Hello", "Arial", 16, color, "Bold", "Center");

        // Assert
        Assert.Equal(color, props.Color);
    }
}
