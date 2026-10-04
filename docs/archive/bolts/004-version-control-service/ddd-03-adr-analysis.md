# DDD-03: ADR Analysis — Version Control Service

**Status**: Stage 3 of 5 (ADR Analysis)  
**Bolt**: 004-version-control-service  
**Created**: 2026-09-26  

---

## ADR-001: JSON Snapshot vs Relational Normalization

**Status**: ✅ ACCEPTED

### Problem
Store banner state snapshots for version history. Two approaches:
1. **JSON Snapshot**: Store complete banner state as JSON blob
2. **Relational Normalization**: Create separate Version, VersionComponent tables

### Decision
**Store as JSON snapshot** in single SnapshotJson column.

### Rationale

| Aspect | JSON | Relational |
|--------|------|-----------|
| **Simplicity** | ✅ Simple schema | ❌ Complex with joins |
| **Query Speed** | ✅ Fast (single row) | ❌ Slow (4+ table joins) |
| **Data Consistency** | ✅ Atomic snapshots | ⚠️ Risk of partial snapshots |
| **Schema Changes** | ✅ No new tables needed | ❌ Schema grows with features |
| **Flexibility** | ✅ Easy to extend | ❌ Requires migrations |
| **Restore Speed** | ✅ O(1) deserialize | ❌ O(n) reconstruct |

### Implementation
- Snapshot stored as JSON string: `{"name":"...", "description":"...", "components":[...]}`
- Deserialized to `BannerSnapshot` value object on read
- EF Core handles serialization/deserialization

### Consequences
- ✅ No new schema complexity
- ✅ Version restore is O(1)
- ✅ Banner changes don't affect snapshots
- ⚠️ JSON size grows with number of components (max ~50KB per version)
- ⚠️ No direct SQL queries on component history (must deserialize)

### Alternatives Rejected
- Relational approach: Too complex, requires multiple migrations

---

## ADR-002: Automatic vs Triggered Versioning

**Status**: ✅ ACCEPTED

### Problem
When should version snapshots be created?
1. **Automatic**: Every time banner is saved
2. **Triggered**: Manually via API when user requests save
3. **Hybrid**: Auto with manual overrides

### Decision
**Automatic on every save**, with optional change description.

### Rationale
- Users don't have to remember to create versions
- Complete audit trail guaranteed
- No missed snapshots
- Change description is optional but encouraged

### Implementation
- `BannerService.UpdateBannerAsync()` calls `VersionControlService.CreateSnapshotAsync()`
- Change description passed from UpdateBannerDto
- Versions created synchronously before commit

### Consequences
- ✅ Complete history (no gaps)
- ✅ Simple flow (no user decision)
- ⚠️ Storage cost: Every save creates a snapshot (~50KB per version max)
- ⚠️ Quick saves can create many versions (user must manage via cleanup policy)

### Alternatives Rejected
- Triggered: Risk of missing snapshots if user forgets
- Hybrid: Adds complexity without benefit

---

## ADR-003: Sequential Version Numbers vs Timestamps

**Status**: ✅ ACCEPTED

### Problem
How to identify versions?
1. **Sequential Numbers**: V1, V2, V3 (per banner)
2. **Timestamps**: Uses CreatedAt
3. **UUIDs**: Globally unique IDs

### Decision
**Sequential numbers per banner** (VersionNumber column).

### Rationale

| Aspect | Sequential | Timestamp | UUID |
|--------|-----------|-----------|------|
| **Human Readable** | ✅ "Restore to V5" | ❌ Complex | ❌ Long IDs |
| **Uniqueness** | ✅ Per banner | ✅ Unique | ✅ Unique |
| **Ordering** | ✅ Natural | ✅ Temporal | ❌ No order |
| **URL Friendly** | ✅ `/versions/5` | ⚠️ `/versions/2026...` | ⚠️ Long |
| **Skipping Versions** | ✅ Easy (v5→v8) | ⚠️ Requires logic | ❌ Hard |

### Implementation
- `VersionNumber` column: INT, auto-incremented per (BannerId, ShopId)
- Unique constraint: `(BannerId, VersionNumber)`
- `GetNextVersionNumberAsync()` returns MAX(VersionNumber) + 1

### Consequences
- ✅ User-friendly ("go to version 3")
- ✅ Compact URLs (`/banners/123/versions/3`)
- ✅ Easy to understand restore flow
- ⚠️ Must track sequence per banner
- ⚠️ Cannot skip numbers (no gaps allowed)

### Alternatives Rejected
- Timestamps: Ugly URLs, complex comparison logic
- UUIDs: Not human-readable

---

## ADR-004: Immutable History (No Deletes)

**Status**: ✅ ACCEPTED

### Problem
Should version history be deletable or immutable?
1. **Immutable**: Versions cannot be deleted (audit trail)
2. **Soft Delete**: Mark as deleted, can be restored
3. **Hard Delete**: Remove from database

### Decision
**Immutable history** — versions cannot be deleted.

### Rationale
- Audit compliance: Cannot hide past states
- Prevents accidental loss of historical data
- Simplifies restore logic (version always exists)
- Aligns with GDPR requirements (audit trail)

### Implementation
- No delete methods on `IBannerVersionRepository`
- `IsActive` flag for future optimization (but not used for deletion)
- Archive policy (cleanup) is future scope

### Consequences
- ✅ Complete audit trail
- ✅ Never lose historical data
- ✅ Compliance-friendly
- ⚠️ Storage grows indefinitely
- ⚠️ Future need for cleanup/archive strategy

### Alternatives Rejected
- Soft delete: Adds complexity without benefit
- Hard delete: Loses audit trail

---

## ADR-005: Version Creation Timing

**Status**: ✅ ACCEPTED

