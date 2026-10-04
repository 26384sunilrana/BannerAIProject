---
bolt: 001-banner-service
created: 2026-09-26T00:00:00Z
status: accepted
---

# ADR-003: Component Layering (ZIndex) Design

## Context

Components on a banner must be layered (one component can appear on top of another). The rendering order is critical for preview and frontend display.

The team must decide how to manage component layering:

1. **How are layers represented?** (zIndex integer, explicit ordering, relative positions)
2. **What constraints apply?** (must be unique per banner, gaps allowed, negative values)
3. **How are layers enforced?** (database check constraint, application validation, both)
4. **How are layers updated?** (reorder components, insert between layers, delete and shift)

**Constraints**:
- Support 50+ components per banner (each needs distinct layer)
- Maintain integrity: no two components with same zIndex in same banner
- Allow efficient preview generation (components ordered by zIndex)
- Prevent accidental layer conflicts when updating

## Decision

**Use integer ZIndex (0-100) per component with uniqueness enforced at application layer.**

Rules:
1. Each component has `ZIndex: int` (range 0-100)
2. No two components in same banner can have same ZIndex
3. ZIndex is immutable (changing layers requires delete + recreate or explicit reorder operation)
4. Application layer enforces uniqueness validation
5. Database has unique index on (BannerId, ZIndex) to prevent data corruption

## Rationale

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| **Integer ZIndex (0-999)** | Simple; widely understood; CSS-like; room for 1000 layers | Unbounded (could allocate zIndex 0, 100, 1000 wasting space) | Range should be bounded. 0-100 is sufficient for 50 components. |
| **Explicit Ordering (Position in List)** | Compact; always contiguous (0, 1, 2, ...); reorder is implicit | Reordering requires updating all components' positions (expensive); DELETE and INSERT shift all higher indices; error-prone | Too expensive. A single reorder operation would touch many rows. |
| **Gap-Free Ordering with Reorder Operation** | Compact; prevents gaps; explicit reorder semantics | Complex business logic; reorder must coordinate all affected components; hard to test | Adds complexity. ZIndex with gaps is simpler and sufficient. |
| **ZIndex with Gaps (Chosen)** | Simple; immutable once set; can insert between any two components; no bulk updates | May have gaps (zIndex 0, 1, 50, 100) wasting some values; requires unique index to prevent conflicts | Chosen: Gaps are acceptable. 0-100 range is still large for 50 components. |

**Why Integer Over Explicit Position?**

- **CSS Familiarity**: Developers know zIndex from web development
- **Immutability**: Once set, zIndex doesn't change unless explicitly reordered
- **Flexibility**: Easy to insert a new component between existing ones without moving other components
- **Queryability**: Can sort by `ORDER BY ZIndex` efficiently

**Why Application Validation?**

Could use database CHECK constraint to enforce uniqueness:
```sql
ALTER TABLE Components ADD CONSTRAINT UQ_BannerId_ZIndex 
UNIQUE (BannerId, ZIndex);
```

But this constraint alone is insufficient because:
1. Doesn't validate range (0-100)
2. Doesn't prevent zIndex out of logical range for banner size
3. Application must validate anyway (never trust database alone)
4. Unique index provides same conflict detection at query time

**Conclusion**: Database provides last-line defense via unique index; application provides business rule enforcement.

## Consequences

### Positive

- ✅ **Simple mental model**: Developers familiar with CSS zIndex understand immediately
- ✅ **Immutable after creation**: No accidental changes to layer order during component edits
- ✅ **Flexible insertion**: New component can go between any two existing components
- ✅ **Efficient queries**: `ORDER BY ZIndex` is simple and indexed
- ✅ **No bulk updates**: Adding or removing components doesn't shift others' zIndex
- ✅ **Preview generation efficient**: Preview sorts components by ZIndex in single O(n log n) operation

### Negative

