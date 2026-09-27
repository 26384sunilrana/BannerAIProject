namespace BannerService.Application.Dto;

using global::BannerService.Domain.Entities;

public class BannerResponseDto
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPublished { get; set; }
    public int ComponentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static BannerResponseDto FromBanner(Banner banner)
    {
        return new BannerResponseDto
        {
            Id = banner.Id,
            ShopId = banner.ShopId,
            Name = banner.Name,
            Description = banner.Description,
            Width = banner.Width,
            Height = banner.Height,
            IsPublished = banner.IsPublished,
            ComponentCount = banner.Components.Count,
            CreatedAt = banner.CreatedAt,
            UpdatedAt = banner.UpdatedAt
        };
    }
}
