---
unit: 001-banner-service
bolt: 002-banner-service
stage: model
status: complete
created: 2026-09-26T00:00:00Z
---

# Static Model - Banner Service (Bolt 002: Video & Graphics)

## Bounded Context

Extends the Banner Service domain to support Video and Graphics components. Builds on Bolt 001's foundation (Banner and Component entities, Text and Image components). This bolt introduces new component types while maintaining the existing aggregate structure and component management operations.

**Scope**: Video and Graphics component types, property validation  
**Builds On**: Bolt 001 (Banner, Component, Text, Image)  
**Out of Scope**: Effects application, media processing (handled by Effects Engine)

---

## Domain Entities

### Banner (Existing - Reused from Bolt 001)
- Properties: Id, ShopId, UserId, Name, Description, Width, Height, CreatedAt, UpdatedAt, IsPublished
- Invariants: Component count ≤ 50, unique ZIndex per banner, data isolation by ShopId

### Component (Extended from Bolt 001)
- New types: ComponentType now includes Video and Graphics (previously Text and Image only)
- Properties: Id, BannerId, ComponentType, Position, Size, ZIndex, PropertiesJson, CreatedAt, UpdatedAt
- Rules: Each type has specific properties stored in JSON

---

## Value Objects

### Existing Value Objects (from Bolt 001)
- **Position** (X, Y coordinates)
- **Size** (Width, Height)
- **TextComponentProperties** (Content, FontFamily, FontSize, Color, FontWeight, TextAlign)
- **ImageComponentProperties** (ImageReference, FilterEffect, Opacity, RotationDegrees)

### New Value Objects (Bolt 002)

**VideoComponentProperties**
- Properties:
  - VideoReference (string URL) - Reference to video file
  - Autoplay (bool) - Start playing automatically
  - Loop (bool) - Repeat video when finished
  - Muted (bool) - Start with sound muted
  - Volume (float) - Volume level (0-1)
  - StartTime (int) - Seconds to start playback
  - Duration (int) - Maximum duration in seconds
- Constraints:
  - VideoReference must be valid URL
  - Volume range: 0-1
  - Duration > 0

**GraphicsComponentProperties**
- Properties:
  - ShapeType (enum: Rectangle, Circle, Triangle, Polygon) - Shape type
  - FillColor (hex string) - Shape fill color
  - BorderColor (hex string) - Border/outline color
  - BorderWidth (int) - Border thickness in pixels
  - BorderStyle (enum: Solid, Dashed, Dotted) - Border style
  - CornerRadius (int) - Rounded corner radius (for Rectangle)
  - Rotation (float) - Rotation in degrees (-360 to 360)
