---
unit: 001-banner-service
bolt: 002-banner-service
stage: design
status: complete
created: 2026-09-26T00:00:00Z
---

# Technical Design - Banner Service (Bolt 002: Video & Graphics)

## Architecture Overview

**No new layers needed.** Bolt 002 extends Bolt 001's 4-layer architecture by adding new value object classes and updating ComponentValidationService. All existing controllers, repositories, and middleware continue to work unchanged.

**Changes**:
- Domain layer: Add VideoComponentProperties and GraphicsComponentProperties value objects
- Domain layer: Extend ComponentValidationService with type-specific validators
- Application layer: No changes (service handles all types uniformly via JSON)
- Infrastructure layer: No changes (repositories handle all types via ComponentType enum)
- Presentation layer: No changes (controllers already accept generic AddComponentRequestDto)

---

## Data Model (No Schema Changes)

**Existing Tables** (from Bolt 001):
- Banners table - Unchanged
- Components table - Unchanged

**Why No Schema Changes**:
- ComponentType enum already supports Video (3) and Graphics (4)
- PropertiesJson column stores any JSON structure
- Component validation happens at application layer
- No new columns needed

**Indexes Remain Effective**:
- IX_Components_BannerId_ZIndex - Supports all component types
- IX_Components_ComponentType - Now includes Video and Graphics rows

---

## Domain Layer Implementation

### New Value Objects

**VideoComponentProperties.cs**
```csharp
public class VideoComponentProperties
{
    public string VideoReference { get; set; } = string.Empty;
    public bool Autoplay { get; set; } = false;
    public bool Loop { get; set; } = false;
    public bool Muted { get; set; } = true;
    public float Volume { get; set; } = 1.0f; // 0-1
    public int StartTime { get; set; } = 0;
    public int Duration { get; set; } = int.MaxValue;
    
    public static VideoComponentProperties FromJson(string json)
    {
        // Deserialize and validate
    }
}
```

**GraphicsComponentProperties.cs**
```csharp
public class GraphicsComponentProperties
{
    public string ShapeType { get; set; } = "Rectangle";
    public string FillColor { get; set; } = "#FFFFFF";
    public string BorderColor { get; set; } = "#000000";
    public int BorderWidth { get; set; } = 1;
    public string BorderStyle { get; set; } = "Solid";
    public int CornerRadius { get; set; } = 0;
    public float Rotation { get; set; } = 0;
    
    public static GraphicsComponentProperties FromJson(string json)
    {
        // Deserialize and validate
    }
}
```

### Extended ComponentValidationService

```csharp
public class ComponentValidationService
{
    // Existing methods from Bolt 001...
    
    // New Video validation
    public void ValidateVideoComponent(VideoComponentProperties props)
    {
        if (!Uri.TryCreate(props.VideoReference, UriKind.Absolute, out _))
            throw new ArgumentException("Invalid video URL");
        
        if (props.Volume < 0 || props.Volume > 1)
            throw new ArgumentException("Volume must be 0-1");
        
        if (props.Duration <= 0)
            throw new ArgumentException("Duration must be positive");
    }
    
    // New Graphics validation
    public void ValidateGraphicsComponent(GraphicsComponentProperties props)
    {
        ValidateHexColor(props.FillColor);
        ValidateHexColor(props.BorderColor);
        
        if (props.BorderWidth < 0 || props.BorderWidth > 10)
            throw new ArgumentException("BorderWidth must be 0-10");
        
        if (props.CornerRadius < 0 || props.CornerRadius > 100)
            throw new ArgumentException("CornerRadius must be 0-100");
        
        if (props.Rotation < -360 || props.Rotation > 360)
            throw new ArgumentException("Rotation must be -360 to 360");
    }
    
    private bool IsValidHexColor(string color) => 
        Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
}
```

---

## Application Layer (No Changes)

**Existing BannerService**:
- AddComponentAsync() - Already handles all types via AddComponentRequestDto
- UpdateComponentAsync() - Already handles property updates
- GetPreviewAsync() - Already returns all component types

