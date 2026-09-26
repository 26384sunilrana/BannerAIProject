---
unit: 001-banner-service
bolt: 003-banner-service
stage: design
status: complete
created: 2026-09-26T00:00:00Z
---

# Technical Design - Banner Service (Bolt 003: Z-index & Preview)

## Architecture Summary

**No new layers.** Extends Bolt 001-002 architecture with:
- New domain service: LayerManagementService
- New value objects: LayerOrder, PreviewConfiguration, PreviewComponent
- Enhanced BannerService with reordering operations
- New API endpoint: POST /api/banners/{bannerId}/components/{componentId}/reorder

---

## Domain Layer Implementation

### New Service: LayerManagementService

```csharp
public class LayerManagementService
{
    public LayerOrder ReorderComponent(Banner banner, Guid componentId, int newZIndex)
    {
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException("Component not found");
        
        if (newZIndex < 0 || newZIndex > 100)
            throw new ArgumentException("ZIndex must be 0-100");
        
        var oldZIndex = component.ZIndex;
        
        // Check if z-index is occupied
        var occupying = banner.Components.FirstOrDefault(c => c.ZIndex == newZIndex && c.Id != componentId);
        if (occupying != null)
        {
            // Shift occupying component down
            banner.UpdateComponent(occupying.Id, 
                new Position(occupying.PositionX, occupying.PositionY),
                new Size(occupying.SizeWidth, occupying.SizeHeight),
                oldZIndex,  // Swap z-indexes
                occupying.PropertiesJson);
        }
        
        // Update target component
        banner.UpdateComponent(componentId,
            new Position(component.PositionX, component.PositionY),
            new Size(component.SizeWidth, component.SizeHeight),
            newZIndex,
            component.PropertiesJson);
        
        return new LayerOrder { ComponentId = componentId, OldZIndex = oldZIndex, NewZIndex = newZIndex };
    }
    
    public LayerOrder MoveForward(Banner banner, Guid componentId)
    {
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException("Component not found");
        
        var nextZIndex = component.ZIndex + 1;
        if (nextZIndex > 100)
            throw new InvalidOperationException("Component is already at front");
        
        // Find next available z-index
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
            throw new InvalidOperationException("Component not found");
        
        var nextZIndex = component.ZIndex - 1;
        if (nextZIndex < 0)
            throw new InvalidOperationException("Component is already at back");
        
        // Find next available z-index
        while (nextZIndex >= 0 && banner.Components.Any(c => c.ZIndex == nextZIndex && c.Id != componentId))
            nextZIndex--;
        
        if (nextZIndex < 0)
            nextZIndex = 0;
        
        return ReorderComponent(banner, componentId, nextZIndex);
    }
    
    public LayerOrder SendToFront(Banner banner, Guid componentId) => ReorderComponent(banner, componentId, 100);
    
    public LayerOrder SendToBack(Banner banner, Guid componentId) => ReorderComponent(banner, componentId, 0);
}
```

### Enhanced PreviewService

```csharp
public class PreviewService
{
    public PreviewConfiguration GeneratePreview(Banner banner)
    {
        // Sort components by z-index (ascending: 0 to 100)
        var sortedComponents = banner.Components
            .OrderBy(c => c.ZIndex)
            .Select(c => MapToPreviewComponent(c))
            .ToList();
        
        return new PreviewConfiguration
        {
            BannerId = banner.Id,
            ShopId = banner.ShopId,
            Name = banner.Name,
            Dimensions = new Size(banner.Width, banner.Height),
            Components = sortedComponents,
            GeneratedAt = DateTime.UtcNow
        };
    }
    
    private PreviewComponent MapToPreviewComponent(Component component)
    {
        var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(component.PropertiesJson);
        
        return new PreviewComponent
        {
            ComponentId = component.Id,
            ComponentType = (int)component.Type,
            Position = new Position(component.PositionX, component.PositionY),
            Size = new Size(component.SizeWidth, component.SizeHeight),
            ZIndex = component.ZIndex,
            Properties = properties
        };
    }
}
```

---

## Application Layer

### Enhanced BannerService

