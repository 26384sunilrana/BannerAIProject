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

    public bool ContentEquals(BannerSnapshot? other)
    {
        if (other == null || Name != other.Name || Description != other.Description ||
            Width != other.Width || Height != other.Height || Components.Count != other.Components.Count)
            return false;

        var mine = Components.OrderBy(c => c.ZIndex).ToList();
        var theirs = other.Components.OrderBy(c => c.ZIndex).ToList();
        for (var i = 0; i < mine.Count; i++)
        {
            var a = mine[i];
            var b = theirs[i];
            if (a.ComponentType != b.ComponentType || a.PositionX != b.PositionX || a.PositionY != b.PositionY ||
                a.SizeWidth != b.SizeWidth || a.SizeHeight != b.SizeHeight || a.ZIndex != b.ZIndex ||
                a.PropertiesJson != b.PropertiesJson)
                return false;
        }
        return true;
    }

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
