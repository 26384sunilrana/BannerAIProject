---
id: 003-banner-service
unit: 001-banner-service
intent: 001-banner-editor-core
type: ddd-construction-bolt
status: planned
stories: [007-manage-component-z-index, 008-preview-banner-configuration]
created: 2026-09-26T00:00:00Z

requires_bolts: [002-banner-service]
enables_bolts: []
requires_units: []
blocks: false

complexity:
  avg_complexity: 1
  avg_uncertainty: 1
  max_dependencies: 1
  testing_scope: 1
---

# Bolt: 003-banner-service

## Objective

Complete Banner Service with component layering (z-index management) and preview functionality. Final foundational work before moving to dependent services (Version Control, Effects, Media).

## Stories Included

- [ ] **007-manage-component-z-index**: Control component layering - bring to front, send to back, reorder layers
  - Priority: Must
  - Acceptance: Z-index properly applied, layer order persisted, API returns correct order

- [ ] **008-preview-banner-configuration**: Get complete banner configuration for preview rendering
  - Priority: Must
  - Acceptance: Preview API returns all components with full properties, positioned correctly

## Expected Outputs (DDD Stages)

- `ddd-01-domain-model.md` - Component ordering, preview model
- `ddd-02-technical-design.md` - Z-index storage and querying, preview API contract
- `ddd-03-test-report.md` - Test coverage for ordering and preview
- Implementation code - Z-index management, preview endpoint
- Test suite - Unit and integration tests

## Dependencies

### Bolt Dependencies (within intent)
- **002-banner-service** (Required): Completed
  - Needs all component types and properties

### Unit Dependencies (cross-intent)
- None

### Enables (other bolts waiting on this)
- **002-version-control-service**: Needs banner preview model for snapshots
- **003-effects-engine**: Needs component structure with z-index
- **005-banner-editor-ui**: Needs preview API and z-index management

## Domain Focus

### Key Concepts
- **Component Ordering**
  - Z-index storage and retrieval
  - Reordering operations (BringToFront, SendToBack, MoveUp, MoveDown)
  - Index normalization

- **Preview Model**
  - Complete banner state (components, properties, order)
  - Rendering-ready format (JSON with all properties)

### Key Operations
- BringToFront(bannerId, componentId) → Updated order
- SendToBack(bannerId, componentId) → Updated order
- ReorderComponents(bannerId, newOrder) → Updated components
- GetBannerPreview(bannerId) → Complete preview configuration

## Success Criteria

### Functional
- [x] Z-index reordering works correctly
- [x] Layer order persisted and retrieved
- [x] Preview returns complete banner state
- [x] All components appear in correct order

### Non-Functional
- [x] Response time < 200ms for preview
- [x] Z-index operations fast (< 100ms)
- [x] Efficient database queries
- [x] Test coverage > 80%

### Technical
- [x] Z-index management clean design
- [x] Preview DTO comprehensive but efficient
- [x] No N+1 queries in preview
- [x] Normalization of indices after reordering

## Notes

- Z-index stored as decimal (e.g., 1.0, 2.0) for easy insertion between existing values
- Preview should include all component properties needed for rendering
- Consider pagination if preview becomes large (future optimization)
- Reordering should be atomic transaction

## Estimated Duration

**2-3 days** (straightforward operations, lower complexity)
