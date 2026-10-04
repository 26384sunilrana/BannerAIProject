# DDD-02: Technical Design — Effects Engine

**Status**: Stage 2 of 5 (Technical Design)  
**Bolt**: 005-effects-engine  
**Created**: 2026-09-26  

---

## Architecture Overview

Effects Engine extends Banner Service with visual effects and carousel capabilities. It adds new domain services, entities, and API endpoints.

```
┌─────────────────────────────────────────────────────┐
│           Presentation Layer                         │
│  (Controllers, DTOs, API Endpoints)                 │
│  - EffectsController (NEW)                          │
│  - CarouselController (NEW)                         │
└──────────┬──────────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────────┐
│           Application Layer                          │
│  (Services, Dto Mapping, Orchestration)             │
│  - EffectService (NEW)                              │
│  - CarouselService (NEW)                            │
└──────────┬──────────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────────┐
│           Domain Layer                               │
│  (Entities, Value Objects, Domain Services)         │
│  - Effect value object (NEW)                        │
│  - Carousel value object (NEW)                      │
│  - EffectValidator (NEW)                            │
│  - Component (UPDATED - add Effects list)           │
└──────────┬──────────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────────┐
│           Infrastructure Layer                       │
│  (Database, Repositories, Migrations)               │
│  - Effects table (NEW)                              │
│  - Carousels table (NEW)                            │
│  - CarouselComponents junction table (NEW)          │
│  - Migrations (NEW)                                 │
└─────────────────────────────────────────────────────┘
```

---

## Database Schema

### Effects Table

```sql
CREATE TABLE [dbo].[Effects] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [ComponentId] [uniqueidentifier] NOT NULL,
    [EffectType] [int] NOT NULL,
    [Parameters] [nvarchar](max) NOT NULL,
    [IsEnabled] [bit] NOT NULL DEFAULT 1,
    [CreatedAt] [datetime2] NOT NULL,
    
    CONSTRAINT [FK_Effects_Components] 
        FOREIGN KEY ([ComponentId]) REFERENCES [dbo].[Components]([Id]) ON DELETE CASCADE,
    
    CONSTRAINT [IX_Effects_ComponentId] 
        INDEX ON ([ComponentId])
);
```

**EffectType enum**:
- 1 = Opacity
- 2 = Rotation
- 3 = Scale
- 4 = Blur
- 5 = Animation

**Parameters JSON examples**:
```json
Opacity: {"opacity": 0.5, "duration": 1000, "delay": 0}
Rotation: {"rotation": 45, "duration": 2000, "delay": 500, "timingCurve": "ease-in"}
Scale: {"scaleX": 1.5, "scaleY": 1.5, "duration": 1500, "delay": 0, "origin": "center"}
Blur: {"blurRadius": 10, "duration": 800, "delay": 0}
Animation: {"animationType": "fade-in", "duration": 1000, "delay": 0, "repeat": 1, "repeatDelay": 0}
```

### Carousels Table

```sql
CREATE TABLE [dbo].[Carousels] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [BannerId] [uniqueidentifier] NOT NULL,
    [IntervalMs] [int] NOT NULL,
    [TransitionDuration] [int] NOT NULL,
    [TransitionType] [int] NOT NULL,
    [IsAutoplay] [bit] NOT NULL DEFAULT 1,
    [Loop] [bit] NOT NULL DEFAULT 1,
    [CreatedAt] [datetime2] NOT NULL,
    
    CONSTRAINT [FK_Carousels_Banners] 
        FOREIGN KEY ([BannerId]) REFERENCES [dbo].[Banners]([Id]) ON DELETE CASCADE,
    
    CONSTRAINT [IX_Carousels_BannerId] 
        INDEX ON ([BannerId])
);
```

**TransitionType enum**:
- 1 = Fade
- 2 = Slide-Left
- 3 = Slide-Right
- 4 = Zoom

### CarouselComponents Junction Table

