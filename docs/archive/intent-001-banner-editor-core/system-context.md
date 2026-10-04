---
intent: 001-banner-editor-core
phase: inception
status: context-defined
updated: 2026-09-26T00:00:00Z
---

# System Context: Banner Editor Core

## System Overview

The Banner Editor is a no-code/low-code web application that enables shop owners and sales executives to create, design, and manage digital banners using drag-and-drop components. The system supports rich media (text, images, videos, graphics), visual effects, component layering, carousel rotation, and 10-version history with rollback. All changes are made through the web UI without requiring code deployment.

The editor is part of a larger SaaS platform where subscription costs are funded by advertisement revenue tracked at the banner level.

---

## Actors

### Human Actors

- **Shop Owner** (Primary): Creates banners, approves designs, manages subscription, views analytics
- **Sales Executive** (Secondary): Creates and edits banners on behalf of shop owner, collaborates
- **Administrator** (System): Manages shops, users, billing, monitors system health (future scope)

### System Actors

- **Advertisement System** (Future): Receives banner publication events for ad tracking and revenue calculation
- **Authentication Service**: Validates user identity and roles
- **Notification Service** (Future): Sends alerts for subscription renewal, approvals, etc.

---

## External Integrations

### Current (Banner Editor Core)

- **Azure SQL Database** (Backend): Stores banner designs, versions, user data
- **Azure Blob Storage** (Future): Stores media files (images, videos, graphics)
- **Authentication Provider**: JWT-based identity (implemented at Backend API level)

### Future (Not in this intent)

- **Advertisement Management System**: Receives banner publication events, tracks ad placement duration
- **Payment/Billing System**: Charges shops based on ad revenue
- **Email/SMS Service**: Notifications for subscription, approvals
- **Analytics Dashboard**: Reports and analytics (shop-level and platform-level)

---

## System Boundaries

### In Scope (Banner Editor Core)

- ✅ Drag-and-drop canvas for component design
- ✅ Component management (text, images, videos, graphics)
- ✅ Visual effects and component layering
- ✅ Version history (10 versions) with rollback
- ✅ Preview functionality
- ✅ Multi-user access with data isolation
- ✅ No-code deployment (all changes via UI)

### Out of Scope (Future Intents)

- ❌ Approval workflows (separate intent: `approval-workflow`)
- ❌ Publishing and scheduling (separate intent: `publishing-system`)
- ❌ Advertisement system integration (separate intent: `advertisement-management`)
- ❌ Subscription management (separate intent: `subscription-management`)
- ❌ Real-time collaboration (multi-user simultaneous editing)

---

## Data Flows

### Inbound Data

**User Inputs** (from Frontend → Backend API):
- Banner creation request (metadata: name, description)
- Component addition/editing (type, properties, position, size, effects)
- Media uploads (images, videos, graphics)
- Save/publish requests
- Version rollback requests

**Data Format**: JSON over HTTP REST API

**Validation Needed**:
- User authentication (JWT token validation)
- User authorization (shop-level data isolation)
- Banner metadata validation
- Component property validation
- Media file type and size validation

### Outbound Data

**API Responses** (Backend → Frontend):
- Banner designs (full configuration, metadata)
- Version history list
- Component libraries and templates
- User permissions and roles
- Preview-ready banner configuration

**Data Format**: JSON over HTTP REST API

**To External Systems** (Future):
- Advertisement events: Banner published → `{banner_id, publication_time, scheduled_date}`
- Billing events: Banner minutes tracked for ad revenue → `{banner_id, cumulative_hours}`

---

## Data Isolation & Multi-Tenancy

### Critical for This System

- **Shop-Level Isolation**: Each shop's banners, versions, and media are completely isolated
- **User Access Control**: Sales Executive can only access banners within their assigned shop
- **Query Filters**: All database queries include `WHERE shop_id = {current_user_shop_id}`
- **No Cross-Shop Access**: API prevents any query or operation that could leak data between shops

### Implementation Approach

- Backend API enforces shop_id context for every request
- Frontend receives banner data scoped to current user's shop only
- Database row-level security (if using MSSQL RLS) enforces isolation
- Audit logs track access for compliance

---

## Integration Points

### Frontend → Backend API

```
Next.js Application
    ↓ (REST API - JWT Auth)
ASP.NET Core Web API
    ↓ (Entity Framework ORM)
MSSQL Database (Local) / Azure SQL (Prod)
    ↓ (Blob references)
Azure Blob Storage (Media files)
```

### Frontend Technology Stack

- **Framework**: Next.js (TypeScript)
- **Components**: Drag-drop library (react-dnd, react-beautiful-dnd, or custom)
- **Communication**: REST API calls with JWT authentication
- **Storage**: Local state management (React Context or Zustand for complex state)

### Backend Technology Stack

- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core
- **Patterns**: Repository + Unit of Work
- **Mapping**: AutoMapper (domain models ↔ DTOs)
- **Data Layer**: MSSQL (local) / Azure SQL (production)
- **Media Handling**: File storage references (future: Azure Blob Storage)

---

## Key Non-Functional Requirements

### Performance Targets

- Canvas load time: < 2 seconds
- Component interactions: < 100ms lag
- Save operations: < 500ms
- Published banner display: ≤ 5 seconds for complex designs
- Video streaming: No buffering for 4K/8K video

### Security & Data Protection

- Data encryption at rest (AES-256)
- Data encryption in transit (HTTPS)
- Shop-level data isolation (critical)
- JWT-based authentication
- Session management with secure tokens
- Media upload validation (MIME type, file scanning)

### Scalability Targets

- Support 1000+ shops with concurrent users
- Handle 50+ components per banner
- Support 4K/8K video without quality loss
- Auto-save and version history without performance degradation

### Reliability

- 99.5% uptime (during operating hours)
- Automatic backups of banner versions
- User-initiated version rollback capability
- Data recovery from blob storage

---

## Deployment Context

### Local Development

- Frontend: Next.js development server (localhost:3000)
- Backend: ASP.NET Core dev server (localhost:5000)
- Database: MSSQL Server (local or Docker container)
- Media: Local file system

### Production (Azure)

- Frontend: Azure App Service or Static Web Apps (Next.js)
- Backend: Azure App Service (ASP.NET Core Web API)
- Database: Azure SQL Database
- Media: Azure Blob Storage
- Orchestration: Docker + Kubernetes (AKS)
- Infrastructure as Code: Terraform

---

## System Context Diagram

```
┌─────────────────────────────────────────────────────────┐
│                                                           │
│  ┌──────────────────┐                                    │
│  │   Shop Owner /   │                                    │
│  │  Sales Executive │                                    │
│  │  (Human Users)   │                                    │
│  └────────┬─────────┘                                    │
│           │                                              │
│           │ (REST API + JWT)                             │
│           ▼                                              │
│  ┌──────────────────────────────────────┐               │
│  │   Next.js Frontend Application       │               │
│  │  (Drag-Drop Banner Editor)           │               │
│  │  - Canvas                            │               │
│  │  - Component Library                 │               │
│  │  - Version History                   │               │
│  │  - Preview                           │               │
│  └──────────┬───────────────────────────┘               │
│             │                                             │
│             │ (REST API - JSON)                          │
│             ▼                                             │
│  ┌──────────────────────────────────────┐               │
│  │  ASP.NET Core Web API (Microservice) │               │
│  │  - Banner CRUD                       │               │
│  │  - Component Management              │               │
│  │  - Version Control                   │               │
│  │  - Version History                   │               │
│  │  - Authentication & Authorization    │               │
│  │  - Multi-Tenant Data Isolation       │               │
│  └──────────┬──────────────────────────┬┘               │
│             │                          │                 │
│             ▼                          ▼                 │
│  ┌─────────────────┐          ┌──────────────────┐      │
│  │  MSSQL Database │          │  Azure Blob      │      │
│  │  (Local/Azure)  │          │  Storage (Prod)  │      │
│  │  - Banners      │          │  - Media Files   │      │
│  │  - Versions     │          │  - Images        │      │
│  │  - Users        │          │  - Videos (4K/8K)       │
│  │  - Media Refs   │          │  - Graphics      │      │
│  └─────────────────┘          └──────────────────┘      │
│                                                           │
│  (Future Integrations)                                   │
│  - Advertisement System                                  │
│  - Billing/Revenue Tracking                              │
│  - Notifications Service                                 │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

---

## High-Level Constraints

### Technical

- Must use Next.js (TypeScript) for frontend
- Must use ASP.NET Core Web API for backend
- Must use MSSQL for database (local) / Azure SQL (production)
- Must support Docker containerization and Kubernetes orchestration
- Must be production-grade (not MVP)

### Business

- Must enable no-code banner creation (all changes via UI)
- Must support 4K/8K video without limitations
- Must implement strict data isolation (no cross-shop data leakage)
- Must integrate with advertisement tracking system (for revenue)
- Must maintain 10-version history per banner
- Must scale from 10 shops → 1000+ shops

### Compliance

- Data encryption at rest and in transit
- HIPAA/PHI compliance readiness (as per overall project)
- Audit logging for data access and changes

---

## Next Steps

This system context establishes the boundaries and integrations for the Banner Editor Core. Next:

1. **Units Decomposition** → Break into frontend and backend components
2. **User Stories** → Define detailed features for each unit
3. **Bolt Plan** → Group stories into time-boxed execution sessions
4. **Checkpoint 3** → Review all artifacts together
