---
intent: 001-banner-editor-core
phase: inception
status: units-defined
updated: 2026-09-26T00:00:00Z
---

# Units: Banner Editor Core

## Overview

The Banner Editor Core intent is decomposed into **5 independent units**:
- **4 Backend Service Units** (ASP.NET Core microservices, domain-driven)
- **1 Frontend Unit** (Next.js drag-drop editor)

Each unit is independently buildable, testable, and deployable. Backend units communicate via REST APIs. The frontend consumes all backend APIs.

---

## Requirement-to-Unit Mapping

| FR | Requirement | Assigned Unit |
|----|-------------|---------------|
| FR-1 | Drag-and-drop canvas | Banner Editor UI |
| FR-2 | Component system (text, images, videos, graphics) | Banner Service |
| FR-3 | Component layering (z-index) | Banner Service |
| FR-4 | Visual effects | Effects Engine |
| FR-5 | Carousel/rotation | Effects Engine |
| FR-6 | Video handling (4K/8K, timing, mute) | Media Service |
| FR-7 | Version history (10 versions, rollback) | Version Control Service |
| FR-8 | Preview functionality | Banner Service |
| FR-9 | No-code deployment (all changes via UI) | Banner Service + Effects Engine + Media Service |
| FR-10 | Multi-user access with data isolation | Banner Service (enforced at API level) |

---

## Units Breakdown

### Unit 1: Banner Service

**Identifier**: `001-banner-service`

**Purpose**: Core banner management - CRUD operations, component management, preview, multi-tenant data isolation

**Responsibility**:
- Create, read, update, delete banners
- Manage banner metadata (name, description, dimensions)
- Add/edit/remove components (text, images, graphics, videos)
- Component positioning and sizing
- Multi-user access with strict shop-level data isolation
- Preview banner configuration
- API endpoints for all banner operations

**Assigned Requirements**: FR-2, FR-3, FR-8, FR-10

**Key Entities**:
- Banner (aggregate root)
  - BannerId, ShopId, UserId
  - Metadata (name, description, dimensions, created_at, updated_at)
  - Components collection
  
- Component (value object within Banner)
  - ComponentId, ComponentType (text/image/graphics/video)
  - Properties (color, size, position, z-index)
  - Content (text content or media reference)

- Shop (for data isolation context)
  - ShopId, ShopName
  - Banners collection

**Dependencies**: None (foundation unit)

**Interfaces**:
- POST /api/banners → Create banner
- GET /api/banners/{id} → Get banner
- PUT /api/banners/{id} → Update banner
- DELETE /api/banners/{id} → Delete banner
- POST /api/banners/{id}/components → Add component
- PUT /api/banners/{id}/components/{componentId} → Update component
- DELETE /api/banners/{id}/components/{componentId} → Remove component
- GET /api/banners/{id}/preview → Preview configuration

**NFR Targets**:
- Response time < 200ms for typical operations
- Support 50+ components per banner
- Data isolation: Query filters ensure shop_id context
- Performance: Optimize component queries

**Technical Constraints**:
- MSSQL with proper indexing on shop_id, banner_id
- Repository pattern + Unit of Work (from data-stack standards)
- AutoMapper for DTO transformation
- JWT authentication at API controller level

---

### Unit 2: Version Control Service

**Identifier**: `002-version-control-service`

**Purpose**: Maintain 10-version history per banner with rollback capability

**Responsibility**:
- Create version snapshots on banner save
- Retrieve historical versions
- Restore/rollback to previous version
- Version metadata (timestamp, user, change summary)
- Enforce 10-version limit (keep latest 10, discard oldest)

**Assigned Requirements**: FR-7

**Key Entities**:
- BannerVersion (aggregate root)
  - VersionId, BannerId, ShopId
  - VersionNumber (1-10)
  - SnapshotData (full banner configuration at this version)
  - CreatedAt, CreatedBy
  - ChangeDescription

**Dependencies**: Banner Service (provides BannerId context)

**Interfaces**:
- POST /api/banners/{id}/versions → Create version snapshot (called after banner save)
- GET /api/banners/{id}/versions → List all versions
- GET /api/banners/{id}/versions/{versionId} → Get specific version details
- POST /api/banners/{id}/versions/{versionId}/restore → Restore version (creates new version as "restored from X")

**NFR Targets**:
- Response time < 500ms for version restore
- Automatic snapshot on save
- Storage: 10 versions × ~1MB per banner = 10MB max per banner

**Technical Constraints**:
- MSSQL with efficient blob storage for snapshots
- Implement version-number rollover (discard oldest when 11th version created)
- Audit trail for rollbacks

---

### Unit 3: Effects Engine

**Identifier**: `003-effects-engine`

**Purpose**: Visual effects and carousel/rotation functionality

**Responsibility**:
- Define available effects (opacity, rotation, scale, animation, blur, etc.)
- Apply effects to components
- Configure carousel/rotation (image/video lists with timing)
- Validate effect parameters
- Render/preview effects

**Assigned Requirements**: FR-4, FR-5

**Key Entities**:
- EffectConfiguration (value object)
  - EffectType (enum: opacity, rotation, scale, blur, etc.)
  - Parameters (effect-specific: duration, easing, intensity)
  - Applied to Component

- CarouselConfiguration (value object)
  - MediaList (ordered list of media items)
  - RotationTiming (seconds per item, or auto per video duration)
  - BackgroundBehavior (rotate while static content on top)

**Dependencies**: Banner Service (provides component context)

**Interfaces**:
- GET /api/effects/library → Available effects and parameters
- PUT /api/banners/{id}/components/{componentId}/effects → Apply effect
- PUT /api/banners/{id}/components/{componentId}/carousel → Configure carousel

