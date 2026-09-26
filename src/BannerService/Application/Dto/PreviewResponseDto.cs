namespace BannerService.Application.Dto;

public class PreviewResponseDto
{
    public Guid BannerId { get; set; }
    public Guid ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPublished { get; set; }
    public List<ComponentResponseDto> Components { get; set; } = new();
}