```sql
CREATE TABLE [dbo].[CarouselComponents] (
    [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
    [CarouselId] [uniqueidentifier] NOT NULL,
    [ComponentId] [uniqueidentifier] NOT NULL,
    [Order] [int] NOT NULL,
    
    CONSTRAINT [FK_CarouselComponents_Carousel] 
        FOREIGN KEY ([CarouselId]) REFERENCES [dbo].[Carousels]([Id]) ON DELETE CASCADE,
    
    CONSTRAINT [FK_CarouselComponents_Component] 
        FOREIGN KEY ([ComponentId]) REFERENCES [dbo].[Components]([Id]),
    
    CONSTRAINT [IX_CarouselComponents_Unique] 
        UNIQUE ([CarouselId], [ComponentId])
);
```

---

## Entity Model (C# Code Structure)

### Domain Layer Entities

```csharp
// Domain/ValueObjects/Effect.cs
public class Effect
{
    public Guid Id { get; set; }
    public int EffectType { get; set; }  // 1-5
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Effect() { }

    public Effect(int effectType, Dictionary<string, object> parameters)
    {
        Id = Guid.NewGuid();
        EffectType = effectType;
        Parameters = parameters ?? new();
        CreatedAt = DateTime.UtcNow;
        IsEnabled = true;
    }
}

// Domain/ValueObjects/Carousel.cs
public class Carousel
{
    public Guid Id { get; set; }
    public Guid BannerId { get; set; }
    public int IntervalMs { get; set; }
    public int TransitionDuration { get; set; }
    public int TransitionType { get; set; }
    public bool IsAutoplay { get; set; } = true;
    public bool Loop { get; set; } = true;
    public List<Guid> ComponentIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public Carousel() { }

    public Carousel(Guid bannerId, int intervalMs, int transitionDuration, 
        int transitionType, List<Guid> componentIds)
    {
        Id = Guid.NewGuid();
        BannerId = bannerId;
        IntervalMs = intervalMs;
        TransitionDuration = transitionDuration;
        TransitionType = transitionType;
        ComponentIds = componentIds ?? new();
        CreatedAt = DateTime.UtcNow;
    }
}

// Domain/Entities/Component.cs (UPDATE)
public class Component
{
    // ... existing fields ...
    
    public List<Effect> Effects { get; set; } = new();
    public Guid? CarouselId { get; set; }
    
    public void AddEffect(Effect effect)
    {
        if (effect == null) throw new ArgumentNullException(nameof(effect));
        Effects.Add(effect);
    }
    
    public void RemoveEffect(Guid effectId)
    {
        var effect = Effects.FirstOrDefault(e => e.Id == effectId);
        if (effect != null) Effects.Remove(effect);
    }
}
```

### Domain Services

