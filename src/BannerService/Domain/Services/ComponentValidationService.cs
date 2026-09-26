namespace BannerService.Domain.Services;

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
}
