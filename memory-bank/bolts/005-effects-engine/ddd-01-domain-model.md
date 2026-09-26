# DDD-01: Domain Model — Effects Engine

**Status**: Stage 1 of 5 (Domain Model)  
**Bolt**: 005-effects-engine  
**Created**: 2026-09-26  

---

## Problem Statement

Components are static. Users need to:
1. **Apply visual effects** (opacity, rotation, scale, blur, animations)
2. **Configure carousels** (rotate through images/videos on timer)
3. **Control effect parameters** (duration, delay, timing curves)

Currently, components have no visual effects or animations.

---

## Core Domain

### Value Object: EffectLibrary

Catalog of all available effects with validation rules.

**Available Effects** (by type):

#### Opacity Effect
- `opacity`: decimal (0.0 to 1.0)
- `duration`: int (milliseconds, 0-5000)
- `delay`: int (milliseconds, 0-5000)

#### Rotation Effect
- `rotation`: decimal (degrees, -360 to 360)
- `duration`: int (milliseconds, 0-5000)
- `delay`: int (milliseconds, 0-5000)
- `timingCurve`: enum (linear, ease-in, ease-out, ease-in-out)

#### Scale Effect
- `scaleX`: decimal (0.1 to 2.0)
- `scaleY`: decimal (0.1 to 2.0)
- `duration`: int (milliseconds, 0-5000)
- `delay`: int (milliseconds, 0-5000)
- `origin`: enum (center, top-left, top-right, bottom-left, bottom-right)

#### Blur Effect
- `blurRadius`: int (pixels, 0-50)
- `duration`: int (milliseconds, 0-5000)
- `delay`: int (milliseconds, 0-5000)

#### Animation Effect
- `animationType`: enum (fade-in, fade-out, slide-left, slide-right, pulse, bounce)
- `duration`: int (milliseconds, 200-3000)
- `delay`: int (milliseconds, 0-5000)
- `repeat`: int (times, 1-unlimited)
- `repeatDelay`: int (milliseconds, 0-5000)

**Validation Rules**:
- Opacity: 0.0-1.0 (inclusive)
- Duration: 0-5000ms
- Delay: 0-5000ms
- Rotation: -360 to 360 degrees
- BlurRadius: 0-50px
- Scale: 0.1 to 2.0

---

### Value Object: Effect

Represents a single effect applied to a component.

**Fields**:
- `Id` (Guid) - unique effect identifier
- `EffectType` (enum) - Opacity, Rotation, Scale, Blur, Animation
- `Parameters` (Dictionary<string, object>) - effect-specific parameters
- `IsEnabled` (bool) - whether effect is active (default true)
- `CreatedAt` (DateTime) - when effect was added

**Constraints**:
- EffectType must match parameters
- Parameters validated against EffectLibrary rules
- Immutable after creation (modifications create new effect)

**Validation**:
```csharp
ValidateOpacity(opacity, duration, delay)
ValidateRotation(rotation, duration, delay, timingCurve)
ValidateScale(scaleX, scaleY, duration, delay, origin)
ValidateBlur(blurRadius, duration, delay)
ValidateAnimation(animationType, duration, delay, repeat, repeatDelay)
```

---

### Value Object: Carousel

Configuration for carousel rotation (image/video cycling).

**Fields**:
- `Id` (Guid) - carousel identifier
- `ComponentIds` (List<Guid>) - which components are in carousel
- `IntervalMs` (int) - milliseconds between transitions (1000-30000)
- `TransitionDuration` (int) - animation duration (200-2000ms)
- `TransitionType` (enum) - fade, slide-left, slide-right, zoom
- `IsAutoplay` (bool) - auto-start carousel
- `Loop` (bool) - restart after last item
- `CreatedAt` (DateTime)

**Constraints**:
- IntervalMs: 1000-30000 (1-30 seconds)
- TransitionDuration: 200-2000 (0.2-2 seconds)
- ComponentIds must be image or video type
- ComponentIds must belong to same banner
- At least 2 components required

**Invariants**:
- Carousel cannot be created with fewer than 2 components
- All components must be in banner
- Cannot add non-image/video components

---

### Entity: ComponentEffect

Represents a component with its effects applied.

**Structure**:
```
Component (from Bolt 001)
  + Effects: List<Effect>
  + Carousel: Carousel? (optional)
```

**Behavior**:
- Add effect to component
- Remove effect from component
- Update effect parameters (immutable: creates new effect)
- Enable/disable effect
- Apply carousel rotation

---

### Domain Service: EffectService

Orchestrates effect creation, validation, and application.

**Operations**:

#### ApplyEffect(component: Component, effectType: EffectType, parameters: Dictionary) → Effect

**Purpose**: Add effect to component with validation.

**Process**:
1. Validate effect type is supported
2. Validate parameters match effect constraints
3. Create Effect object
4. Add to component.Effects list
5. Return effect

**Errors**:
- Throws if parameters invalid
- Throws if effect type not supported

---

#### ApplyCarousel(banner: Banner, componentIds: List<Guid>, config: CarouselConfig) → Carousel

**Purpose**: Create carousel from list of image/video components.

**Process**:
1. Validate all component IDs exist in banner
2. Validate all components are Image or Video type
3. Validate carousel config (interval, duration, etc.)
4. Create Carousel object
5. Add to banner.Carousels
6. Return carousel

**Constraints**:
- Minimum 2 components
- All must be same banner
- All must be image/video type
- IntervalMs: 1000-30000
- TransitionDuration: 200-2000

**Errors**:
- Throws if components not found
- Throws if component type invalid
- Throws if parameters invalid

---

#### RemoveEffect(component: Component, effectId: Guid) → void

**Purpose**: Remove effect from component.