```csharp
// Domain/Services/EffectValidator.cs
public class EffectValidator
{
    public ValidationResult ValidateOpacity(decimal opacity, int duration, int delay)
    {
        if (opacity < 0 || opacity > 1)
            return ValidationResult.Failure("Opacity must be 0.0-1.0");
        if (duration < 0 || duration > 5000)
            return ValidationResult.Failure("Duration must be 0-5000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateRotation(decimal rotation, int duration, int delay, string timingCurve)
    {
        if (rotation < -360 || rotation > 360)
            return ValidationResult.Failure("Rotation must be -360 to 360 degrees");
        if (duration < 0 || duration > 5000)
            return ValidationResult.Failure("Duration must be 0-5000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        var validCurves = new[] { "linear", "ease-in", "ease-out", "ease-in-out" };
        if (!validCurves.Contains(timingCurve))
            return ValidationResult.Failure("Invalid timing curve");
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateScale(decimal scaleX, decimal scaleY, int duration, int delay, string origin)
    {
        if (scaleX < 0.1 || scaleX > 2.0 || scaleY < 0.1 || scaleY > 2.0)
            return ValidationResult.Failure("Scale must be 0.1-2.0");
        if (duration < 0 || duration > 5000)
            return ValidationResult.Failure("Duration must be 0-5000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        var validOrigins = new[] { "center", "top-left", "top-right", "bottom-left", "bottom-right" };
        if (!validOrigins.Contains(origin))
            return ValidationResult.Failure("Invalid origin");
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateBlur(int blurRadius, int duration, int delay)
    {
        if (blurRadius < 0 || blurRadius > 50)
            return ValidationResult.Failure("Blur radius must be 0-50px");
        if (duration < 0 || duration > 5000)
            return ValidationResult.Failure("Duration must be 0-5000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateAnimation(string animationType, int duration, int delay, int repeat, int repeatDelay)
    {
        var validTypes = new[] { "fade-in", "fade-out", "slide-left", "slide-right", "pulse", "bounce" };
        if (!validTypes.Contains(animationType))
            return ValidationResult.Failure("Invalid animation type");
        if (duration < 200 || duration > 3000)
            return ValidationResult.Failure("Duration must be 200-3000ms");
        if (delay < 0 || delay > 5000)
            return ValidationResult.Failure("Delay must be 0-5000ms");
        if (repeat < 1)
            return ValidationResult.Failure("Repeat must be >= 1");
        if (repeatDelay < 0 || repeatDelay > 5000)
            return ValidationResult.Failure("Repeat delay must be 0-5000ms");
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateCarouselConfig(Carousel carousel, Banner banner)
    {
        if (carousel.ComponentIds.Count < 2)
            return ValidationResult.Failure("Carousel requires at least 2 components");
        if (carousel.IntervalMs < 1000 || carousel.IntervalMs > 30000)
            return ValidationResult.Failure("Interval must be 1000-30000ms");
        if (carousel.TransitionDuration < 200 || carousel.TransitionDuration > 2000)
            return ValidationResult.Failure("Transition duration must be 200-2000ms");
        
        // Verify all components exist and are image/video
        foreach (var componentId in carousel.ComponentIds)
        {
            var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
            if (component == null)
                return ValidationResult.Failure($"Component {componentId} not found");
            if (component.ComponentType != ComponentType.Image && component.ComponentType != ComponentType.Video)
                return ValidationResult.Failure("Carousel components must be Image or Video");
        }
        
        return ValidationResult.Success();
    }
}
```

### Application Layer Services

