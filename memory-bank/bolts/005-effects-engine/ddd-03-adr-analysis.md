# DDD-03: ADR Analysis — Effects Engine

**Status**: Stage 3 of 5 (ADR Analysis)  
**Bolt**: 005-effects-engine  
**Created**: 2026-09-26  

---

## ADR-001: Effect Parameter Storage (JSON vs Typed Columns)

**Status**: ✅ ACCEPTED

### Problem
Store effect-specific parameters. Options:
1. **JSON column** — All parameters in one NVARCHAR(MAX) column
2. **Typed columns** — Separate OpacityValue, RotationValue, etc. columns
3. **Normalized table** — One row per effect with all parameters

### Decision
**JSON column** — All parameters in single Parameters NVARCHAR(MAX).

### Rationale

| Aspect | JSON | Typed | Normalized |
|--------|------|-------|-----------|
| **Flexibility** | ✅ Easy to add effects | ❌ Fixed set | ⚠️ Complex |
| **Schema** | ✅ Single column | ❌ Many columns | ❌ Many tables |
| **Queries** | ⚠️ No SQL filtering | ✅ SQL filtering | ✅ SQL filtering |
| **Extensibility** | ✅ New effects easy | ❌ Requires migration | ❌ Requires migration |
| **Storage Size** | ✅ Compact | ✅ Compact | ❌ Verbose |

### Implementation
- Parameters stored as JSON: `{"opacity": 0.5, "duration": 1000, "delay": 0}`
- Deserialized in application layer
- Validation in EffectValidator
- No SQL-level type information

### Consequences
- ✅ Simple schema (single Parameters column)
- ✅ Easy to add new effect types
- ✅ Matches existing pattern (ComponentProperties)
- ⚠️ Cannot filter by effect parameters in SQL
- ⚠️ Type checking only at application layer

### Alternatives Rejected
- Typed columns: Inflexible, requires new columns per effect
- Normalized table: Overengineered for this scope

---

## ADR-002: Effect Immutability Strategy

**Status**: ✅ ACCEPTED

### Problem
Should effects be modifiable or immutable?
1. **Immutable** — Cannot modify, create new effect to update
2. **Mutable** — Modify parameters in place
3. **Hybrid** — Can disable but not modify

### Decision
**Immutable with IsEnabled flag** — Cannot modify parameters, must create new effect.

### Rationale
- Audit trail (old effect preserved)
- Simpler implementation (no update logic)
- Prevents accidental parameter changes
- Still allows disabling via flag

### Implementation
- `UpdateEffect()` removes old effect, creates new
- IsEnabled flag allows disable without deletion
- CreatedAt timestamp on each effect

### Consequences
- ✅ Audit trail maintained
- ✅ No update bugs
- ✅ Clear change history
- ⚠️ Many effects if user changes frequently
- 📋 Future: Could add archiving strategy

### Alternatives Rejected
- Mutable: Risk of audit trail loss
- Hybrid: Confusing (sometimes mutable, sometimes not)

---

## ADR-003: Carousel Component Ordering

**Status**: ✅ ACCEPTED

### Problem
How to track carousel component sequence?
1. **Explicit Order column** — Each component has Order int
2. **List order** — Rely on insertion order
3. **Separate sequence table** — Track position separately

### Decision
**Explicit Order column** in CarouselComponents junction table.

### Rationale
- Explicit order independent of insertion time
- Can reorder without recreating carousel
- Database enforces ordering
- Clear semantics (Order=1 is first)

### Implementation
```sql
CarouselComponents:
  - CarouselId
  - ComponentId
  - Order (int)
```

### Consequences
- ✅ Explicit ordering
- ✅ Can reorder components
- ✅ Database-enforced
- ⚠️ Requires Order management on updates

---

## ADR-004: Carousel Auto-Start Behavior

**Status**: ✅ ACCEPTED

### Problem
Should carousels auto-start by default?
1. **Auto-start** — Carousel begins immediately on load
2. **Manual start** — User must click play
3. **Configurable** — IsAutoplay flag

### Decision
**Configurable via IsAutoplay flag**, default true.

### Rationale
- Most carousels are auto-play (ads, promotions)
- Allows manual-play for special cases
- Simple boolean flag
- Matches user expectation

### Implementation
- `IsAutoplay` boolean (default = true)
- Passed in CreateCarouselRequestDto
- Rendered on frontend based on flag

### Consequences
- ✅ Flexible (both auto and manual)
- ✅ Default matches common use case
- ✅ Simple to implement
- ⚠️ Frontend must respect flag

---

## ADR-005: Effect Type Enumeration

**Status**: ✅ ACCEPTED

### Problem
Should effect types be:
1. **Fixed enum** — 1=Opacity, 2=Rotation, ... (predefined)
2. **Extensible** — Database table of effect types
3. **String-based** — "opacity", "rotation"

### Decision
**Fixed integer enum** with 5 predefined types.

### Rationale

| Type | Flexibility | Type Safety | Complexity |
|------|-------------|------------|-----------|
| **Fixed enum** | ❌ Limited | ✅ High | ✅ Low |
| **Extensible** | ✅ Unlimited | ⚠️ Medium | ❌ High |
| **String-based** | ✅ Unlimited | ❌ Low | ⚠️ Medium |

