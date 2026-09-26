namespace BannerService.Application.Dto;

public class ComponentResponseDto
{
    public Guid Id { get; set; }
    public Guid BannerId { get; set; }
    public int ComponentType { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SizeWidth { get; set; }
    public int SizeHeight { get; set; }
    public int ZIndex { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