**Process**:
1. Find effect in component.Effects
2. Remove from list
3. Component unchanged otherwise

**Errors**:
- Throws if effect not found

---

#### UpdateEffect(component: Component, effectId: Guid, newParameters: Dictionary) → Effect

**Purpose**: Modify effect parameters (creates new effect, removes old).

**Process**:
1. Find existing effect
2. Validate new parameters
3. Remove old effect
4. Create new effect with new parameters
5. Add to component
6. Return new effect

**Errors**:
- Throws if effect not found
- Throws if parameters invalid

---

### Domain Service: EffectValidator

Validates all effect parameters.

**Methods**:

```csharp
ValidateEffectType(effectType: EffectType) → bool
ValidateOpacityParameters(opacity, duration, delay) → ValidationResult
ValidateRotationParameters(rotation, duration, delay, timingCurve) → ValidationResult
ValidateScaleParameters(scaleX, scaleY, duration, delay, origin) → ValidationResult
ValidateBlurParameters(blurRadius, duration, delay) → ValidationResult
ValidateAnimationParameters(animationType, duration, delay, repeat, repeatDelay) → ValidationResult
ValidateCarouselConfig(config: CarouselConfig, banner: Banner) → ValidationResult
```

**Validation Rules**:
- Opacity: 0.0-1.0
- Rotation: -360 to 360
- Duration: 0-5000ms
- Delay: 0-5000ms
- BlurRadius: 0-50px
- Scale: 0.1-2.0
- IntervalMs: 1000-30000
- TransitionDuration: 200-2000
- Carousel: min 2 components, same banner

---

## Data Structures

### Component Enhancement (existing entity, new fields)

```csharp
public class Component
{
    // ... existing fields ...
    
    // NEW:
    public List<Effect> Effects { get; set; } = new();
    public Guid? CarouselId { get; set; }  // Which carousel this belongs to
}
```

### New Table: Effects

```sql
CREATE TABLE [dbo].[Effects] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [ComponentId] [uniqueidentifier] NOT NULL,
    [EffectType] [int] NOT NULL,  -- 1=Opacity, 2=Rotation, 3=Scale, 4=Blur, 5=Animation
    [Parameters] [nvarchar](max) NOT NULL,  -- JSON
    [IsEnabled] [bit] NOT NULL DEFAULT 1,
    [CreatedAt] [datetime2] NOT NULL,
    
    CONSTRAINT [FK_Effects_Components] 
        FOREIGN KEY ([ComponentId]) REFERENCES [dbo].[Components]([Id]),
    
    CONSTRAINT [IX_Effects_ComponentId] 
        INDEX ON ([ComponentId])
);
```

### New Table: Carousels

```sql
CREATE TABLE [dbo].[Carousels] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [BannerId] [uniqueidentifier] NOT NULL,
    [IntervalMs] [int] NOT NULL,
    [TransitionDuration] [int] NOT NULL,
    [TransitionType] [int] NOT NULL,  -- 1=fade, 2=slide-left, 3=slide-right, 4=zoom
    [IsAutoplay] [bit] NOT NULL DEFAULT 1,
    [Loop] [bit] NOT NULL DEFAULT 1,
    [CreatedAt] [datetime2] NOT NULL,
    
    CONSTRAINT [FK_Carousels_Banners] 
        FOREIGN KEY ([BannerId]) REFERENCES [dbo].[Banners]([Id]),
    
    CONSTRAINT [IX_Carousels_BannerId] 
        INDEX ON ([BannerId])
);

CREATE TABLE [dbo].[CarouselComponents] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [CarouselId] [uniqueidentifier] NOT NULL,
    [ComponentId] [uniqueidentifier] NOT NULL,
    [Order] [int] NOT NULL,  -- Sequence in carousel
    
    CONSTRAINT [FK_CarouselComponents_Carousel] 
        FOREIGN KEY ([CarouselId]) REFERENCES [dbo].[Carousels]([Id]),
    
    CONSTRAINT [FK_CarouselComponents_Component] 
        FOREIGN KEY ([ComponentId]) REFERENCES [dbo].[Components]([Id])
);
```

---

## Stories Mapping

### S12: Define Effect Library
- Service: EffectValidator
- Domain: Effect value object with 5 effect types
- Validation: All parameters validated per type
- Result: Reusable effect library with constraints

### S13: Apply Effects
- Service: EffectService.ApplyEffect()
- API: POST /api/banners/{bannerId}/components/{componentId}/effects
- Validation: Parameters checked against EffectLibrary
- Result: Effects persisted to database

### S14: Carousel Configuration
- Service: EffectService.ApplyCarousel()
- API: POST /api/banners/{bannerId}/carousels
- Validation: Components exist, correct type, same banner
- Result: Carousel created with rotation list

---

## Design Principles

✅ **Immutable Effects**: Cannot modify, create new instead  
✅ **Type-Safe Validation**: Each effect type validates its parameters  
✅ **Carousel Constraints**: Minimum 2 components, same type, same banner  
✅ **JSON Storage**: Parameters stored as JSON for flexibility  
✅ **Component Enhancement**: Minimal changes to existing Component entity  
✅ **Extensibility**: Easy to add new effect types  

---

## Decisions for ADR

1. **Effect storage location**: JSON in parameters vs relational fields
2. **Carousel implementation**: Separate table vs Component field
3. **Effect immutability**: Create new vs modify in place
4. **Supported effect types**: Fixed set vs extensible library
5. **Carousel auto-start behavior**: User-controlled vs default enabled

---

## Next: Technical Design (Stage 2)

Once domain model is approved:
- Database schema and migrations
- API endpoint design
- DTOs and request bodies
- Integration with Component entity
- Carousel rendering logic