```csharp
// Application/Services/EffectService.cs
public interface IEffectService
{
    Task<Effect> ApplyEffectAsync(Guid bannerId, Guid componentId, Guid shopId, 
        int effectType, Dictionary<string, object> parameters);
    Task RemoveEffectAsync(Guid bannerId, Guid componentId, Guid effectId, Guid shopId);
    Task<Effect> UpdateEffectAsync(Guid bannerId, Guid componentId, Guid effectId, 
        Dictionary<string, object> newParameters, Guid shopId);
    Task<List<Effect>> GetComponentEffectsAsync(Guid bannerId, Guid componentId, Guid shopId);
}

public class EffectService : IEffectService
{
    private readonly IBannerRepository _bannerRepository;
    private readonly IComponentRepository _componentRepository;
    private readonly EffectValidator _validator;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Effect> ApplyEffectAsync(Guid bannerId, Guid componentId, Guid shopId, 
        int effectType, Dictionary<string, object> parameters)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null) throw new KeyNotFoundException("Banner not found");
        
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null) throw new KeyNotFoundException("Component not found");
        
        // Validate parameters based on effect type
        var validationResult = ValidateEffectParameters(effectType, parameters);
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.Error);
        
        var effect = new Effect(effectType, parameters);
        component.AddEffect(effect);
        
        await _bannerRepository.UpdateAsync(banner);
        await _unitOfWork.CommitAsync();
        
        return effect;
    }
    
    public async Task RemoveEffectAsync(Guid bannerId, Guid componentId, Guid effectId, Guid shopId)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null) throw new KeyNotFoundException("Banner not found");
        
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null) throw new KeyNotFoundException("Component not found");
        
        component.RemoveEffect(effectId);
        
        await _bannerRepository.UpdateAsync(banner);
        await _unitOfWork.CommitAsync();
    }
    
    public async Task<Effect> UpdateEffectAsync(Guid bannerId, Guid componentId, Guid effectId, 
        Dictionary<string, object> newParameters, Guid shopId)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null) throw new KeyNotFoundException("Banner not found");
        
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null) throw new KeyNotFoundException("Component not found");
        
        var effect = component.Effects.FirstOrDefault(e => e.Id == effectId);
        if (effect == null) throw new KeyNotFoundException("Effect not found");
        
        var validationResult = ValidateEffectParameters(effect.EffectType, newParameters);
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.Error);
        
        component.RemoveEffect(effectId);
        var newEffect = new Effect(effect.EffectType, newParameters);
        component.AddEffect(newEffect);
        
        await _bannerRepository.UpdateAsync(banner);
        await _unitOfWork.CommitAsync();
        
        return newEffect;
    }
    
    public async Task<List<Effect>> GetComponentEffectsAsync(Guid bannerId, Guid componentId, Guid shopId)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null) throw new KeyNotFoundException("Banner not found");
        
        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null) throw new KeyNotFoundException("Component not found");
        
        return component.Effects.ToList();
    }
    
    private ValidationResult ValidateEffectParameters(int effectType, Dictionary<string, object> parameters)
    {
        return effectType switch
        {
            1 => _validator.ValidateOpacity(
                Convert.ToDecimal(parameters["opacity"]),
                Convert.ToInt32(parameters["duration"]),
                Convert.ToInt32(parameters["delay"])),
            2 => _validator.ValidateRotation(
                Convert.ToDecimal(parameters["rotation"]),
                Convert.ToInt32(parameters["duration"]),
                Convert.ToInt32(parameters["delay"]),
                parameters["timingCurve"].ToString()!),
            3 => _validator.ValidateScale(
                Convert.ToDecimal(parameters["scaleX"]),
                Convert.ToDecimal(parameters["scaleY"]),
                Convert.ToInt32(parameters["duration"]),
                Convert.ToInt32(parameters["delay"]),
                parameters["origin"].ToString()!),
            4 => _validator.ValidateBlur(
                Convert.ToInt32(parameters["blurRadius"]),
                Convert.ToInt32(parameters["duration"]),
                Convert.ToInt32(parameters["delay"])),
            5 => _validator.ValidateAnimation(
                parameters["animationType"].ToString()!,
                Convert.ToInt32(parameters["duration"]),
                Convert.ToInt32(parameters["delay"]),
                Convert.ToInt32(parameters["repeat"]),
                Convert.ToInt32(parameters["repeatDelay"])),
            _ => ValidationResult.Failure("Invalid effect type")
        };
    }
}

// Application/Services/CarouselService.cs
public interface ICarouselService
{
    Task<Carousel> CreateCarouselAsync(Guid bannerId, Guid shopId, int intervalMs, 
        int transitionDuration, int transitionType, List<Guid> componentIds);
    Task RemoveCarouselAsync(Guid carouselId, Guid shopId);
    Task<List<Carousel>> GetBannerCarouselsAsync(Guid bannerId, Guid shopId);
}

public class CarouselService : ICarouselService
{
    private readonly IBannerRepository _bannerRepository;
    private readonly EffectValidator _validator;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Carousel> CreateCarouselAsync(Guid bannerId, Guid shopId, int intervalMs, 
        int transitionDuration, int transitionType, List<Guid> componentIds)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null) throw new KeyNotFoundException("Banner not found");
        
        var carousel = new Carousel(bannerId, intervalMs, transitionDuration, transitionType, componentIds);
        var validationResult = _validator.ValidateCarouselConfig(carousel, banner);
        
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.Error);
        
        // Store carousel (implementation pending - requires repository)
        await _unitOfWork.CommitAsync();
        
        return carousel;
    }
    
    public async Task RemoveCarouselAsync(Guid carouselId, Guid shopId)
    {
        // Implementation pending
        await Task.CompletedTask;
    }
    
    public async Task<List<Carousel>> GetBannerCarouselsAsync(Guid bannerId, Guid shopId)
    {
        // Implementation pending
        return await Task.FromResult(new List<Carousel>());
    }
}
```

---

## Presentation Layer (API)

### DTOs