- Constraints:
  - Colors must be valid hex format (#RRGGBB)
  - BorderWidth: 0-10 pixels
  - CornerRadius: 0-100 pixels
  - Rotation: -360 to 360 degrees

---

## Aggregates

**Banner** (Aggregate Root - Unchanged)
- Root: Banner entity
- Members: Component collection
- Invariants:
  - All components belong to this banner
  - Total components ≤ 50
  - No duplicate ZIndex values
  - ShopId immutable and consistent
  - Multi-tenant data isolation

---

## Domain Events

### Existing Events (from Bolt 001)
- BannerCreated
- ComponentAdded
- ComponentUpdated
- ComponentRemoved
- BannerPublished
- BannerUnpublished
- PreviewGenerated

### New Events (Bolt 002)
- **VideoComponentAdded** - Triggered when video component added
  - Payload: BannerId, ComponentId, VideoReference, StartTime
  
- **GraphicsComponentAdded** - Triggered when graphics component added
  - Payload: BannerId, ComponentId, ShapeType, FillColor

- **ComponentPropertiesUpdated** - When component properties changed
  - Payload: BannerId, ComponentId, UpdatedProperties, UpdatedAt

---

## Domain Services

### Existing Services (from Bolt 001)
- **BannerDomainService** - Banner CRUD operations
- **ComponentValidationService** - Generic component validation

### Extended Service (Bolt 002)

**ComponentValidationService** (Extended)
- Existing methods:
  - ValidatePosition(x, y, bannerWidth, bannerHeight)
  - ValidateSize(width, height)
  - ValidateZIndex(zIndex)
  - ValidateComponentCount(currentCount)

- New methods:
  - **ValidateVideoComponent**(properties: VideoComponentProperties)
    - Validates VideoReference is valid URL
    - Validates Volume (0-1 range)
    - Validates Duration > 0
    - Validates Autoplay, Loop, Muted boolean values

  - **ValidateGraphicsComponent**(properties: GraphicsComponentProperties)
    - Validates hex colors (#RRGGBB format)
    - Validates ShapeType enum value
    - Validates BorderWidth (0-10 pixels)
    - Validates CornerRadius (0-100 pixels)
    - Validates Rotation (-360 to 360)
    - Validates BorderStyle enum value

  - **ValidateComponentProperties**(componentType: ComponentType, properties: object)
    - Dispatches to type-specific validation methods

---

## Repository Interfaces

No new interfaces needed. Existing repositories (IBannerRepository, IComponentRepository) handle all component types uniformly via the PropertiesJson field.

---

## Ubiquitous Language (Extended)

### New Terms

| Term | Definition |
|------|-----------|
| **Video Component** | Component displaying video content with playback controls (autoplay, loop, volume) |
| **Graphics Component** | Component rendering geometric shapes (rectangle, circle, triangle) with fill/border styling |
| **Shape Type** | Category of graphic shape (Rectangle, Circle, Triangle, Polygon) |
| **VideoReference** | URL pointing to video file (mp4, webm, etc.) hosted in Media Service |
| **Autoplay** | Boolean flag to start video playback automatically when banner loads |
| **Loop** | Boolean flag to repeat video when it finishes playing |
| **Muted** | Boolean flag indicating audio disabled on component load |
| **Volume** | Audio level from 0 (silent) to 1 (maximum), applies when Muted is false |
| **FillColor** | Interior color of graphics component, specified as hex (#RRGGBB) |
| **BorderColor** | Outline color of graphics component, specified as hex (#RRGGBB) |
| **BorderWidth** | Thickness of graphics component outline in pixels |
| **BorderStyle** | Pattern of graphics outline (Solid, Dashed, Dotted) |
| **CornerRadius** | Degree of roundness for rectangle corners (0 = sharp, 100 = fully rounded) |

---

## Design Notes

- **Polymorphic Component System**: Four component types (Text, Image, Video, Graphics) managed through single Component entity with ComponentType discriminator and JSON properties
- **Property Validation**: Each type has specific constraints enforced by ComponentValidationService
- **No New Entities**: Video and Graphics integrate seamlessly using existing architecture
- **JSON Storage Strategy** (per ADR-002): Component properties stored as JSON, validated at application layer
- **Future Extension**: New component types (3D, Animation, etc.) can be added by creating new property value objects without schema changes
- **Immutable Type**: Once a component is created with a specific type, the type cannot change (only properties within that type can be updated)

---

## Coverage Against Stories

✅ **Story 004-add-video-component**: VideoComponentProperties with all video-specific fields  
✅ **Story 005-add-graphics-component**: GraphicsComponentProperties with shape and styling fields  
✅ **Story 006-update-component-properties**: UpdateComponent operation supports all property types

---

## Relationship to Bolt 001

**Builds On**:
- Banner entity and aggregate root
- Component entity structure
- Component validation patterns
- JSON property storage strategy

**Extends**:
- ComponentType enum (adds Video, Graphics)
- ComponentValidationService (adds type-specific validators)
- Component property value objects (adds Video and Graphics)

**Maintains**:
- Multi-tenant isolation
- ZIndex uniqueness
- 50-component limit per banner
- Repository pattern for data access

---

## Acceptance Criteria Validation

### Story 004: Add Video Component
- [ ] VideoComponentProperties with video-specific fields
- [ ] Autoplay, loop, muted, volume properties validated
- [ ] VideoReference URL validation
- [ ] Component added to banner and stored with video properties

### Story 005: Add Graphics Component  
- [ ] GraphicsComponentProperties with shape fields
- [ ] FillColor and BorderColor hex validation
- [ ] BorderWidth and CornerRadius range validation
- [ ] Component added to banner with graphics properties

### Story 006: Update Component Properties
- [ ] UpdateComponent supports all component types
- [ ] Properties validated per component type
- [ ] UpdatedAt timestamp updated on property change
- [ ] Existing Text/Image components unaffected
