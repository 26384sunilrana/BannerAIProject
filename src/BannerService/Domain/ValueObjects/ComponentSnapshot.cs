namespace BannerService.Domain.ValueObjects;

using Entities;

public class ComponentSnapshot
{
    public Guid ComponentId { get; set; }
    public int ComponentType { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SizeWidth { get; set; }
    public int SizeHeight { get; set; }
    public int ZIndex { get; set; }
    public string PropertiesJson { get; set; } = string.Empty;

    public ComponentSnapshot() { }

    public ComponentSnapshot(Component component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        ComponentId = component.Id;
        ComponentType = (int)component.ComponentType;
        PositionX = component.PositionX;
        PositionY = component.PositionY;
        SizeWidth = component.SizeWidth;
        SizeHeight = component.SizeHeight;
        ZIndex = component.ZIndex;
        PropertiesJson = component.PropertiesJson;
    }
}
