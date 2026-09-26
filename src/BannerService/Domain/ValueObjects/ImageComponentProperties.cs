namespace BannerService.Domain.ValueObjects;

public class ImageComponentProperties
{
    public string ImageReference { get; set; } = string.Empty;
    public string FilterEffect { get; set; } = "None";
    public float Opacity { get; set; } = 1.0f;
    public float RotationDegrees { get; set; } = 0;

    public ImageComponentProperties() { }

    public ImageComponentProperties(
        string imageReference,
        string filterEffect,
        float opacity,
        float rotationDegrees)
    {
        Validate(imageReference, opacity, rotationDegrees);

        ImageReference = imageReference;
        FilterEffect = filterEffect;
        Opacity = opacity;
        RotationDegrees = rotationDegrees;
    }

    private static void Validate(string imageReference, float opacity, float rotationDegrees)
    {
        if (string.IsNullOrWhiteSpace(imageReference))
            throw new ArgumentException("ImageReference cannot be empty");

        if (!Uri.TryCreate(imageReference, UriKind.Absolute, out _))
            throw new ArgumentException("ImageReference must be a valid URL");

        if (opacity < 0 || opacity > 1)
            throw new ArgumentException("Opacity must be 0-1");

        if (rotationDegrees < -360 || rotationDegrees > 360)
            throw new ArgumentException("RotationDegrees must be -360 to 360");
    }
}