```csharp
// Application/Dto/EffectDto.cs
public class EffectDto
{
    public Guid Id { get; set; }
    public int EffectType { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApplyEffectRequestDto
{
    public int EffectType { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

// Application/Dto/CarouselDto.cs
public class CarouselDto
{
    public Guid Id { get; set; }
    public int IntervalMs { get; set; }
    public int TransitionDuration { get; set; }
    public int TransitionType { get; set; }
    public bool IsAutoplay { get; set; }
    public bool Loop { get; set; }
    public List<Guid> ComponentIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateCarouselRequestDto
{
    public int IntervalMs { get; set; }
    public int TransitionDuration { get; set; }
    public int TransitionType { get; set; }
    public List<Guid> ComponentIds { get; set; } = new();
}
```

### Controllers

```csharp
// Presentation/Controllers/EffectsController.cs
[ApiController]
[Route("api/banners/{bannerId}/components/{componentId}/effects")]
[Authorize]
public class EffectsController : ControllerBase
{
    private readonly IEffectService _effectService;
    
    [HttpPost]
    public async Task<IActionResult> ApplyEffect(Guid bannerId, Guid componentId, 
        [FromBody] ApplyEffectRequestDto request)
    {
        var shopId = GetShopId();
        var effect = await _effectService.ApplyEffectAsync(bannerId, componentId, shopId, 
            request.EffectType, request.Parameters);
        return Ok(MapToDto(effect));
    }
    
    [HttpGet]
    public async Task<IActionResult> ListEffects(Guid bannerId, Guid componentId)
    {
        var shopId = GetShopId();
        var effects = await _effectService.GetComponentEffectsAsync(bannerId, componentId, shopId);
        return Ok(effects.Select(MapToDto).ToList());
    }
    
    [HttpDelete("{effectId}")]
    public async Task<IActionResult> RemoveEffect(Guid bannerId, Guid componentId, Guid effectId)
    {
        var shopId = GetShopId();
        await _effectService.RemoveEffectAsync(bannerId, componentId, effectId, shopId);
        return NoContent();
    }
    
    private Guid GetShopId() => Guid.Parse(User.FindFirst("shop_id")?.Value ?? "");
}

// Presentation/Controllers/CarouselController.cs
[ApiController]
[Route("api/banners/{bannerId}/carousels")]
[Authorize]
public class CarouselController : ControllerBase
{
    private readonly ICarouselService _carouselService;
    
    [HttpPost]
    public async Task<IActionResult> CreateCarousel(Guid bannerId, 
        [FromBody] CreateCarouselRequestDto request)
    {
        var shopId = GetShopId();
        var carousel = await _carouselService.CreateCarouselAsync(bannerId, shopId,
            request.IntervalMs, request.TransitionDuration, request.TransitionType, 
            request.ComponentIds);
        return Ok(MapToDto(carousel));
    }
    
    [HttpGet]
    public async Task<IActionResult> ListCarousels(Guid bannerId)
    {
        var shopId = GetShopId();
        var carousels = await _carouselService.GetBannerCarouselsAsync(bannerId, shopId);
        return Ok(carousels.Select(MapToDto).ToList());
    }
    
    [HttpDelete("{carouselId}")]
    public async Task<IActionResult> RemoveCarousel(Guid bannerId, Guid carouselId)
    {
        var shopId = GetShopId();
        await _carouselService.RemoveCarouselAsync(carouselId, shopId);
        return NoContent();
    }
    
    private Guid GetShopId() => Guid.Parse(User.FindFirst("shop_id")?.Value ?? "");
}
```

---

## Integration Points

### Modification to ApplicationDbContext

```csharp
public DbSet<Effect> Effects { get; set; } = null!;
public DbSet<Carousel> Carousels { get; set; } = null!;

// In OnModelCreating:
modelBuilder.Entity<Effect>(entity => { /* config */ });
modelBuilder.Entity<Carousel>(entity => { /* config */ });
modelBuilder.Entity<Component>().Navigation(c => c.Effects).AutoInclude();
```

### Registration in Program.cs

```csharp
builder.Services.AddScoped<EffectValidator>();
builder.Services.AddScoped<IEffectService, EffectService>();
builder.Services.AddScoped<ICarouselService, CarouselService>();
```

---

## Next: ADR Analysis (Stage 3)

Key decisions to document:
1. Effect parameter storage (JSON vs typed columns)
2. Carousel auto-start behavior
3. Effect immutability strategy
4. Carousel component ordering
