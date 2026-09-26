namespace BannerService.Domain.ValueObjects;

using System.Text.RegularExpressions;

public class GraphicsComponentProperties
{
    public string ShapeType { get; set; } = "Rectangle";
    public string FillColor { get; set; } = "#FFFFFF";
    public string BorderColor { get; set; } = "#000000";
    public int BorderWidth { get; set; } = 1;
    public string BorderStyle { get; set; } = "Solid";
    public int CornerRadius { get; set; } = 0;
    public float Rotation { get; set; } = 0;

    public GraphicsComponentProperties() { }

    public GraphicsComponentProperties(
        string shapeType,
        string fillColor,
        string borderColor,
        int borderWidth,
        string borderStyle,
        int cornerRadius,
        float rotation)
    {
        Validate(fillColor, borderColor, borderWidth, cornerRadius, rotation);

        ShapeType = shapeType;
        FillColor = fillColor;
        BorderColor = borderColor;
        BorderWidth = borderWidth;
        BorderStyle = borderStyle;
        CornerRadius = cornerRadius;
        Rotation = rotation;
    }

    private static void Validate(string fillColor, string borderColor, int borderWidth, int cornerRadius, float rotation)
    {
        if (!IsValidHexColor(fillColor))
            throw new ArgumentException("FillColor must be valid hex format (e.g., #FF0000)");

        if (!IsValidHexColor(borderColor))
            throw new ArgumentException("BorderColor must be valid hex format (e.g., #000000)");

        if (borderWidth < 0 || borderWidth > 10)
            throw new ArgumentException("BorderWidth must be 0-10 pixels");

        if (cornerRadius < 0 || cornerRadius > 100)
            throw new ArgumentException("CornerRadius must be 0-100 pixels");

        if (rotation < -360 || rotation > 360)
            throw new ArgumentException("Rotation must be -360 to 360 degrees");
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        return Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }
}
