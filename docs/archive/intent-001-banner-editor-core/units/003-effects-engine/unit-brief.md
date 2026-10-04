---
unit: 003-effects-engine
intent: 001-banner-editor-core
phase: inception
status: draft
created: 2026-09-26T00:00:00Z
---

# Unit Brief: Effects Engine

## Purpose

ASP.NET Core microservice managing visual effects and carousel/rotation functionality. Defines available effects, validates parameters, and stores effect configurations for components.

## Scope

### In Scope
- Effect library (opacity, rotation, scale, blur, animation, etc.)
- Apply effects to components
- Carousel/rotation configuration
- Effect parameter validation
- Effect metadata and documentation

### Out of Scope
- Component management (Unit 001)
- Video handling (Unit 004)
- Rendering/preview (Unit 005 frontend)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-4 | Visual effects | Must |
| FR-5 | Carousel/rotation | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description |
|--------|-------------|
| Effect | Effect definition with parameters |
| EffectParameter | Typed parameter for effect |
| Carousel | Multi-item rotation configuration |

### Key Operations
| Operation | Description |
|-----------|-------------|
| GetEffectLibrary | List all available effects |
| ApplyEffect | Add effect to component |
| ValidateEffect | Validate effect parameters |
| ConfigureCarousel | Setup carousel rotation |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 4 |
| Must Have | 4 |

### Stories (Summary)

- S1: Define effect library and types
- S2: Apply effects to components
- S3: Configure carousel rotation
- S4: Validate effect parameters

---

## Dependencies

### Depends On
- Unit 001: Banner Service (component context)

### Depended By
- Unit 005: Banner Editor UI (effects selector UI)

---

## Technical Context

### Technology
- Language: C# (.NET 8+)
- Framework: ASP.NET Core Web API
- Storage: MSSQL + Configuration

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Banner Service | Internal API | REST |
| Banner Editor UI | API | REST/JSON |

### Data Storage
| Data | Type | Volume | Retention |
|------|------|--------|-----------|
| Effect definitions | Config DB | Statically defined | Indefinite |
| Component effects | SQL | 1-5 per component | Per banner |
| Carousel configs | SQL | 1 per component | Per banner |

---

## Constraints

- Effect parameters must be validated server-side
- Carousel timing flexible (seconds or per-video duration)
- Support complex effect chains
- Performance: Real-time preview responsiveness

---

## Success Criteria

### Functional
- [x] Effect library accessible
- [x] Effects apply and persist
- [x] Carousel rotation configurable
- [x] Parameters validated

### Non-Functional
- [x] Effect library retrieved < 100ms
- [x] Parameter validation instant

---

## Bolt Plan

| Bolt | Stories | Focus |
|------|---------|-------|
| Bolt-003 | S1, S2 | Effects configuration |
| Bolt-003b | S3, S4 | Carousel + validation |

---

## Notes

- Use enum for effect types
- Store effect configs as JSON in MSSQL
- Design for easy effect library extension
- Consider video duration integration with carousel timing
