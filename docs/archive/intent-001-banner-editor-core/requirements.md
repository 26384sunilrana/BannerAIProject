---
intent: 001-banner-editor-core
phase: inception
status: draft
created: 2026-09-26T00:00:00Z
updated: 2026-09-26T00:00:00Z
---

# Requirements: Banner Editor Core

## Intent Overview

Enable shop owners and sales executives to create visually appealing digital banners using a simple, intuitive drag-and-drop editor. The editor supports rich components (text, images, videos, graphics), visual effects, component layering, carousel rotation, and version history with rollback capabilities. No coding required—all operations are no-code/low-code through the web application.

**Key Focus**: Production-grade application (not MVP) with emphasis on ease of use, data isolation between shops, video performance (4K/8K support), and integration with advertisement tracking for revenue calculation.

---

## Business Goals

| Goal | Success Metric | Priority |
|------|----------------|----------|
| Enable no-code banner creation | 100% of features accessible via UI, zero manual coding | Must |
| Intuitive user experience | Low learning curve, simple navigation, findable features | Must |
| Support rich media components | Support images, videos (4K/8K), graphics, text without limitations | Must |
| Preserve design history | 10-version history with rollback capability | Must |
| Preview before publishing | Users can preview final appearance before commit | Must |
| Track advertisement placement | Integrate with ad system: publication time, duration, cumulative hours | Must |
| Production-ready system | Full production application with enterprise-grade reliability | Must |
| Revenue enablement | Accurate ad tracking enables revenue calculation for subscriptions | Must |

---

## Functional Requirements

### FR-1: Drag-and-Drop Canvas
- **Description**: Provide a visual canvas where shop owners can drag components into position
- **Acceptance Criteria**:
  - Canvas renders with visible grid/guidelines
  - Components can be dragged from toolbox to canvas
  - Components can be resized and repositioned on canvas
  - Canvas dimensions match deployed banner size
- **Priority**: Must
- **Related Stories**: TBD (defined in story creation phase)

### FR-2: Component System
- **Description**: Support multiple component types (text, images, videos, graphics) with configurable properties
- **Acceptance Criteria**:
  - Text component: font, size, color, alignment, effects
  - Image component: upload, resize, effects, rotation
  - Video component: upload, duration, timing, mute/volume control, list rotation
  - Graphics component: shapes, colors, effects
  - Each component has property panel for customization
- **Priority**: Must

### FR-3: Component Layering (Z-Index)
- **Description**: Control which components appear on top when they overlap
- **Acceptance Criteria**:
  - Visual indicator showing layer order (layer panel)
  - User can reorder layers (bring to front, send to back)
  - CSS z-index properly applied in rendered banner
- **Priority**: Must

### FR-4: Visual Effects
- **Description**: Apply effects to components (opacity, rotation, scale, animation, blur, etc.)
- **Acceptance Criteria**:
  - Effect picker for each component
  - Preview of effect in canvas (real-time or near real-time)
  - Effects persist in saved design
- **Priority**: Must

### FR-5: Carousel/Rotation
- **Description**: Support rotating background images or videos in sequence
- **Acceptance Criteria**:
  - Add list of images/videos to a component
  - Configure rotation timing (seconds per item)
  - Background rotates through list while static content stays on top
  - Falling back to manual timing if video duration is shorter than configured time
- **Priority**: Must

### FR-6: Video Handling (4K/8K Support)
- **Description**: Support video components with advanced timing, audio control, and high-quality formats without limitations
- **Acceptance Criteria**:
  - Upload video files in all common formats (MP4, WebM, MOV, etc.)
  - Support high-resolution videos (4K, 8K) without quality degradation
  - Configure per-video duration before rotation
  - Toggle mute/unmute for audio
  - Play full video before rotation, or rotate at specified interval
  - Handle short videos (shorter than rotation interval)
  - Video performance optimized (fast streaming, no buffering delays)
- **Priority**: Must

### FR-7: Version History (10 Versions)
- **Description**: Maintain up to 10 historical versions of each banner design
- **Acceptance Criteria**:
  - Auto-save creates version snapshot on each save
  - Version list shows all available versions with timestamps
  - User can view/preview any historical version
  - User can rollback to any historical version
  - Rollback to old version keeps older version as "latest" history entry
- **Priority**: Must

### FR-8: Preview Functionality
- **Description**: Allow users to preview banner appearance before publishing
- **Acceptance Criteria**:
  - Preview mode shows banner as it will appear on deployed board
  - Preview available to all user roles (Shop Owner, Sales Executive)
  - Preview reflects all components, effects, and animations
  - Can switch between edit and preview modes
- **Priority**: Must

### FR-9: No-Code Deployment
- **Description**: All banner creation/modification happens in web app without code deployment
- **Acceptance Criteria**:
  - Create new banner → immediately appears in system
  - Modify banner → changes apply in real-time
  - Publish banner → no backend code changes needed
  - Approval workflow → web UI only, no deployment required
- **Priority**: Must

### FR-10: Multi-User Access with Data Isolation
- **Description**: Support Shop Owner and Sales Executives with strict data isolation (critical for security and compliance)
- **Acceptance Criteria**:
  - Shop Owner can view and edit own banners only
  - Sales Executives can create/edit banners assigned to them only
  - **CRITICAL: Two users must NEVER see each other's data** (different shops have isolated data)
  - Multiple users from same shop can access preview simultaneously (read-only)
  - User sessions properly isolated at database and API levels
  - Role-based access control prevents cross-shop data leakage
