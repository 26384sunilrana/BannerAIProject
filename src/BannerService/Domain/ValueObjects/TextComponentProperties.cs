namespace BannerService.Domain.ValueObjects;

public class TextComponentProperties
{
    public string Content { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 16;
    public string Color { get; set; } = "#000000";
    public string FontWeight { get; set; } = "Normal";
    public string TextAlign { get; set; } = "Left";

    public TextComponentProperties() { }

    public TextComponentProperties(
        string content,
        string fontFamily,
        int fontSize,
        string color,
        string fontWeight,
        string textAlign)
    {
        Validate(content, fontSize, color);

        Content = content;
        FontFamily = fontFamily;
        FontSize = fontSize;
        Color = color;
        FontWeight = fontWeight;
        TextAlign = textAlign;
    }

    private static void Validate(string content, int fontSize, string color)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 500)
            throw new ArgumentException("Content must be 1-500 characters");

        if (fontSize < 8 || fontSize > 72)
            throw new ArgumentException("FontSize must be 8-72px");

        if (!IsValidHexColor(color))
            throw new ArgumentException("Color must be valid hex format (e.g., #FF0000)");
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }
}