- ⚠️ **May have gaps**: If you delete zIndex 50, the gap remains (zIndex 0, 1, 51, 52, ...)
- ⚠️ **Range limit**: Max 101 unique values (0-100) means max 101 layers theoretically (but 50 component limit applies first)
- ⚠️ **Immutability is strict**: To reorder, must delete and recreate component (loses component ID, history)

### Risks

- **Risk**: Developer assigns duplicate zIndex to two components in same banner
  - **Mitigation**:
    1. Application validation before insert/update: `ValidateZIndexUniqueness(bannerId, zIndex)`
    2. Database unique index `UQ_Components_BannerId_ZIndex` catches programmer errors
    3. Unit tests verify uniqueness validation with duplicates

- **Risk**: ZIndex value out of range (e.g., 500, -1)
  - **Mitigation**:
    1. ComponentValidationService enforces range: `if (zIndex < 0 || zIndex > 100) throw`
    2. Controller DTO validation (FluentValidation rule)
    3. Database CHECK constraint (optional, adds safety layer)

- **Risk**: Preview generation doesn't preserve intended layer order
  - **Mitigation**:
    1. Integration tests verify preview components are sorted by zIndex
    2. PreviewService explicitly sorts: `components.OrderBy(c => c.ZIndex)`

## Related

- **Stories**: 007-manage-component-z-index (future bolt)
- **Standards**: Should be documented in `standards/component-layering-strategy.md` for Effects Engine and UI services
- **Previous ADRs**: 
  - ADR-001 (multi-tenancy): ShopId filtering applies to component queries regardless of zIndex
  - ADR-002 (JSON properties): ZIndex is separate column; not stored in JSON
- **Dependencies**: Preview Service (future) must sort components by zIndex
- **Downstream**: Version Control Service will track zIndex changes; Effects Engine will render in zIndex order

---

## Implementation Notes

**Component Entity**:
```csharp
public class Component
{
    public Guid Id { get; set; }
    public Guid BannerId { get; set; }
    public int ZIndex { get; set; } // 0-100
    public ComponentType Type { get; set; }
    // ... other properties
}
```

**Validation in Domain Service**:
```csharp
public class ComponentValidationService
{
    public void ValidateZIndex(int zIndex)
    {
        if (zIndex < 0 || zIndex > 100)
            throw new BannerValidationException($"ZIndex must be 0-100, got {zIndex}");
    }
    
    public async Task ValidateZIndexUniqueAsync(Guid bannerId, int zIndex, IComponentRepository repo)
    {
        var exists = await repo.ExistsByZIndexAsync(bannerId, zIndex);
        if (exists)
            throw new BannerValidationException($"Component with zIndex {zIndex} already exists in banner {bannerId}");
    }
}
```

**EF Core Configuration**:
```csharp
public void Configure(EntityTypeBuilder<Component> builder)
{
    builder.HasIndex(c => new { c.BannerId, c.ZIndex }).IsUnique();
    
    builder.Property(c => c.ZIndex)
        .HasColumnType("INT")
        .IsRequired();
}
```

**Preview Generation**:
```csharp
public class PreviewService
{
    public PreviewResponse GeneratePreview(Banner banner, List<Component> components)
    {
        var sortedComponents = components.OrderBy(c => c.ZIndex).ToList();
        
        return new PreviewResponse
        {
            Components = sortedComponents.Select(c => MapToDto(c)).ToList()
        };
    }
}
```

**Controller DTO Validation**:
```csharp
public class AddComponentValidator : AbstractValidator<AddComponentRequest>
{
    public AddComponentValidator()
    {
        RuleFor(x => x.ZIndex)
            .InclusiveBetween(0, 100)
            .WithMessage("ZIndex must be between 0 and 100");
    }
}
```

This decision ensures that component layering is:
- Simple to understand and implement
- Performant for preview generation (single sort)
- Flexible for future features (reordering, effects that depend on layer order)
- Enforceable at both application and database level
