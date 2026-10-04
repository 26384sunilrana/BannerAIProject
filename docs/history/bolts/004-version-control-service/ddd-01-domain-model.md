# DDD-01: Domain Model — Version Control Service

**Status**: Stage 1 of 5 (Domain Model)  
**Bolt**: 004-version-control-service  
**Created**: 2026-09-26  

---

## Problem Statement

Banner Service persists banner state but has no version history. Users need to:
1. **Automatically capture** banner state when saved
2. **View history** of all versions with metadata
3. **Restore** banner to any previous version

Currently, once a banner is edited, previous state is lost. This service adds version tracking.

---

## Core Domain

### Aggregate: BannerVersion

Root entity representing an immutable snapshot of banner state at a specific point in time.

**Aggregate Root**: `BannerVersion`
- **Identity**: `Id` (Guid) - unique version identifier
- **Tenant**: `ShopId` (Guid) - multi-tenant isolation
- **Reference**: `BannerId` (Guid) - which banner this version belongs to
- **Versioning**: `VersionNumber` (int) - sequential, auto-incremented per banner (1, 2, 3, ...)
- **Timeline**:
  - `CreatedAt` (DateTime) - when snapshot was created (UTC)
  - `CreatedBy` (Guid) - user ID who triggered the save
- **Snapshot**: `BannerSnapshot` (value object) - immutable copy of banner state
- **Metadata**: `ChangeDescription` (string, 1-500 chars) - human-readable change summary
- **State**: `IsActive` (bool) - whether this version is the current banner state

**Constraints**:
- VersionNumber must be unique per (BannerId, ShopId)
- VersionNumber sequence cannot have gaps
- CreatedAt must be UTC
- ChangeDescription is optional but recommended
- Only one version per banner can have IsActive = true

**Invariants**:
- Cannot delete versions (immutable history)
- Cannot modify snapshot content after creation
- BannerId and ShopId are immutable

### Value Object: BannerSnapshot

Immutable capture of banner state at a point in time.

**Fields**:
- `Name` (string, 1-100 chars) - banner name
- `Description` (string, 0-500 chars) - banner description
- `Width` (int, 100-4000px) - banner width
- `Height` (int, 100-2000px) - banner height
- `Components` (List<ComponentSnapshot>) - all components in banner at this version
- `CapturedAt` (DateTime) - when snapshot was taken (UTC)

**Behavior**:
- Immutable after creation
- Equality based on content (value object)
- Serializable to JSON for storage

### Value Object: ComponentSnapshot

Immutable capture of a single component in the banner snapshot.

**Fields**:
- `ComponentId` (Guid) - original component ID
- `ComponentType` (int) - 1=Text, 2=Image, 3=Video, 4=Graphics
- `PositionX` (int), `PositionY` (int) - position
- `SizeWidth` (int), `SizeHeight` (int) - size
- `ZIndex` (int, 0-100) - layer order
- `PropertiesJson` (string) - type-specific properties as JSON

**Behavior**:
- Immutable
- Equality based on ComponentId and content

---

## Domain Services

### VersionControlService

Orchestrates version snapshot creation, listing, and restoration.

**Operations**:

#### CreateSnapshot(banner: Banner, changeDescription: string) → BannerVersion

**Purpose**: Capture current banner state as a new version.

**Inputs**:
- `banner` - current Banner aggregate from Banner Service
- `changeDescription` - optional description of what changed (e.g., "Added product image", "Updated text color")

**Process**:
1. Validate banner is not null
2. Validate changeDescription length (max 500 chars)
3. Get next VersionNumber for this banner (MAX(VersionNumber) + 1)
4. Create ComponentSnapshots for each component in banner
5. Create BannerSnapshot from banner state
6. Create BannerVersion with:
   - New GUID Id
   - VersionNumber from step 3
   - BannerSnapshot from step 5
   - Current timestamp as CreatedAt
   - Current user ID as CreatedBy
   - ChangeDescription
   - IsActive = true (marks as current)
7. Return BannerVersion

**Errors**:
- Throws if banner is null
- Throws if ChangeDescription > 500 chars

---

#### ListVersions(bannerId: Guid, shopId: Guid) → List<BannerVersionDto>

**Purpose**: Get all versions of a banner with metadata (without full snapshots).

**Inputs**:
- `bannerId` - which banner
- `shopId` - tenant isolation