### Implementation
```csharp
enum EffectType
{
    Opacity = 1,
    Rotation = 2,
    Scale = 3,
    Blur = 4,
    Animation = 5
}
```

### Consequences
- ✅ Type-safe (integer)
- ✅ Simple validation
- ✅ Database efficient (int vs string)
- ⚠️ Cannot add new types without code change
- 📋 Future: Could extend if needed

### Alternatives Rejected
- Extensible: Overkill for current scope
- String-based: Weak type safety

---

## ADR-006: Carousel Multi-use Components

**Status**: ✅ ACCEPTED

### Problem
Can one component be in multiple carousels simultaneously?
1. **Multi-use** — Same component in many carousels
2. **Exclusive** — Component belongs to max one carousel
3. **One-way link** — CarouselId on Component table

### Decision
**Multi-use allowed** via junction table (many-to-many).

### Rationale
- Flexibility (same image in multiple carousels)
- No artificial constraints
- Junction table is standard pattern
- Supports all use cases

### Implementation
- CarouselComponents junction table
- No CarouselId on Component
- Component can appear in multiple carousels

### Consequences
- ✅ Flexible (one component, many carousels)
- ✅ Standard pattern
- ⚠️ Must clean up junction on component delete
- ⚠️ Potential for accidental duplication

---

## ADR-007: Effect Validation Timing

**Status**: ✅ ACCEPTED

### Problem
When to validate effect parameters?
1. **At creation** — Validate when ApplyEffect called
2. **At save** — Validate on database insert
3. **Both** — Application and database validation

### Decision
**Validate at application layer** (ApplyEffect), not database.

### Rationale
- Fail fast (return error before DB)
- Better user experience (immediate feedback)
- Centralized validation logic
- Database constraints are fallback

### Implementation
- EffectValidator validates in ApplyEffect
- Throws ArgumentException if invalid
- Database has no CHECK constraints

### Consequences
- ✅ Fast failure
- ✅ Centralized validation
- ✅ Better error messages
- ⚠️ Requires application validation on all inserts
- ⚠️ Database cannot enforce alone

---

## ADR-008: Carousel Duration Limits

**Status**: ✅ ACCEPTED

### Problem
What interval/duration limits should carousels have?
1. **Flexible** — No limits
2. **Reasonable bounds** — 1-30sec intervals, 0.2-2sec transitions
3. **Strict** — Very tight bounds

### Decision
**Reasonable bounds**: 1000-30000ms intervals, 200-2000ms transitions.

### Rationale
- 1sec minimum: Too fast carousel is unusable
- 30sec maximum: Too slow feels broken
- 0.2sec min transition: Smoother than 100ms
- 2sec max transition: Keeps users engaged

### Implementation
```csharp
IntervalMs: 1000-30000
TransitionDuration: 200-2000
```

### Consequences
- ✅ Reasonable range for most banners
- ✅ Prevents unusable carousels
- ⚠️ Cannot configure beyond bounds
- 📋 Future: Could make configurable

---

## ADR-009: Effect Parameter Validation Responsibility

**Status**: ✅ ACCEPTED

### Problem
Who validates effect parameters?
1. **EffectValidator only** — Central validation
2. **Effect constructor** — Validation on creation
3. **Shared** — Both constructor and service

### Decision
**EffectValidator only** — Centralized validation service.

### Rationale
- Single source of truth
- Easier to maintain
- Service can provide detailed error messages
- Decoupled from entity

### Implementation
- EffectValidator.ValidateXxx() methods
- Called in EffectService.ApplyEffect()
- Effect constructor trusts input (no validation)

### Consequences
- ✅ Centralized logic
- ✅ Reusable validation
- ✅ Better error messages
- ⚠️ Entity can be created with invalid data (if constructor bypassed)

---

## Decision Summary Table

| ADR | Decision | Status | Risk |
|-----|----------|--------|------|
| ADR-001 | JSON parameters | ✅ ACCEPTED | Low |
| ADR-002 | Immutable effects | ✅ ACCEPTED | Low |
| ADR-003 | Explicit ordering | ✅ ACCEPTED | Low |
| ADR-004 | Configurable autoplay | ✅ ACCEPTED | Low |
| ADR-005 | Fixed effect enum | ✅ ACCEPTED | Medium |
| ADR-006 | Multi-use components | ✅ ACCEPTED | Low |
| ADR-007 | App-layer validation | ✅ ACCEPTED | Low |
| ADR-008 | Reasonable bounds | ✅ ACCEPTED | Low |
| ADR-009 | Centralized validator | ✅ ACCEPTED | Low |

---

## Open Questions for Review

1. ✅ Should we support custom effect types? (No, fixed enum for now)
2. ✅ Can effects be applied to text components? (Yes, all types)
3. ✅ Can carousels contain same component twice? (Yes, via junction table)
4. ✅ Do we need effect priority/ordering? (No, simultaneous for Bolt 005)
5. ✅ Should effects compose (blur + opacity together)? (Yes, via Effects list)

---

## Next: Implementation (Stage 4)

Ready to implement based on these 9 architectural decisions.
