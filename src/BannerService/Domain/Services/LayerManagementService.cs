namespace BannerService.Domain.Services;

using Entities;
using ValueObjects;

public class LayerManagementService
{
    public LayerOrder ReorderComponent(Banner banner, Guid componentId, int newZIndex)
    {
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found");

        if (newZIndex < 0 || newZIndex > 100)
            throw new ArgumentException("ZIndex must be 0-100");

        var oldZIndex = component.ZIndex;

        if (oldZIndex == newZIndex)
            throw new InvalidOperationException("Component already at this z-index");

        // Check if z-index is occupied by another component
        var occupying = banner.Components.FirstOrDefault(c => c.ZIndex == newZIndex && c.Id != componentId);
        if (occupying != null)
        {
            // Swap z-indexes
            banner.UpdateComponent(occupying.Id,
                new Position(occupying.PositionX, occupying.PositionY),
                new Size(occupying.SizeWidth, occupying.SizeHeight),
                oldZIndex,
                occupying.PropertiesJson);
        }

        // Update target component
        banner.UpdateComponent(componentId,
            new Position(component.PositionX, component.PositionY),
            new Size(component.SizeWidth, component.SizeHeight),
            newZIndex,
            component.PropertiesJson);

        return new LayerOrder(componentId, oldZIndex, newZIndex, "reorder");
    }

    public LayerOrder MoveForward(Banner banner, Guid componentId)
    {
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found");

        if (component.ZIndex >= 100)
            throw new InvalidOperationException("Component is already at front");

        var nextZIndex = component.ZIndex + 1;

        // Find next available z-index moving forward
        while (nextZIndex <= 100 && banner.Components.Any(c => c.ZIndex == nextZIndex && c.Id != componentId))
            nextZIndex++;

        if (nextZIndex > 100)
            nextZIndex = 100;

        return ReorderComponent(banner, componentId, nextZIndex);
    }

    public LayerOrder MoveBackward(Banner banner, Guid componentId)
    {
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found");

        if (component.ZIndex <= 0)
            throw new InvalidOperationException("Component is already at back");

        var nextZIndex = component.ZIndex - 1;

        // Find next available z-index moving backward
        while (nextZIndex >= 0 && banner.Components.Any(c => c.ZIndex == nextZIndex && c.Id != componentId))
            nextZIndex--;

        if (nextZIndex < 0)
            nextZIndex = 0;

        return ReorderComponent(banner, componentId, nextZIndex);
    }

    public LayerOrder SendToFront(Banner banner, Guid componentId)
    {
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found");

        return ReorderComponent(banner, componentId, 100);
    }

    public LayerOrder SendToBack(Banner banner, Guid componentId)
    {
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found");

        return ReorderComponent(banner, componentId, 0);
    }
}
