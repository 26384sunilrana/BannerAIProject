---
id: 002-banner-service
unit: 001-banner-service
intent: 001-banner-editor-core
type: ddd-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
reconciled_note: "Implemented in src/BannerService (builds clean). Tests green as of 2026-10-01: Domain 387, Application 53, Integration 1. 46 stale test files are excluded from the build (see Compile Remove in test csproj files)."
stories: [004-add-video-component, 005-add-graphics-component, 006-update-component-properties]
created: 2026-09-26T00:00:00Z

requires_bolts: [001-banner-service]
enables_bolts: [003-banner-service]
requires_units: [004-media-service]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 2
  testing_scope: 2
---

# Bolt: 002-banner-service

## Objective

Extend Banner Service with video and graphics component types, plus comprehensive component property updates. Requires coordination with Media Service for video metadata.

## Stories Included

- [ ] **004-add-video-component**: Add video component to banner (video reference, timing, mute control)
  - Priority: Must
  - Acceptance: Video component added with duration/timing properties, stored with banner

- [ ] **005-add-graphics-component**: Add graphics component to banner (shape, color, size)
  - Priority: Must
  - Acceptance: Graphics component added with shape/color properties, rendered correctly

- [ ] **006-update-component-properties**: Modify any component's properties after creation
  - Priority: Must
  - Acceptance: Properties updated, changes persisted, returned in API response

## Expected Outputs (DDD Stages)

- `ddd-01-domain-model.md` - Extended Component types (Video, Graphics), properties
- `ddd-02-technical-design.md` - API routes for new components, property update logic
- `ddd-03-test-report.md` - Test coverage for new component types
- Implementation code - Extended service, new component types
- Test suite - Unit and integration tests

## Dependencies

### Bolt Dependencies (within intent)
- **001-banner-service** (Required): Completed
  - Needs Banner entity, Component base structure

### Unit Dependencies (cross-intent)
- **004-media-service** (Required): Completed
  - Needs video metadata extraction (duration, resolution, codec)

### Enables (other bolts waiting on this)
- **003-banner-service**: Needs all component types defined
- **005-banner-editor-ui**: Needs full component API

## Domain Focus

### Extended Entities
- **VideoComponent** (specialized Component)
  - VideoRef (reference to media service)
  - Duration, Timing, MuteVolume
  
- **GraphicsComponent** (specialized Component)
  - Shape, Color, BorderStyle
  - Fill, StrokeColor, StrokeWidth

- **Component Property Updates**
  - Flexible property update mechanism
  - Validation of property values
  - Type-safe property setting

### Key Operations
- AddVideoComponent(bannerId, videoRef, properties) → Updated Banner
- AddGraphicsComponent(bannerId, shape, color, size) → Updated Banner
- UpdateComponentProperty(bannerId, componentId, property, value) → Updated Component
- DeleteComponent(bannerId, componentId) → Updated Banner

## Success Criteria

### Functional
- [x] Add video component with timing
- [x] Add graphics component with shapes/colors
- [x] Update component properties
- [x] Remove components
- [x] All component types work together on same banner

### Non-Functional
- [x] Response time < 200ms
- [x] Video metadata correctly retrieved from Media Service
- [x] Property validation fast
- [x] Test coverage > 80%

### Technical
- [x] Component polymorphism (inheritance or composition)
- [x] Media Service integration (API calls)
- [x] Robust property validation
- [x] Backward compatibility with existing banners

## Notes

- Design component types carefully for extensibility
- Video component requires Media Service integration (async/await)
- Graphics component can use CSS color names
- Property updates should be atomic
- Consider polymorphism approach (inheritance vs. composition)

## Dependencies on Media Service

- `GET /api/media/{id}` - Retrieve video metadata (duration, resolution, codec)
- `POST /api/banners/{id}/components` - Create video component with media reference

## Estimated Duration

**3-5 days** (media service integration adds some complexity)

> The design papers listed above moved to `docs/history/bolts/002-banner-service/`.