**NFR Targets**:
- Real-time preview of effects (< 100ms lag)
- Smooth animations (60 FPS)
- Support complex effect chains

**Technical Constraints**:
- CSS/Canvas rendering effects preview on frontend
- Backend validates effect parameters and stores configuration
- Integration with Media Service for carousel timing

---

### Unit 4: Media Service

**Identifier**: `004-media-service`

**Purpose**: Video/image handling with 4K/8K support, uploads, storage references

**Responsibility**:
- Media file upload (images, videos, graphics)
- Validate media type and file size
- Store media references and metadata
- Support 4K/8K video formats
- Video codec/quality handling
- Chunked upload support for large files
- Return media URLs for use in banners

**Assigned Requirements**: FR-6

**Key Entities**:
- MediaFile (aggregate root)
  - MediaId, ShopId, BannerId (context)
  - FileName, ContentType, FileSize
  - StoragePath (local path or Azure Blob reference)
  - Metadata (resolution, duration for videos, codec)
  - UploadedAt, UploadedBy

- VideoMetadata (value object)
  - Duration (in seconds)
  - Resolution (width, height, 4K/8K flag)
  - Codec, Bitrate
  - FrameRate

**Dependencies**: None (but used by Banner Service for media references)

**Interfaces**:
- POST /api/media/upload → Upload media file (multipart/form-data)
- POST /api/media/upload-chunk → Chunked upload endpoint
- GET /api/media/{id} → Get media metadata
- DELETE /api/media/{id} → Delete media file
- GET /api/media/{id}/url → Get media access URL (with temporary signed URL for Azure)

**NFR Targets**:
- Support files up to 500MB (4K/8K videos)
- Chunked uploads with resume capability
- Video streaming optimization (adaptive bitrate)
- No quality degradation for 4K/8K

**Technical Constraints**:
- Local file system (development) → Azure Blob Storage (production)
- File type validation (MIME types: video/*, image/*, etc.)
- Implement chunked upload with progress tracking
- Video transcoding or format conversion (if needed) - scope TBD
- CDN/optimization layer for media delivery (future)

---

### Unit 5: Banner Editor UI (Frontend)

**Identifier**: `005-banner-editor-ui`

**Unit Type**: frontend

**Purpose**: Next.js application - Drag-and-drop editor frontend for banner creation and editing

**Responsibility**:
- Drag-and-drop canvas interface
- Component library/toolbox
- Real-time property panels for component editing
- Visual effect selection and preview
- Carousel/rotation configuration UI
- Save/publish interface
- Version history UI and rollback buttons
- Preview mode
- Multi-user session management
- Shop-level data display (only user's shop banners)

**Assigned Requirements**: FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-7, FR-8, FR-10 (all user-facing aspects)

**Key Features**:
- Canvas component (drag-drop framework: react-dnd or alternative)
- Component toolbox with text, image, video, graphics options
- Property inspector for component configuration
- Effects selector and preview
- Layer panel for z-index management
- Version history sidebar with rollback buttons
- Save button (auto-save with version creation)
- Preview mode toggle
- Responsive design

**Dependencies**: All backend units (Banner Service, Version Control, Effects Engine, Media Service)

**Interfaces** (consumes backend REST APIs):
- All Banner Service endpoints
- All Version Control Service endpoints
- All Effects Engine endpoints
- All Media Service endpoints

**NFR Targets**:
- Canvas load time < 2 seconds
- Component interactions < 100ms lag
- Smooth drag-drop experience
- Mobile-responsive (tablet+ support)
- Intuitive navigation and low learning curve

**Technical Constraints**:
- Next.js with TypeScript (strict mode)
- Drag-drop library (react-dnd or react-beautiful-dnd)
- State management (React Context or Zustand)
- CSS-in-JS or Tailwind for styling
- Eslint + Prettier for code quality
- Jest + React Testing Library for tests

---

## Dependency Graph

```
┌──────────────────────────────────────────────┐
│         Frontend Unit (005)                  │
│     Banner Editor UI (Next.js)               │
│      ↓↓↓↓↓ (depends on all)                  │
├──────────┬───────────┬──────────┬────────────┤
│          │           │          │            │
▼          ▼           ▼          ▼            ▼
Unit-001   Unit-002   Unit-003   Unit-004    (others)
Banner     Version    Effects    Media
Service    Control    Engine     Service
  ↓
  └─ No dependencies (foundation)
```

**Execution Order** (for construction):
1. **Unit 1**: Banner Service (foundation)
2. **Unit 2**: Version Control Service (depends on Unit 1 context)
3. **Unit 3**: Effects Engine (enhances Unit 1)
4. **Unit 4**: Media Service (independent, feeds into Unit 1)
5. **Unit 5**: Banner Editor UI (depends on all backend units)

---

## Summary

| Unit | Purpose | Type | Stories | Bolt Type |
|------|---------|------|---------|-----------|
| 001-banner-service | Core banner CRUD | Backend | ~8 | DDD |
| 002-version-control-service | Version history & rollback | Backend | ~3 | DDD |
| 003-effects-engine | Visual effects & carousel | Backend | ~4 | DDD |
| 004-media-service | Video/image handling | Backend | ~4 | DDD |
| 005-banner-editor-ui | Drag-drop editor UI | Frontend | ~10 | Simple |

**Total Stories**: ~29

---

## Next Steps

1. Create detailed **User Stories** for each unit
2. Group stories into **Bolts** (time-boxed execution sessions, typically 3-5 days each)
3. Review all artifacts at **Checkpoint 3**
4. Proceed to **Construction Phase**
