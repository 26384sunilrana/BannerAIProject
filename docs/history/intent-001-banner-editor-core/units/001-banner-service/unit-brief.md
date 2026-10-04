---
unit: 001-banner-service
intent: 001-banner-editor-core
phase: inception
status: draft
created: 2026-09-26T00:00:00Z
---

# Unit Brief: Banner Service

## Purpose

Core ASP.NET Core microservice managing banner lifecycle - CRUD operations, component management, multi-tenant data isolation, and banner preview functionality.

## Scope

### In Scope
- Banner CRUD (create, read, update, delete)
- Component management (add, edit, remove components)
- Component properties (position, size, color, effects reference)
- Component layering (z-index management)
- Banner preview configuration
- Multi-tenant data isolation (shop-level segregation)
- REST API endpoints for all operations

### Out of Scope
- Version history (Unit 002)
- Visual effects definitions (Unit 003)
- Media file storage (Unit 004)
- User interface (Unit 005)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-2 | Component system (text, images, videos, graphics) | Must |
| FR-3 | Component layering (z-index) | Must |
| FR-8 | Preview functionality | Must |
| FR-10 | Multi-user access with data isolation | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description |
|--------|-------------|
| Banner | Aggregate root - represents a digital banner with metadata and components |
| Component | Value object - represents a single element (text/image/video/graphics) on banner |
| Shop | Aggregate root for data isolation context |

### Key Operations
| Operation | Description |
|-----------|-------------|
| CreateBanner | Create new banner for shop |
| AddComponent | Add component to banner |
| UpdateComponent | Modify component properties |
| RemoveComponent | Delete component from banner |
| GetBannerPreview | Retrieve banner configuration for preview |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 8 |
| Must Have | 8 |

### Stories (Summary)

- S1: Create new banner
- S2: Add text component
- S3: Add image component
- S4: Add video component
- S5: Add graphics component
- S6: Update component properties
- S7: Manage component z-index
- S8: Preview banner configuration

---

## Dependencies

### Depends On
- None (foundation unit)

### Depended By
- Unit 002: Version Control Service
- Unit 003: Effects Engine
- Unit 004: Media Service
- Unit 005: Banner Editor UI

---

## Technical Context

### Technology
- Language: C# (.NET 8+)
- Framework: ASP.NET Core Web API
- ORM: Entity Framework Core
- Database: MSSQL
- Patterns: Repository + Unit of Work + AutoMapper

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Banner Editor UI | API | REST/JSON |
| Version Control Service | Internal API | REST |
| Effects Engine | Internal API | REST |
| Media Service | Internal API | REST |

### Data Storage
| Data | Type | Volume | Retention |
|------|------|--------|-----------|
| Banners | SQL | 1000s per shop | Indefinite |
| Components | SQL | 50+ per banner | Indefinite |
| Shop context | SQL | Indexed | Indefinite |

---

## Constraints

- Multi-tenant data isolation is CRITICAL - all queries must filter by shop_id
- Component limit: 50 per banner (performance)
- No cross-shop data leakage allowed
- Scalable to 1000+ shops

---

## Success Criteria

### Functional
- [x] Create, read, update, delete banners
- [x] Add and manage components
- [x] Component z-index properly applied
- [x] Preview returns complete banner configuration
- [x] Multi-user access doesn't leak data

### Non-Functional
- [x] Response time < 200ms
- [x] Support 50+ components
- [x] Query optimization for large banner lists
- [x] Data isolation verified through tests

---

## Bolt Plan

| Bolt | Stories | Focus |
|------|---------|-------|
| Bolt-001 | S1, S2, S3 | Banner CRUD + Text/Image components |
| Bolt-002 | S4, S5, S6 | Video/Graphics components + property updates |
| Bolt-003 | S7, S8 | Z-index management + preview API |

---

## Notes

- Use DB indexes on (ShopId, BannerId) for fast queries
- Implement request-scoped ShopId validation middleware
- Return DTOs not domain entities in API responses
- Test data isolation extensively
