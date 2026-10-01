namespace BannerService.Domain.Services;

using ValueObjects;

public class ComponentValidationService
{
    public void ValidatePosition(int x, int y, int bannerWidth, int bannerHeight)
    {
        if (x < 0 || y < 0)
            throw new ArgumentException("Position coordinates must be non-negative");

        // Position can extend beyond banner (for overflow effects)
    }

    public void ValidateSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive");

        if (width > 5000 || height > 5000)
            throw new ArgumentException("Width and height cannot exceed 5000px");
    }

    public void ValidateZIndex(int zIndex)
    {
        if (zIndex < 0 || zIndex > 100)
            throw new ArgumentException("ZIndex must be 0-100");
    }

    public void ValidateComponentCount(int currentCount)
    {
        if (currentCount >= 50)
            throw new InvalidOperationException("Cannot add more than 50 components to a banner");
    }

    public void ValidateTextComponent(TextComponentProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.Content) || props.Content.Length > 500)
            throw new ArgumentException("Content must be 1-500 characters");

        if (props.FontSize < 8 || props.FontSize > 72)
            throw new ArgumentException("FontSize must be 8-72px");

        if (!IsValidHexColor(props.Color))
            throw new ArgumentException("Color must be valid hex format");
    }

    public void ValidateImageComponent(ImageComponentProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.ImageReference))
            throw new ArgumentException("ImageReference cannot be empty");

        if (!Uri.TryCreate(props.ImageReference, UriKind.Absolute, out _))
            throw new ArgumentException("ImageReference must be a valid URL");

        if (props.Opacity < 0 || props.Opacity > 1)
            throw new ArgumentException("Opacity must be 0-1");

        if (props.RotationDegrees < -360 || props.RotationDegrees > 360)
            throw new ArgumentException("RotationDegrees must be -360 to 360");
    }

    /// <summary>Validates the optional video playlist of a video component's properties.</summary>
    public void ValidateVideoPlaylist(IDictionary<string, object>? properties)
    {
        VideoPlaylist.FromProperties(properties)?.Validate();
    }

    public void ValidateVideoComponent(VideoComponentProperties props)
    {
        if (string.IsNullOrWhiteSpace(props.VideoReference))
            throw new ArgumentException("VideoReference cannot be empty");

        if (!Uri.TryCreate(props.VideoReference, UriKind.Absolute, out _))
            throw new ArgumentException("VideoReference must be a valid URL");

        if (props.Volume < 0 || props.Volume > 1)
            throw new ArgumentException("Volume must be 0-1");

        if (props.Duration <= 0)
            throw new ArgumentException("Duration must be positive");
    }

    public void ValidateGraphicsComponent(GraphicsComponentProperties props)
    {
        if (!IsValidHexColor(props.FillColor))
            throw new ArgumentException("FillColor must be valid hex format");

        if (!IsValidHexColor(props.BorderColor))
            throw new ArgumentException("BorderColor must be valid hex format");

        if (props.BorderWidth < 0 || props.BorderWidth > 10)
            throw new ArgumentException("BorderWidth must be 0-10 pixels");

        if (props.CornerRadius < 0 || props.CornerRadius > 100)
            throw new ArgumentException("CornerRadius must be 0-100 pixels");

        if (props.Rotation < -360 || props.Rotation > 360)
            throw new ArgumentException("Rotation must be -360 to 360 degrees");
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }
}
