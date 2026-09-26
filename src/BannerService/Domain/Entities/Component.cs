namespace BannerService.Domain.Entities;

using ValueObjects;

public class Component
{
    public Guid Id { get; set; }
    public Guid BannerId { get; set; }
    public ComponentType Type { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SizeWidth { get; set; }
    public int SizeHeight { get; set; }
    public int ZIndex { get; set; }
    public string PropertiesJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Component() { }

    public Component(
        Guid bannerId,
        ComponentType type,
        Position position,
        Size size,
        int zIndex,
        string propertiesJson)
    {
        Id = Guid.NewGuid();
        BannerId = bannerId;
        Type = type;
        PositionX = position.X;
        PositionY = position.Y;
        SizeWidth = size.Width;
        SizeHeight = size.Height;
        ZIndex = zIndex;
        PropertiesJson = propertiesJson;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(Position position, Size size, int zIndex, string propertiesJson)
    {
        PositionX = position.X;
        PositionY = position.Y;
        SizeWidth = size.Width;
        SizeHeight = size.Height;
        ZIndex = zIndex;
        PropertiesJson = propertiesJson;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ComponentType
{
    Text = 1,
    Image = 2,
    Video = 3,
    Graphics = 4
}
