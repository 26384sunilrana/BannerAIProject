namespace BannerService.Domain.Tests.ValueObjects;

using Xunit;
using BannerService.Domain.ValueObjects;

public class VideoComponentPropertiesTests
{
    [Fact]
    public void CreateVideoProperties_WithValidValues_ShouldSucceed()
    {
        // Act
        var props = new VideoComponentProperties(
            "https://example.com/video.mp4",
            true,
            false,
            false,
            0.8f,
            0,
            120);

        // Assert
        Assert.Equal("https://example.com/video.mp4", props.VideoReference);
        Assert.True(props.Autoplay);
        Assert.False(props.Loop);
        Assert.Equal(0.8f, props.Volume);
    }

    [Fact]
    public void CreateVideoProperties_WithInvalidUrl_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new VideoComponentProperties("invalid-url", true, false, false, 0.8f, 0, 120));
    }

    [Fact]
    public void CreateVideoProperties_WithVolumeOutOfRange_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new VideoComponentProperties("https://example.com/video.mp4", true, false, false, 1.5f, 0, 120));

        Assert.Throws<ArgumentException>(() =>
            new VideoComponentProperties("https://example.com/video.mp4", true, false, false, -0.1f, 0, 120));
    }

    [Fact]
    public void CreateVideoProperties_WithInvalidDuration_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new VideoComponentProperties("https://example.com/video.mp4", true, false, false, 0.8f, 0, 0));

        Assert.Throws<ArgumentException>(() =>
            new VideoComponentProperties("https://example.com/video.mp4", true, false, false, 0.8f, 0, -10));
    }

    [Theory]
    [InlineData("https://example.com/video.mp4")]
    [InlineData("https://cdn.example.com/media/video.webm")]
    [InlineData("https://storage.example.com/videos/demo.mkv")]
    public void CreateVideoProperties_WithValidUrls_ShouldSucceed(string url)
    {
        // Act
        var props = new VideoComponentProperties(url, false, true, true, 0.5f, 10, 300);

        // Assert
        Assert.Equal(url, props.VideoReference);
    }
}

public class GraphicsComponentPropertiesTests
{
    [Fact]
    public void CreateGraphicsProperties_WithValidValues_ShouldSucceed()
    {
        // Act
        var props = new GraphicsComponentProperties(
            "Circle",
            "#FF0000",
            "#000000",
            2,
            "Solid",
            0,
            45);

        // Assert
        Assert.Equal("Circle", props.ShapeType);
        Assert.Equal("#FF0000", props.FillColor);
        Assert.Equal(2, props.BorderWidth);
        Assert.Equal(45, props.Rotation);
    }

    [Fact]
    public void CreateGraphicsProperties_WithInvalidFillColor_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new GraphicsComponentProperties("Circle", "INVALID", "#000000", 2, "Solid", 0, 0));
    }

    [Fact]
    public void CreateGraphicsProperties_WithInvalidBorderColor_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new GraphicsComponentProperties("Circle", "#FF0000", "not-a-color", 2, "Solid", 0, 0));
    }

    [Fact]
    public void CreateGraphicsProperties_WithBorderWidthOutOfRange_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new GraphicsComponentProperties("Circle", "#FF0000", "#000000", 11, "Solid", 0, 0));

        Assert.Throws<ArgumentException>(() =>
            new GraphicsComponentProperties("Circle", "#FF0000", "#000000", -1, "Solid", 0, 0));
    }

    [Fact]
    public void CreateGraphicsProperties_WithCornerRadiusOutOfRange_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new GraphicsComponentProperties("Rectangle", "#FF0000", "#000000", 2, "Solid", 101, 0));

        Assert.Throws<ArgumentException>(() =>
            new GraphicsComponentProperties("Rectangle", "#FF0000", "#000000", 2, "Solid", -5, 0));
    }

    [Fact]
    public void CreateGraphicsProperties_WithRotationOutOfRange_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new GraphicsComponentProperties("Circle", "#FF0000", "#000000", 2, "Solid", 0, 400));

        Assert.Throws<ArgumentException>(() =>
            new GraphicsComponentProperties("Circle", "#FF0000", "#000000", 2, "Solid", 0, -400));
    }

    [Theory]
    [InlineData("#FFF")]
    [InlineData("#FFFFFF")]
    [InlineData("#123456")]
    [InlineData("#00FF00")]
    public void CreateGraphicsProperties_WithValidHexColors_ShouldSucceed(string color)
    {
        // Act
        var props = new GraphicsComponentProperties("Rectangle", color, color, 1, "Solid", 10, 90);

        // Assert
        Assert.Equal(color, props.FillColor);
        Assert.Equal(color, props.BorderColor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(360)]
    [InlineData(-45)]
    [InlineData(-180)]
    [InlineData(-360)]
    public void CreateGraphicsProperties_WithValidRotations_ShouldSucceed(float rotation)
    {
        // Act
        var props = new GraphicsComponentProperties("Circle", "#FF0000", "#000000", 1, "Solid", 0, rotation);

        // Assert
        Assert.Equal(rotation, props.Rotation);
    }
}
