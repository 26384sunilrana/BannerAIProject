---
unit: 002-version-control-service
intent: 001-banner-editor-core
phase: inception
status: draft
created: 2026-09-26T00:00:00Z
---

# Unit Brief: Version Control Service

## Purpose

ASP.NET Core microservice managing banner version history - automatic snapshots, version retrieval, and rollback to previous versions. Maintains up to 10 historical versions per banner.

## Scope

### In Scope
- Create version snapshots on banner save
- List version history for a banner
- Retrieve specific version details
- Restore/rollback to previous version
- Version metadata (timestamp, user, description)
- Enforce 10-version limit (oldest discarded)

### Out of Scope
- Banner editing (Unit 001)
- Visual effects (Unit 003)
- Media storage (Unit 004)
- UI (Unit 005)

---

## Assigned Requirements

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-7 | Version history (10 versions) and rollback | Must |

---

## Domain Concepts

### Key Entities
| Entity | Description |
|--------|-------------|
| BannerVersion | Aggregate root - snapshot of banner at point in time |
| VersionSnapshot | Value object - complete banner configuration at version |

### Key Operations
| Operation | Description |
|-----------|-------------|
| CreateVersion | Create new version snapshot after banner save |
| ListVersions | Get all versions for a banner |
| GetVersion | Retrieve specific version details |
| RestoreVersion | Restore to previous version |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 3 |
| Must Have | 3 |

### Stories (Summary)

- S1: Auto-create version on banner save
- S2: List and view version history
- S3: Restore banner to previous version

---

## Dependencies

### Depends On
- Unit 001: Banner Service (to get BannerId context)

### Depended By
- Unit 005: Banner Editor UI (restore functionality)

---

## Technical Context

### Technology
- Language: C# (.NET 8+)
- Framework: ASP.NET Core Web API
- ORM: Entity Framework Core
- Database: MSSQL
- Storage: BLOB/Serialized JSON

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Banner Service | Internal API | REST |
| Banner Editor UI | API | REST/JSON |

### Data Storage
| Data | Type | Volume | Retention |
|------|------|--------|-----------|
| Version snapshots | SQL BLOB | 10 × ~1MB per banner | Indefinite |
| Version metadata | SQL | 10 per banner | Indefinite |

---

## Constraints

- 10-version limit per banner (design for easy expansion)
- Version snapshots stored as serialized JSON
- Automatic deletion of oldest version when 11th created
- Response time < 500ms for restore

---

## Success Criteria

### Functional
- [x] Version created on save
- [x] 10-version limit enforced
- [x] Restore creates new version entry
- [x] Metadata tracked (timestamp, user)

### Non-Functional
- [x] Snapshot storage efficient
- [x] Restore < 500ms
- [x] No data loss on version churn

---

## Bolt Plan

| Bolt | Stories | Focus |
|------|---------|-------|
| Bolt-002 | S1, S2, S3 | Complete version control |

---

## Notes

- Design versioning as event-based (banner saved → version created)
- Consider event sourcing for audit trail
- Test version limit enforcement thoroughly