- **Priority**: Must

---

## Non-Functional Requirements

### Performance
| Requirement | Metric | Target |
|-------------|--------|--------|
| Canvas Load Time | Initial render | < 2 seconds |
| Component Drag/Drop | UI responsiveness | < 100ms interaction lag |
| Save Operation | Database commit | < 500ms |
| Preview Render | Full design render | < 1 second |
| Published Banner Display | Complex design on deployed board | ≤ 5 seconds |
| Scheduled Publishing | Future-dated publish | Instant (or per schedule) |
| Video Playback | High-quality video (4K/8K) | No buffering, smooth playback |

### Scalability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Concurrent Users | Simultaneous editors | 100+ per service instance |
| Banner Size | Max pixels | 4K resolution (3840 x 2160) |
| Component Count | Max components per banner | 50 components |
| Media Upload | Max file size | 100MB per file |

### Security
| Requirement | Standard | Notes |
|-------------|----------|-------|
| Data Encryption | AES-256 | Banner designs encrypted at rest and in transit |
| API Authentication | JWT | Token-based access control |
| Authorization | RBAC + Data Isolation | Shop Owner, Sales Executive roles with strict data segregation |
| Data Isolation | Multi-tenant architecture | Different shops completely isolated; no cross-shop data leakage |
| File Upload Validation | MIME type + scanning | Validate uploaded media (images, videos, graphics) |
| Session Management | Secure cookies/tokens | Prevent session hijacking, enforce logout |

### Reliability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Availability | Uptime | 99.5% (during operating hours) |
| Data Backup | Auto-save frequency | Save every 30 seconds or on user action |
| Recovery | Version rollback | User-initiated, < 1 second |

### Usability
| Requirement | Metric | Target |
|-------------|--------|--------|
| Learning Curve | First-time user | Intuitive UI, minimal training needed |
| Navigation | Feature discoverability | Simple, clear menus; easily find tools |
| Accessibility | WCAG 2.1 | AA compliance for web UI |
| Responsiveness | UI lag | < 200ms for all interactions |
| Design Complexity | Measurable | Support complex designs (50+ components, multiple layers, effects) |

---

## Constraints

### Technical Constraints
- **Frontend**: Next.js with TypeScript (strict mode) — production-grade
- **Backend**: ASP.NET Core Web API (microservice) — production-grade
- **Database**: MSSQL (local development) / Azure SQL (production)
- **Media Storage**: Local project folder (development) / Azure Blob Storage (production)
- **Component Limit**: Canvas supports up to 50 components per banner (performance constraint)
- **Video Format Support**: All common video formats (MP4, WebM, MOV, MKV, AVI, etc.)
- **Video Quality**: Support 4K (3840x2160), 8K (7680x4320) without limitations
- **Data Isolation**: Multi-tenant architecture with strict shop-level data segregation
- **Scalability**: Design for 1000+ shops with concurrent users

### Business Constraints
- **Scope**: Full production application (not MVP) — comprehensive feature set
- **Quality**: Production-grade reliability, security, performance
- **Scale**: Start with 10 shops, scale to 1000+, ready for 10,000+
- **Revenue Integration**: Tightly integrated with ad tracking for subscription revenue calculation
- **Build Approach**: Quality over speed; take necessary time to build correctly

---

## Assumptions

| Assumption | Risk if Invalid | Mitigation |
|------------|-----------------|------------|
| Drag-drop libraries available in Next.js | No suitable library exists | Evaluate: react-dnd, react-beautiful-dnd, custom Canvas API implementation |
| Media storage in Azure Blob (not DB blobs) | MSSQL bloat, slow queries | Use Azure Blob Storage for all media, store metadata + references in DB |
| Data isolation achievable via RBAC at API level | Cross-shop data leakage possible | Implement strict query filters, audit logs, security testing |
| Video performance acceptable at 4K/8K quality | Video buffering/lag issues | Implement adaptive bitrate streaming, CDN for video delivery |
| 10-version history sufficient | Users want more history | Design extensible versioning (easy to increase) |
| Shop owners have sufficient upload bandwidth | Slow uploads degrade UX | Implement chunked uploads, progress tracking, pause/resume |
| Advertisement tracking can be integrated at banner level | Revenue calculation fails | Design ad events system early, test with realistic ad scenarios |

---

## Open Questions

| Question | Owner | Due Date | Resolution |
|----------|-------|----------|------------|
| Should editor support component groups/layers (hierarchical organization)? | Product Owner | During units decomposition | Pending |
| Is undo/redo required in addition to version history? | Product Owner | During story creation | Pending |
| How are advertisement placements tracked (triggered at publish? scheduled? bandwidth-based?)? | Product Owner + Revenue Team | During context definition | Pending |
| What video codec/compression standards for 4K/8K support? | Technical Lead | During unit planning | Pending |
| How will multi-user concurrent edit conflicts be handled? | Technical Lead | During system context | Pending |

---

## Next Steps

This requirements document will be reviewed and refined in the next checkpoint (Checkpoint 1: Requirements Review). 

Following approval:
1. Define system context and architecture boundaries
2. Decompose into units (frontend editor, backend API, database schema)
3. Create user stories with acceptance criteria
4. Plan construction bolts (time-boxed execution sessions)