**Returns** (ordered by VersionNumber descending):
```
{
  "versionNumber": 3,
  "createdAt": "2026-09-26T10:30:00Z",
  "createdBy": "user-id",
  "changeDescription": "Updated background color",
  "isActive": true,
  "components": 5,
  "width": 1200,
  "height": 600
}
```

**Process**:
1. Query all BannerVersions for (bannerId, shopId)
2. Project to DTO (exclude full snapshot)
3. Sort by VersionNumber descending
4. Return list

**Constraints**:
- Must filter by ShopId (multi-tenant isolation)
- Must be authorized for this banner

---

#### RestoreVersion(bannerId: Guid, versionNumber: int, shopId: Guid, userId: Guid) → Banner

**Purpose**: Restore banner to state of a previous version.

**Inputs**:
- `bannerId` - which banner
- `versionNumber` - which version (1-N)
- `shopId` - tenant isolation
- `userId` - who initiated restore

**Process**:
1. Validate versionNumber > 0
2. Query BannerVersion for (bannerId, versionNumber, shopId)
3. Throw 404 if not found
4. Extract BannerSnapshot
5. Update current banner aggregate with snapshot state:
   - Name, Description, Width, Height
   - Clear all components
   - Add components from snapshot
6. Create new version snapshot with:
   - ChangeDescription: "Restored to version {versionNumber}"
   - CreatedBy: userId
7. Save both updated banner and new restoration snapshot
8. Return updated banner

**Errors**:
- 404 if version not found
- 400 if versionNumber ≤ 0
- 401 if user not authorized

---

## Repository Interface

### IBannerVersionRepository

```csharp
Task<BannerVersion> SaveAsync(BannerVersion version);
Task<List<BannerVersion>> GetVersionsAsync(Guid bannerId, Guid shopId);
Task<BannerVersion?> GetVersionAsync(Guid bannerId, int versionNumber, Guid shopId);
Task<int> GetNextVersionNumberAsync(Guid bannerId, Guid shopId);
```

**Invariants**:
- All queries must filter by ShopId (multi-tenant)
- GetVersionAsync returns null if not found (no throw)
- SaveAsync enforces VersionNumber uniqueness per banner

---

## Data Structure

### BannerVersion Table

```
Columns:
- Id (GUID, PK)
- BannerId (GUID, FK → Banners)
- ShopId (GUID, index with BannerId for queries)
- VersionNumber (int, unique constraint with BannerId)
- SnapshotJson (NVARCHAR(MAX), serialized BannerSnapshot)
- ChangeDescription (NVARCHAR(500), nullable)
- CreatedAt (DATETIME2, UTC)
- CreatedBy (GUID)
- IsActive (BIT, default 1)

Indexes:
- PK: (Id)
- Unique: (BannerId, VersionNumber)
- Query: (BannerId, ShopId, VersionNumber DESC)
```

---

## Stories Mapping

### S9: Create Version Snapshot
- Service: VersionControlService.CreateSnapshot()
- Trigger: When BannerService.UpdateBannerAsync() completes
- Result: New BannerVersion persisted with snapshot

### S10: List Banner Versions
- Service: VersionControlService.ListVersions()
- API: GET /api/banners/{bannerId}/versions
- Result: List of versions with metadata

### S11: Restore Version
- Service: VersionControlService.RestoreVersion()
- API: POST /api/banners/{bannerId}/versions/{versionNumber}/restore
- Result: Banner state restored, new snapshot created

---

## Design Principles

✅ **Multi-tenant Isolation**: ShopId on all queries  
✅ **Immutability**: Snapshots cannot be modified  
✅ **Event-driven**: Snapshot created on banner save  
✅ **Audit Trail**: CreatedBy and CreatedAt tracked  
✅ **Backward Compatibility**: Doesn't modify Banner Service  
✅ **JSON Storage**: Reuses PropertiesJson pattern  

---

## Decisions for ADR

1. **Store as JSON snapshot vs relational**: Choose JSON to avoid schema complexity
2. **Automatic vs manual versioning**: Choose automatic on save
3. **Version numbers vs timestamps**: Choose sequential numbers for simplicity
4. **Soft delete vs hard delete**: Choose immutable (no delete)
5. **When to prune old versions**: Out of scope for Bolt 004

---

## Next: Technical Design (Stage 2)

Once domain model is approved, proceed to:
- Database schema design
- API endpoint design
- DTOs and mappers
- Service integration with Banner Service
