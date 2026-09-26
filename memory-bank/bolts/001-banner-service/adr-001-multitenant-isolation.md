---
bolt: 001-banner-service
created: 2026-09-26T00:00:00Z
status: accepted
---

# ADR-001: Multi-Tenant Data Isolation at Repository Layer

## Context

The Banner Service operates in a multi-tenant environment where each shop must be completely isolated from others. Data leakage is a critical risk that could expose one shop's banners to another.

Multiple architectural patterns exist for implementing multi-tenancy:

1. **Repository-layer filtering** (query-time): Repositories check ShopId in every query
2. **Database-level isolation**: Separate database per tenant (not practical for 1000+ shops)
3. **Row-level security (RLS)**: Database policy enforces filtering at SQL level
4. **Middleware + request context**: Middleware extracts ShopId and makes it available to repositories

The team must decide where to enforce the isolation constraint.

**Constraints**:
- Support 1000+ shops scalably
- Prevent accidental data leakage
- Maintain clear, testable code
- Align with team standards (clean architecture, dependency injection)

## Decision

**Enforce multi-tenant isolation at the repository layer via request-scoped ShopContext.**

All Banner and Component repository methods will:
1. Accept an implicit `ShopContext` (injected via DI)
2. Filter queries with `WHERE ShopId = context.ShopId`
3. Validate that operations belong to the current shop before allowing modifications

ShopId is extracted from JWT token claims by `ShopContextMiddleware` and registered as request-scoped in the DI container.

## Rationale

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| **Row-Level Security (SQL)** | Database enforces isolation; cannot be bypassed | Vendor-specific (MSSQL RLS); complex policy management; harder to test; MSSQL RLS requires SQL Server Enterprise in some versions; harder to debug queries | SQL-level isolation is opaque to application code and makes testing harder. Team prefers application-layer enforcement for clarity. |
| **Separate DB per Tenant** | Complete isolation; can use different schemas | Not scalable for 1000+ shops; operational nightmare (1000+ databases); database per tenant costs multiply; backup/restore complexity explodes | Cost and operational overhead make this impractical. |
| **Middleware + Repository Filter** | Application-layer enforcement is explicit and testable; DI makes it easy to inject; enables gradual enforcement | Risk: Developer could forget to filter or use wrong ShopId; requires discipline | This is the chosen approach. Risk mitigated via unit tests that validate ShopId filtering. |

**Why Repository Layer?**

- **Explicit**: Code shows `WHERE ShopId = context.ShopId` clearly
- **Testable**: Unit tests mock ShopContext and verify isolation
- **Debuggable**: Easy to step through and confirm filtering logic
- **Framework-agnostic**: Works with EF Core, Dapper, or raw SQL
- **Aligned with standards**: Clean architecture specifies infrastructure layer owns data access

**Why Request-Scoped Injection?**

- Prevents cross-request contamination (each request gets fresh context)
- DI container manages lifecycle (no manual cleanup needed)
- Middleware cleanly extracts ShopId from JWT; repositories don't know about HTTP details
- Standard .NET dependency injection pattern (everyone knows it)

## Consequences

### Positive

- ✅ **Explicit in code**: Every repository method shows the isolation logic clearly
- ✅ **Testable**: Unit tests can mock ShopContext and verify filtering
- ✅ **Scalable**: No need for separate databases; single schema supports 1000+ shops
- ✅ **Clear ownership**: Infrastructure layer owns isolation enforcement
- ✅ **Maintainable**: Future developers see `WHERE ShopId = context.ShopId` and understand intent
- ✅ **Centralized extraction**: Middleware extracts ShopId once per request; all repositories reuse it

### Negative

- ⚠️ **Developer discipline required**: A bug in one repository method breaks isolation for that entity
- ⚠️ **Not SQL-enforced**: Depends on application code, not database constraints
- ⚠️ **Overhead**: Every query includes ShopId filter (minor performance cost mitigated by indexes)

### Risks

- **Risk**: Developer forgets to add ShopId filter in new repository method
  - **Mitigation**: 
    1. Unit tests for all repository methods verify ShopId filtering
    2. Code review checklist includes "isolation verification"
    3. Constructor validates that ShopContext is injected (fail-fast if missing)
    4. Consider runtime assertion: `Debug.Assert(shopContext != null)` in production code

- **Risk**: ShopContext extracted incorrectly from JWT (picks wrong shop)
  - **Mitigation**: 
    1. Unit tests for `ShopContextMiddleware` verify correct extraction
    2. Integration tests verify end-to-end isolation
    3. Logging includes ShopId for audit trail

## Related

- **Stories**: 001-create-banner, 002-add-text-component, 003-add-image-component (all require isolation)
- **Standards**: Should be added to `standards/data-isolation-strategy.md` for future services
- **Previous ADRs**: None (this is foundational)
- **Dependencies**: ADR-001 constraints ADR-002 and ADR-003 (all data access flows through ShopId filtering)

---

## Implementation Notes

**In Repository Constructor**:
```csharp
public BannerRepository(ApplicationDbContext context, IShopContextAccessor shopContext)
{
    _context = context;
    _shopContext = shopContext ?? throw new ArgumentNullException(nameof(shopContext));
}
```

**In Every Query Method**:
```csharp
public async Task<Banner> GetByIdAsync(Guid bannerId)
{
    var shopId = _shopContext.ShopId;
    return await _context.Banners
        .Where(b => b.ShopId == shopId && b.Id == bannerId)
        .FirstOrDefaultAsync();
}
```

**In Middleware**:
```csharp
public async Task InvokeAsync(HttpContext context, IShopContextAccessor shopContextAccessor)
{
    var shopId = context.User.FindFirst("shop_id")?.Value;
    shopContextAccessor.SetShopId(Guid.Parse(shopId));
    await _next(context);
}
```

This decision ensures that Banner Service cannot accidentally leak data between shops, and future services can follow the same pattern.
