---
last_updated: 2026-09-26T00:00:00Z
total_decisions: 3
---

# Decision Index

This index tracks all Architecture Decision Records (ADRs) created during Construction bolts.
Use this to find relevant prior decisions when working on related features.

## How to Use

**For Agents**: Scan the "Read when" fields below to identify decisions relevant to your current task. Before implementing new features, check if existing ADRs constrain or guide your approach. Load the full ADR for matching entries.

**For Humans**: Browse decisions chronologically or search for keywords. Each entry links to the full ADR with complete context, alternatives considered, and consequences.

---

## Decisions

### ADR-001: Multi-Tenant Data Isolation at Repository Layer
- **Status**: accepted
- **Date**: 2026-09-26
- **Bolt**: 001-banner-service (Banner Service)
- **Path**: `bolts/001-banner-service/adr-001-multitenant-isolation.md`
- **Summary**: The Banner Service operates in a multi-tenant environment where each shop must be isolated. We enforce isolation at the repository layer via request-scoped ShopContext and query-time filtering.
- **Read when**: Building any service that operates in multi-tenant environment; designing data access layers; working on user/tenant authentication flows; implementing queries that must filter by shop/tenant context

### ADR-002: JSON Storage for Polymorphic Component Properties
- **Status**: accepted
- **Date**: 2026-09-26
- **Bolt**: 001-banner-service (Banner Service)
- **Path**: `bolts/001-banner-service/adr-002-json-component-properties.md`
- **Summary**: Components on a banner have different properties based on type (Text, Image, Video, Graphics). We store component-specific properties as JSON in a single column, with type safety enforced at the application layer.
- **Read when**: Working on polymorphic data structures; implementing component systems with variable properties; adding new component types; designing extensible schemas; storing heterogeneous data in relational databases

### ADR-003: Component Layering (ZIndex) Design
- **Status**: accepted
- **Date**: 2026-09-26
- **Bolt**: 001-banner-service (Banner Service)
- **Path**: `bolts/001-banner-service/adr-003-zindex-layering.md`
- **Summary**: Components must be layered on a banner with rendering order determined by ZIndex. We use integer ZIndex (0-100) with uniqueness enforced at the application layer and database unique index for data integrity.
- **Read when**: Implementing component layering or depth management; designing preview/rendering systems; working on effects that depend on component order; managing z-order in visual composition tools
