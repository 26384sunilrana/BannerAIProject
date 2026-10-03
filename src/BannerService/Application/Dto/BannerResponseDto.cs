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
    /// <summary>True after a restore: the banner must be approved again before it goes live.</summary>
    public bool RequiresApproval { get; set; }
    public int ComponentCount { get; set; }
    /// <summary>When the banner is shown (UTC); null until a schedule is set.</summary>
    public DateTime? PublishStartAt { get; set; }
    public DateTime? PublishEndAt { get; set; }
    public int? DailyStartMinutes { get; set; }
    public int? DailyEndMinutes { get; set; }
    public int ActiveDays { get; set; }
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
            PublishStartAt = banner.PublishStartAt,
            PublishEndAt = banner.PublishEndAt,
            DailyStartMinutes = banner.DailyStartMinutes,
            DailyEndMinutes = banner.DailyEndMinutes,
            ActiveDays = banner.ActiveDays,
            CreatedAt = banner.CreatedAt,
            UpdatedAt = banner.UpdatedAt
        };
    }
}
