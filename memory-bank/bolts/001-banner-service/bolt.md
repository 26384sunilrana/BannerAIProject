---
id: 001-banner-service
unit: 001-banner-service
intent: 001-banner-editor-core
type: ddd-construction-bolt
status: complete
started: 2026-09-26T00:00:00Z
completed: 2026-09-26T00:00:00Z
current_stage: null
stages_completed:
  - name: domain-model
    completed: 2026-09-26T00:00:00Z
    artifact: ddd-01-domain-model.md
  - name: technical-design
    completed: 2026-09-26T00:00:00Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-09-26T00:00:00Z
    artifact: adr-001-multitenant-isolation.md, adr-002-json-component-properties.md, adr-003-zindex-layering.md
  - name: implement
    completed: 2026-09-26T00:00:00Z
    artifact: source code in src/BannerService/
  - name: test
    completed: 2026-09-26T00:00:00Z
    artifact: ddd-03-test-report.md
stories: [001-create-banner, 002-add-text-component, 003-add-image-component]
created: 2026-09-26T00:00:00Z

requires_bolts: []
enables_bolts: [002-banner-service, 003-banner-service]
requires_units: []
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 2
---

# Bolt: 001-banner-service

## Objective

Establish the foundation of the Banner Service: core domain model, banner CRUD operations, and text/image component management. This bolt defines the core entities and API endpoints needed by downstream work.

## Stories Included

- [ ] **001-create-banner**: Create new banner with metadata (name, description, dimensions)
  - Priority: Must
  - Acceptance: Banner created, assigned to shop, persisted in MSSQL

- [ ] **002-add-text-component**: Add text component to banner (content, font, size, color)
  - Priority: Must
  - Acceptance: Text component added, positioned on canvas, stored with banner

- [ ] **003-add-image-component**: Add image component to banner (reference, size, position)
  - Priority: Must
  - Acceptance: Image component added, sized, positioned, stored with banner

## Expected Outputs (DDD Stages)

- `ddd-01-domain-model.md` - Banner and Component entities, value objects
- `ddd-02-technical-design.md` - Database schema, API routes, repository interfaces
- `ddd-03-test-report.md` - Test strategy, coverage metrics
- Implementation code - Banner service, repositories, DTOs, API controllers
- Test suite - Unit tests, integration tests

## Dependencies

### Bolt Dependencies (within intent)
- None - this is the foundation bolt

### Unit Dependencies (cross-intent)
- None - standalone backend service

### Enables (other bolts waiting on this)
- **002-banner-service**: Needs Banner entity and repository
- **003-banner-service**: Needs Banner and Component entities
- **005-banner-editor-ui**: Needs Banner CRUD API endpoints

## Domain Focus

### Key Entities
- **Banner** (aggregate root)
  - BannerId, ShopId, UserId
  - Name, Description, Dimensions
  - CreatedAt, UpdatedAt
  - Components collection (initially empty)

- **Component** (value object)
  - ComponentId, ComponentType (text/image)
  - Position (x, y), Size (width, height)
  - Properties (color for text, reference for image)

### Key Operations
- CreateBanner(shopId, metadata) → Banner
- AddComponent(bannerId, component) → Updated Banner
- UpdateComponent(bannerId, componentId, properties) → Updated Component
- RemoveComponent(bannerId, componentId) → Updated Banner

## Success Criteria

### Functional
- [x] Create banner with metadata
- [x] Add text component (content, font, color)
- [x] Add image component (image reference, size)
- [x] Components persist with banner
- [x] Data isolation: Only shop owner can modify

### Non-Functional
- [x] Response time < 200ms for CRUD
- [x] Support 50+ components per banner
- [x] Database queries optimized with indexes
- [x] Code coverage > 80%

### Technical
- [x] Repository pattern with Unit of Work
- [x] AutoMapper for DTO conversion
- [x] Entity Framework with proper navigation
- [x] Multi-tenant query filters

## Notes

- Start simple: Text and Image only (no video/graphics in this bolt)
- Use clean entity design for extension (next bolts add Video, Graphics)
- Implement query filters for shop_id from the start (data isolation critical)
- Design Components as value objects initially (may become entities later)
- Database indexing: (ShopId, BannerId) for fast queries

## Estimated Duration

**3-5 days** for full DDD cycle (domain modeling, technical design, implementation, testing)