**Why No Changes**:
```csharp
// Existing code works for Video and Graphics:
var propertiesJson = JsonSerializer.Serialize(request.Properties);
var component = new Component(
    bannerId,
    (ComponentType)request.ComponentType,  // Now includes Video (3), Graphics (4)
    position,
    size,
    zIndex,
    propertiesJson);  // Accepts any JSON structure
```

---

## Presentation Layer (No Changes)

**Existing Endpoints** (from Bolt 001):
- POST /api/banners/{bannerId}/components - Accepts Video/Graphics
- PUT /api/banners/{bannerId}/components/{componentId} - Updates any type
- DELETE /api/banners/{bannerId}/components/{componentId} - Deletes any type

**Request DTO** (Generic - Already Extensible):
```csharp
public class AddComponentRequestDto
{
    public int ComponentType { get; set; }  // 3=Video, 4=Graphics
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int SizeWidth { get; set; }
    public int SizeHeight { get; set; }
    public int ZIndex { get; set; }
    public Dictionary<string, object> Properties { get; set; }  // Any JSON structure
}
```

**Example Request for Video**:
```json
{
  "componentType": 3,
  "positionX": 50,
  "positionY": 100,
  "sizeWidth": 600,
  "sizeHeight": 400,
  "zIndex": 5,
  "properties": {
    "videoReference": "https://media.example.com/video.mp4",
    "autoplay": true,
    "loop": false,
    "muted": false,
    "volume": 0.8,
    "startTime": 0,
    "duration": 120
  }
}
```

**Example Request for Graphics**:
```json
{
  "componentType": 4,
  "positionX": 200,
  "positionY": 150,
  "sizeWidth": 300,
  "sizeHeight": 300,
  "zIndex": 2,
  "properties": {
    "shapeType": "Circle",
    "fillColor": "#FF0000",
    "borderColor": "#000000",
    "borderWidth": 2,
    "borderStyle": "Solid",
    "cornerRadius": 0,
    "rotation": 0
  }
}
```

---

## Database Query Efficiency

**No changes to query strategy**:
- (ShopId, BannerId) composite index still optimized
- ComponentType filtering works as before
- ZIndex ordering unaffected by new types
- JSON validation happens at application layer

---

## Security (No Changes)

**Multi-tenant isolation** (ADR-001):
- All queries still filter by ShopId at repository level
- No cross-shop data leakage possible

**Input validation**:
- VideoComponentProperties validates URLs and ranges
- GraphicsComponentProperties validates colors and dimensions
- Existing Authorization checks apply to all component types

---

## Performance Targets (Met)

| Operation | Target | Implementation |
|-----------|--------|-----------------|
| Add Video Component | <200ms | JSON serialization + validation |
| Add Graphics Component | <200ms | JSON serialization + validation |
| Update Component Properties | <200ms | JSON update + EF Core save |
| Get Preview (50 components) | <200ms | Single query + JSON deserialization |

**Why Targets Met**:
- No new database queries
- JSON validation is O(1) per component
- No additional indexes needed
- EF Core handles all types uniformly

---

## Testing Strategy

**No changes to test infrastructure**:
- Same xUnit + Moq framework
- Same BannerRepository tests
- Same integration tests

**New test coverage**:
- Unit tests for VideoComponentProperties validation
- Unit tests for GraphicsComponentProperties validation
- Integration tests for adding Video components
- Integration tests for adding Graphics components
- API tests for Video/Graphics endpoints

---

## Deployment (No Changes)

**Docker image**:
- Same Dockerfile as Bolt 001
- No new dependencies
- Same runtime

**Configuration**:
- Same appsettings.json
- No new environment variables

**Kubernetes**:
- Same deployment manifest
- No additional resources needed

---

## Summary: Minimal Impact Design

**Key Points**:
✅ No new tables or migrations  
✅ No new API endpoints  
✅ No changes to existing services  
✅ No changes to authentication/authorization  
✅ Backward compatible with Bolt 001 components  
✅ Extensible for future component types  

**Architecture Decision**: Rather than creating new tables or endpoints for Video/Graphics, we leverage the existing polymorphic component system established in Bolt 001. This keeps the design clean and the codebase maintainable while supporting unlimited component types.