```csharp
public async Task<LayerOrderDto> ReorderComponentAsync(
    Guid bannerId, 
    Guid componentId, 
    Guid shopId, 
    int newZIndex)
{
    var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
    if (banner == null)
        throw new InvalidOperationException($"Banner {bannerId} not found");
    
    var layerOrder = _layerManagementService.ReorderComponent(banner, componentId, newZIndex);
    await _unitOfWork.BannerRepository.UpdateAsync(banner);
    
    return new LayerOrderDto
    {
        ComponentId = layerOrder.ComponentId,
        OldZIndex = layerOrder.OldZIndex,
        NewZIndex = layerOrder.NewZIndex
    };
}

public async Task<LayerOrderDto> MoveComponentForwardAsync(Guid bannerId, Guid componentId, Guid shopId)
{
    var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
    if (banner == null)
        throw new InvalidOperationException($"Banner {bannerId} not found");
    
    var layerOrder = _layerManagementService.MoveForward(banner, componentId);
    await _unitOfWork.BannerRepository.UpdateAsync(banner);
    
    return MapToLayerOrderDto(layerOrder);
}

// Similar methods for MoveBackward, SendToFront, SendToBack...
```

### New DTOs

```csharp
public class ReorderComponentRequestDto
{
    public int NewZIndex { get; set; }
}

public class LayerOrderDto
{
    public Guid ComponentId { get; set; }
    public int OldZIndex { get; set; }
    public int NewZIndex { get; set; }
}

public class PreviewConfigurationDto
{
    public Guid BannerId { get; set; }
    public Guid ShopId { get; set; }
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<PreviewComponentDto> Components { get; set; }
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
    public Dictionary<string, object> Properties { get; set; }
}
```

---

## Presentation Layer

### New API Endpoints

| Endpoint | Method | Request | Response | Purpose |
|----------|--------|---------|----------|---------|
| `/api/banners/{bannerId}/components/{componentId}/reorder` | POST | ReorderComponentRequestDto | LayerOrderDto | Set z-index to specific value |
| `/api/banners/{bannerId}/components/{componentId}/move-forward` | POST | None | LayerOrderDto | Move component forward one layer |
| `/api/banners/{bannerId}/components/{componentId}/move-backward` | POST | None | LayerOrderDto | Move component backward one layer |
| `/api/banners/{bannerId}/components/{componentId}/send-to-front` | POST | None | LayerOrderDto | Move component to front |
| `/api/banners/{bannerId}/components/{componentId}/send-to-back` | POST | None | LayerOrderDto | Move component to back |

### Controller Implementation

```csharp
[ApiController]
[Route("api/banners/{bannerId}/components")]
[Authorize]
public class LayerManagementController : ControllerBase
{
    [HttpPost("{componentId}/reorder")]
    [ProducesResponseType(typeof(LayerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderComponent(
        Guid bannerId,
        Guid componentId,
        [FromBody] ReorderComponentRequestDto request)
    {
        var shopId = GetShopId();
        var result = await _bannerService.ReorderComponentAsync(bannerId, componentId, shopId, request.NewZIndex);
        return Ok(result);
    }
    
    [HttpPost("{componentId}/move-forward")]
    [ProducesResponseType(typeof(LayerOrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveForward(Guid bannerId, Guid componentId)
    {
        var shopId = GetShopId();
        var result = await _bannerService.MoveComponentForwardAsync(bannerId, componentId, shopId);
        return Ok(result);
    }
    
    // Similar methods for move-backward, send-to-front, send-to-back...
}
```

---

## Database (No Schema Changes)

- ComponentType enum supports all 4 types ✅
- ZIndex column unchanged
- PropertiesJson supports all types ✅
- Existing indexes sufficient (IX_Components_BannerId_ZIndex) ✅

---

## Performance

| Operation | Complexity | Optimization |
|-----------|-----------|-----------------|
| Reorder component | O(n) - Find occupied slot | All in-memory (components ≤ 50) |
| Generate preview | O(n log n) - Sort by z-index | EF Core single query, sort in memory |
| Move forward/back | O(n) - Find next available | Scan forward/backward through 100 slots |

---

## Testing Strategy

- Unit tests: LayerManagementService reordering logic
- Integration tests: Banner z-index updates via repository
- API tests: Layer management endpoints
- Preview tests: Components sorted correctly by z-index

---

## Summary

✅ **No schema changes** - ComponentType and ZIndex already support needs  
✅ **Backward compatible** - Existing components unaffected  
✅ **Clean API** - Five intuitive layer management endpoints  
✅ **Performant** - All operations O(n) with n ≤ 50  
✅ **Complete** - Finishes component lifecycle and preview functionality
