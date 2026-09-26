namespace BannerService.Domain.Services;

using ValueObjects;
using Entities;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(string error) => new() { IsValid = false, Error = error };
}

public class EffectValidator
{
    public ValidationResult ValidateOpacity(decimal opacity, int duration, int delay)
    {
        if (opacity < 0 || opacity > 1)
            return ValidationResult.Failure("Opacity must be 0.0-1.0");
        if (duration < 0 || duration > 5000)
            return ValidationResult.Failure("Duration must be 0-5000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        return ValidationResult.Success();
    }

    public ValidationResult ValidateRotation(decimal rotation, int duration, int delay, string timingCurve)
    {
        if (rotation < -360 || rotation > 360)
            return ValidationResult.Failure("Rotation must be -360 to 360 degrees");
        if (duration < 0 || duration > 5000)
            return ValidationResult.Failure("Duration must be 0-5000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        var validCurves = new[] { "linear", "ease-in", "ease-out", "ease-in-out" };
        if (!validCurves.Contains(timingCurve ?? ""))
            return ValidationResult.Failure("Invalid timing curve");
        return ValidationResult.Success();
    }

    public ValidationResult ValidateScale(decimal scaleX, decimal scaleY, int duration, int delay, string origin)
    {
        if (scaleX < 0.1m || scaleX > 2.0m || scaleY < 0.1m || scaleY > 2.0m)
            return ValidationResult.Failure("Scale must be 0.1-2.0");
        if (duration < 0 || duration > 5000)
            return ValidationResult.Failure("Duration must be 0-5000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        var validOrigins = new[] { "center", "top-left", "top-right", "bottom-left", "bottom-right" };
        if (!validOrigins.Contains(origin ?? ""))
            return ValidationResult.Failure("Invalid origin");
        return ValidationResult.Success();
    }

    public ValidationResult ValidateBlur(int blurRadius, int duration, int delay)
    {
        if (blurRadius < 0 || blurRadius > 50)
            return ValidationResult.Failure("Blur radius must be 0-50px");
        if (duration < 0 || duration > 5000)
            return ValidationResult.Failure("Duration must be 0-5000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        return ValidationResult.Success();
    }

    public ValidationResult ValidateAnimation(string animationType, int duration, int delay, int repeat, int repeatDelay)
    {
        var validTypes = new[] { "fade-in", "fade-out", "slide-left", "slide-right", "pulse", "bounce" };
        if (!validTypes.Contains(animationType ?? ""))
            return ValidationResult.Failure("Invalid animation type");
        if (duration < 200 || duration > 3000)
            return ValidationResult.Failure("Duration must be 200-3000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        if (repeat < 1)
            return ValidationResult.Failure("Repeat must be >= 1");
        if (repeatDelay < 0 || repeatDelay > 5000)
            return ValidationResult.Failure("Repeat delay must be 0-5000ms");
        return ValidationResult.Success();
    }

    public ValidationResult ValidateCarouselConfig(Carousel carousel, Banner banner)
    {
        if (carousel.ComponentIds.Count < 2)
            return ValidationResult.Failure("Carousel requires at least 2 components");
        if (carousel.IntervalMs < 1000 || carousel.IntervalMs > 30000)
            return ValidationResult.Failure("Interval must be 1000-30000ms");
        if (carousel.TransitionDuration < 200 || carousel.TransitionDuration > 2000)
            return ValidationResult.Failure("Transition duration must be 200-2000ms");

        // Verify all components exist and are image/video
        foreach (var componentId in carousel.ComponentIds)
        {
            var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
            if (component == null)
                return ValidationResult.Failure($"Component {componentId} not found");
            if (component.ComponentType != ComponentType.Image && component.ComponentType != ComponentType.Video)
                return ValidationResult.Failure("Carousel components must be Image or Video");
        }

        return ValidationResult.Success();
    }
}
