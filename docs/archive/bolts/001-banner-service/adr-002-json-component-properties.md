---
bolt: 001-banner-service
created: 2026-09-26T00:00:00Z
status: accepted
---

# ADR-002: JSON Storage for Polymorphic Component Properties

## Context

The Banner Service supports multiple component types (Text, Image, Video, Graphics), each with different properties:

- **TextComponent**: content, fontFamily, fontSize, color, fontWeight, textAlign
- **ImageComponent**: imageReference, filterEffect, opacity, rotation
- **VideoComponent**: videoReference, autoplay, loop, volume (future)
- **GraphicsComponent**: shapeType, fillColor, borderColor (future)

This creates a **polymorphic data structure challenge**: How should we persist properties that differ by component type?

**Constraints**:
- Minimize database schema changes as new component types are added
- Keep queries performant (no complex joins)
- Maintain type safety at application layer (domain entities enforce constraints)
- Align with clean architecture (domain layer is type-safe; persistence layer is flexible)

## Decision

**Store component properties as JSON in a single NVARCHAR(MAX) column.**

Each Component row has:
- `ComponentType` (enum: Text, Image, Video, Graphics) as a regular column
- `Properties` (JSON string) containing type-specific fields

Application layer (BannerService) deserializes JSON to strongly-typed value objects based on ComponentType.

## Rationale

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| **Separate Tables per Type** | Fully normalized; enforces schema per type; easy to add type-specific constraints | Adds 4 tables; complex queries need outer joins; schema bloat; painful to add new types; violates "don't pay for what you don't use" | Adds complexity that doesn't pay off. We'd need to JOIN 4 tables just to get component properties. New types require migrations. |
| **EAV (Entity-Attribute-Value)** | Fully flexible; easy to add arbitrary properties | Notoriously slow (N queries for M properties); hard to enforce constraints; anti-pattern in SQL; queries become complex | Performance nightmare. A single component could require 6+ queries to get all properties. |
| **JSON in NVARCHAR(MAX)** | Flexible for new types; single table; queries simple; MSSQL has built-in JSON functions; application controls type safety | JSON querying in SQL is awkward (use application layer instead); no database-level type enforcement; requires careful serialization | This is the chosen approach. Trade JSON flexibility against application-layer type safety. |
| **SQL XML** | Alternative to JSON; MSSQL support | Verbose; fewer developer tools; JSON is standard now | JSON is more idiomatic for modern applications. |

**Why JSON Over Separate Tables?**

This bolt covers Text and Image. Future bolts will add Video and Graphics. If we created separate tables now, each new bolt would require:
1. Create new table (e.g., `VideoComponents`)
2. Add foreign key to `Components` 
3. Update queries to check type and join correct table
4. Migration scripts for production

**With JSON**:
1. Add new properties to JSON schema (no database change)
2. Application deserializes to `VideoComponentProperties`
3. Domain layer validates constraints (ComponentValidationService)
4. No migrations needed; backward compatible

Cost is shifted from database schema (expensive, hard to change) to application code (easy, can validate in unit tests).

**Why Not SQL-Level Validation?**

We could use MSSQL CHECK constraints to validate JSON structure, but:
- Constraint expressions become complex and hard to read
- Cannot enforce business rules (e.g., "FontSize must be 8-72")
- Application must validate anyway (don't trust client input)
- Team standards prefer application-layer validation (coding-standards.md)

Conclusion: **Application validates; database stores flexibly.**

## Consequences

### Positive

- ✅ **Schema extensibility**: Add Video and Graphics without migrations
- ✅ **Simple queries**: No joins needed; `SELECT * FROM Components` gets everything
- ✅ **Type flexibility**: Properties can evolve per type without changing table
- ✅ **Backward compatible**: Existing rows continue to work as new types added
- ✅ **MSSQL native support**: JSON functions available for filtered queries if needed
- ✅ **Smaller feature scope**: Current bolt (Text, Image) doesn't over-engineer for Video/Graphics

### Negative

- ⚠️ **No database-level type safety**: MSSQL can't enforce "VideoComponent.volume must be 0-100"
- ⚠️ **Serialization overhead**: JSON serialization/deserialization cost (negligible for 50 components)
- ⚠️ **SQL querying harder**: Filtering by property requires JSON path (e.g., `JSON_VALUE(Properties, '$.color')`)
- ⚠️ **Documentation burden**: Properties must be documented in code comments and schema docs

### Risks

- **Risk**: Developer stores incorrect JSON structure
  - **Mitigation**: 
    1. Value object constructors validate structure (TextComponentProperties validates color hex)
    2. Unit tests serialize/deserialize and verify round-tripping
    3. Controller DTOs enforce correct structure before reaching domain

- **Risk**: Query filtering by property becomes slow
  - **Mitigation**:
    1. Filtering always done in application, not SQL (easier to optimize)
    2. If performance becomes issue, add computed column with `PERSISTED` flag for frequently-filtered properties
    3. Can denormalize properties to regular columns later without changing storage model

## Related

- **Stories**: 002-add-text-component, 003-add-image-component (both use JSON storage)
- **Standards**: Should be documented in `standards/data-persistence-strategy.md` for future services
- **Previous ADRs**: ADR-001 (multi-tenancy) does not conflict; both use repository pattern
- **Depends On**: ADR-001 (ShopId filtering) - repository queries still filter by ShopId regardless of JSON
- **Future**: Video and Graphics components will store additional properties in same JSON column

---

## Implementation Notes

**Component Entity**:
```csharp
public class Component
{
    public Guid Id { get; set; }
    public Guid BannerId { get; set; }
    public ComponentType Type { get; set; }
    public string PropertiesJson { get; set; } // JSON stored here
    public Position Position { get; set; }
    public Size Size { get; set; }
    public int ZIndex { get; set; }
}

public enum ComponentType
{
    Text = 1,
    Image = 2,
    Video = 3,
    Graphics = 4
}
```

**Value Objects (Type-Safe)**:
```csharp
public class TextComponentProperties
{
    public string Content { get; set; }
    public string FontFamily { get; set; }
    public int FontSize { get; set; } // Validated: 8-72
    public string Color { get; set; } // Validated: hex format
    
    public static TextComponentProperties FromJson(string json)
    {
        // Deserialize and validate
        var obj = JsonConvert.DeserializeObject<TextComponentProperties>(json);
        Validate(obj);
        return obj;
    }
}
```

**EF Core Configuration**:
```csharp
public void Configure(EntityTypeBuilder<Component> builder)
{
    builder.Property(c => c.PropertiesJson)
        .HasColumnType("NVARCHAR(MAX)")
        .IsRequired();
    
    // No JSON constraints at database level
    // Validation happens in application layer
}
```

**Deserialization in Application Service**:
```csharp
public class ComponentService
{
    public ComponentResponse AddTextComponent(AddTextComponentRequest request)
    {
        var props = new TextComponentProperties 
        { 
            Content = request.Content,
            FontSize = request.FontSize, // Constructor validates
            Color = request.Color // Constructor validates
        };
        
        var component = new Component
        {
            Type = ComponentType.Text,
            PropertiesJson = JsonConvert.SerializeObject(props),
            // ... other properties
        };
        
        return _repository.CreateAsync(component);
    }
}
```

This decision allows the schema to grow with new component types while keeping queries simple and queries fast.
