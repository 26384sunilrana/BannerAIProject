namespace BannerService.Domain.Tests.Services;

using Xunit;
using BannerService.Domain.Services;

public class EffectValidatorTests
{
    private readonly EffectValidator _validator = new();

    [Fact]
    public void ValidateOpacity_WithValidOpacity_ShouldPass()
    {
        // Act
        var result = _validator.ValidateOpacity(0.5m, 1000, 0);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateOpacity_WithOpacityAboveOne_ShouldFail()
    {
        // Act
        var result = _validator.ValidateOpacity(1.1m, 1000, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("0.0-1.0", result.Error);
    }

    [Fact]
    public void ValidateOpacity_WithOpacityBelowZero_ShouldFail()
    {
        // Act
        var result = _validator.ValidateOpacity(-0.1m, 1000, 0);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateOpacity_WithDurationAbove5000_ShouldFail()
    {
        // Act
        var result = _validator.ValidateOpacity(0.5m, 5001, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("0-5000ms", result.Error);
    }

    [Fact]
    public void ValidateRotation_WithValidRotation_ShouldPass()
    {
        // Act
        var result = _validator.ValidateRotation(45m, 2000, 500, "ease-in");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateRotation_With361Degrees_ShouldFail()
    {
        // Act
        var result = _validator.ValidateRotation(361m, 2000, 500, "ease-in");

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateRotation_WithInvalidTimingCurve_ShouldFail()
    {
        // Act
        var result = _validator.ValidateRotation(45m, 2000, 500, "invalid-curve");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("timing curve", result.Error);
    }

    [Fact]
    public void ValidateScale_WithValidScale_ShouldPass()
    {
        // Act
        var result = _validator.ValidateScale(1.5m, 1.5m, 1500, 0, "center");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateScale_WithScaleBelowMinimum_ShouldFail()
    {
        // Act
        var result = _validator.ValidateScale(0.05m, 1.5m, 1500, 0, "center");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("0.1-2.0", result.Error);
    }

    [Fact]
    public void ValidateScale_WithInvalidOrigin_ShouldFail()
    {
        // Act
        var result = _validator.ValidateScale(1.5m, 1.5m, 1500, 0, "invalid");

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateBlur_WithValidBlur_ShouldPass()
    {
        // Act
        var result = _validator.ValidateBlur(10, 800, 0);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateBlur_WithBlurAbove50_ShouldFail()
    {
        // Act
        var result = _validator.ValidateBlur(51, 800, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("0-50px", result.Error);
    }

    [Fact]
    public void ValidateAnimation_WithValidAnimation_ShouldPass()
    {
        // Act
        var result = _validator.ValidateAnimation("fade-in", 1000, 0, 1, 0);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateAnimation_WithDurationBelowMinimum_ShouldFail()
    {
        // Act
        var result = _validator.ValidateAnimation("fade-in", 100, 0, 1, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("200-3000ms", result.Error);
    }

    [Fact]
    public void ValidateAnimation_WithInvalidAnimationType_ShouldFail()
    {
        // Act
        var result = _validator.ValidateAnimation("invalid", 1000, 0, 1, 0);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateAnimation_WithRepeatZero_ShouldFail()
    {
        // Act
        var result = _validator.ValidateAnimation("fade-in", 1000, 0, 0, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Repeat must be >= 1", result.Error);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(0.5)]
    public void ValidateOpacity_WithBoundaryValues_ShouldPass(decimal opacity)
    {
        // Act
        var result = _validator.ValidateOpacity(opacity, 1000, 0);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-360)]
    [InlineData(0)]
    [InlineData(360)]
    public void ValidateRotation_WithBoundaryDegrees_ShouldPass(decimal degrees)
    {
        // Act
        var result = _validator.ValidateRotation(degrees, 1000, 0, "linear");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateScale_WithMinimumScaleBoundary_ShouldPass()
    {
        // Act
        var result = _validator.ValidateScale(0.1m, 0.1m, 1000, 0, "center");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateScale_WithMaximumScaleBoundary_ShouldPass()
    {
        // Act
        var result = _validator.ValidateScale(2.0m, 2.0m, 1000, 0, "center");

        // Assert
        Assert.True(result.IsValid);
    }
}
