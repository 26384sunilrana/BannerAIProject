namespace BannerService.Application.Dto;

public class PreviewConfigurationDto
{
    public Guid BannerId { get; set; }
    public Guid ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public List<PreviewComponentDto> Components { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class PreviewComponentDto
{
    public Guid ComponentId { get; set; }
    public int ComponentType { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SizeWidth { get; set; }
    public int SizeHeight { get; set; }
    public int ZIndex { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class LayerOrderDto
{
    public Guid ComponentId { get; set; }
    public int OldZIndex { get; set; }
    public int NewZIndex { get; set; }
}

public class ReorderComponentRequestDto
{
    public int NewZIndex { get; set; }
}