### Problem
When in the update flow should snapshot be created?
1. **Before Update**: Snapshot old state, then update
2. **After Update**: Update, then snapshot new state
3. **Both**: Snapshot before and after

### Decision
**After update completes**, snapshot contains new state.

### Rationale
- New version represents final banner state
- Cleaner restore logic (snapshot state = restored state)
- Matches user expectation ("version contains what I saved")
- Simpler implementation (no state tracking)

### Implementation
1. Update banner in `BannerService.UpdateBannerAsync()`
2. Call `VersionControlService.CreateSnapshotAsync(banner)`
3. Snapshot is created with updated banner state
4. Commit transaction

### Consequences
- ✅ Restored state matches snapshot
- ✅ No intermediate states
- ✅ Clear semantics ("V3 = banner as of this update")
- ⚠️ Cannot restore to "state before update"

### Alternatives Rejected
- Before update: Would require storing old state, confusing semantics

---

## ADR-006: Multi-Tenant Isolation Strategy

**Status**: ✅ ACCEPTED (Consistent with ADR-001 from Banner Service)

### Problem
Ensure versions only accessible to authorized shop.

### Decision
**Filter by ShopId on all queries** (enforced at repository layer).

### Rationale
- Consistent with Banner Service approach
- Prevents cross-shop data leakage
- ShopId extracted from JWT claim in middleware

### Implementation
- `IBannerVersionRepository.GetVersionsAsync(bannerId, shopId)` filters by both
- All queries include: `WHERE ShopId = @shopId`
- Database index: `(BannerId, ShopId, VersionNumber DESC)`

### Consequences
- ✅ Secure multi-tenant isolation
- ✅ Consistent with rest of system
- ✅ No cross-shop leakage possible
- ✅ Scales with shops

---

## ADR-007: Service Boundary (Integrated vs Separate Microservice)

**Status**: ✅ ACCEPTED

### Problem
Should Version Control be:
1. **Integrated**: Part of Banner Service
2. **Separate Microservice**: Own service, own database

### Decision
**Integrated into Banner Service** for Bolt 004.

### Rationale
- Banner Service already persists banner data
- Version snapshots depend on banner state
- No external service needs version history yet
- Simpler deployment (one service)
- Can separate later if needed (future bolt)

### Implementation
- New repositories in same DbContext
- New controllers in Banner Service
- New application services
- Shared middleware and auth

### Consequences
- ✅ Simpler deployment
- ✅ Single transaction for banner + version
- ✅ Shared infrastructure (auth, logging, DB)
- ⚠️ Version Control cannot be scaled independently
- ⚠️ Future separation requires data migration
- 📋 Future: Can become separate microservice (Bolt X)

### Alternatives Rejected
- Separate microservice: Premature complexity, breaks transaction boundary

---

## ADR-008: Change Description Requirement

**Status**: ✅ ACCEPTED

### Problem
Should change description be mandatory?
1. **Required**: User must provide
2. **Optional**: Nice-to-have
3. **Auto-Generated**: Compute from diff

### Decision
**Optional but encouraged** — passed from UpdateBannerDto, defaults to null.

### Rationale
- Flexibility (don't force users to describe every edit)
- Audit trail still complete (timestamp, userId exist)
- Descriptions improve human understanding
- Can be mandatory in future via API validation

### Implementation
- `ChangeDescription` (string?, max 500 chars)
- Passed from `UpdateBannerDto.ChangeDescription`
- Not validated for presence (optional)
- Validated for length: `<= 500 chars`

### Consequences
- ✅ Flexible versioning
- ✅ Users can add context if helpful
- ✅ No forced workflow
- ⚠️ Some versions may have no description
- 📋 Future: Could require descriptions for major versions

---

## ADR-009: Storage Format for Snapshots

**Status**: ✅ ACCEPTED

### Problem
How to serialize `BannerSnapshot` to JSON?
1. **System.Text.Json**: Modern, built-in, fast
2. **Newtonsoft.Json**: Feature-rich, legacy
3. **Custom serializer**: Full control

### Decision
**System.Text.Json** (Utf8JsonWriter / JsonSerializer).

### Rationale
- Built into .NET 8
- No external dependency
- Fast and modern
- Works with EF Core value objects

### Implementation
- `BannerSnapshot` uses `[JsonSerializable]`
- EF Core Converter for column mapping
- Handles `ComponentSnapshot[]` serialization

### Consequences
- ✅ No additional NuGet dependencies
- ✅ Modern .NET standard
- ✅ Performance optimized
- ✅ Works with EF Core

---

## Decision Summary Table

| ADR | Decision | Status | Risk |
|-----|----------|--------|------|
| ADR-001 | JSON Snapshot | ✅ ACCEPTED | Low |
| ADR-002 | Automatic Versioning | ✅ ACCEPTED | Low |
| ADR-003 | Sequential Numbers | ✅ ACCEPTED | Low |
| ADR-004 | Immutable History | ✅ ACCEPTED | Low |
| ADR-005 | Post-Update Snapshot | ✅ ACCEPTED | Low |
| ADR-006 | ShopId Filtering | ✅ ACCEPTED | Low |
| ADR-007 | Integrated Service | ✅ ACCEPTED | Low |
| ADR-008 | Optional Descriptions | ✅ ACCEPTED | Low |
| ADR-009 | System.Text.Json | ✅ ACCEPTED | Low |

---

## Open Questions for Review

1. ✅ Should we auto-cleanup old versions? (Scope: Future bolt)
2. ✅ Should restore create a new version? (Yes, with "Restored to V{N}" description)
3. ✅ Should we track version diffs? (Scope: Future enhancement)
4. ✅ Should components be individually versionable? (No, banner-level snapshots only)

---

## Next: Implementation (Stage 4)

Ready to implement based on these decisions.
