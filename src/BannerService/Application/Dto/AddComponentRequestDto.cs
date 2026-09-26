namespace BannerService.Application.Dto;

public class AddComponentRequestDto
{
    public int ComponentType { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SizeWidth { get; set; }
    public int SizeHeight { get; set; }
    public int ZIndex { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class TextComponentRequest
{
    public string Content { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 16;
    public string Color { get; set; } = "#000000";
    public string FontWeight { get; set; } = "Normal";
    public string TextAlign { get; set; } = "Left";
}

public class ImageComponentRequest
{
    public string ImageReference { get; set; } = string.Empty;
    public string FilterEffect { get; set; } = "None";
    public float Opacity { get; set; } = 1.0f;
    public float RotationDegrees { get; set; } = 0;
}
