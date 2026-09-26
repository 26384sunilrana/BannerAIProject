namespace BannerService.Domain.ValueObjects;

using Entities;

public class BannerSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public List<ComponentSnapshot> Components { get; set; } = new();
    public DateTime CapturedAt { get; set; }

    public BannerSnapshot() { }

    public BannerSnapshot(Banner banner)
    {
        if (banner == null)
            throw new ArgumentNullException(nameof(banner));

        Name = banner.Name;
        Description = banner.Description;
        Width = banner.Width;
        Height = banner.Height;
        CapturedAt = DateTime.UtcNow;
        Components = banner.Components
            .OrderBy(c => c.ZIndex)
            .Select(c => new ComponentSnapshot(c))
            .ToList();
    }
}
