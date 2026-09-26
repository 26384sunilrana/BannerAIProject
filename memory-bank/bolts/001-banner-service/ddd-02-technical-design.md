---
unit: 001-banner-service
bolt: 001-banner-service
stage: design
status: complete
created: 2026-09-26T00:00:00Z
---

# Technical Design - Banner Service

## Architecture Pattern

**Clean Architecture (Hexagonal)** with Domain-Driven Design principles

**Rationale**:
- Clear separation of concerns across 4 layers (Presentation, Application, Domain, Infrastructure)
- Domain logic isolated from infrastructure details (database, API framework)
- Testable: domain and business logic can be tested without database or HTTP
- Scalable: each layer can evolve independently
- Aligns with team standards (coding-standards.md specifies Clean Architecture)

**Constraint**: Multi-tenant data isolation enforced at repository layer (all queries filter by ShopId)

---

## Layer Structure

```text
┌──────────────────────────────────────────────────┐
│      Presentation Layer                          │
│  Controllers, Middleware, Request/Response DTOs  │
│  → BannersController, ComponentsController       │
├──────────────────────────────────────────────────┤
│      Application Layer                           │
│  Use Cases, DTOs, Validators, Service Interfaces │
│  → BannerService, ComponentService               │
├──────────────────────────────────────────────────┤
│      Domain Layer                                │
│  Entities, Value Objects, Domain Services        │
│  → Banner, Component, TextComponentProperties    │
├──────────────────────────────────────────────────┤
│      Infrastructure Layer                        │
│  Database (EF Core), Repositories, UnitOfWork    │
│  → BannerRepository, ApplicationDbContext        │
└──────────────────────────────────────────────────┘
```

**Layer Responsibilities**:

| Layer | Responsibility | Dependencies |
|-------|----------------|--------------|
| Presentation | HTTP routing, request validation, response formatting | Application layer only |
| Application | Use case orchestration, DTOs, business rules enforcement | Domain layer, Infrastructure interfaces |
| Domain | Entity definitions, value objects, business logic, rules | None (no external dependencies) |
| Infrastructure | Database access, persistence, external service integration | Domain interfaces, EF Core |

---

## Project Structure

```
BannerService/
├── src/
│   ├── Presentation/
│   │   ├── Controllers/
│   │   │   ├── BannersController.cs
│   │   │   └── ComponentsController.cs
│   │   ├── Middleware/
│   │   │   ├── ShopContextMiddleware.cs          (extracts ShopId from request)
│   │   │   └── GlobalExceptionHandlingMiddleware.cs
│   │   ├── Dto/
│   │   │   ├── CreateBannerRequestDto.cs
│   │   │   ├── BannerResponseDto.cs
│   │   │   ├── AddComponentRequestDto.cs
│   │   │   └── ComponentResponseDto.cs
│   │   └── Program.cs
│   │
│   ├── Application/
│   │   ├── Services/
│   │   │   ├── BannerService.cs
│   │   │   └── ComponentService.cs
│   │   ├── Interfaces/
│   │   │   ├── IBannerService.cs
│   │   │   └── IComponentService.cs
│   │   ├── Dto/
│   │   │   ├── CreateBannerCommand.cs
│   │   │   ├── AddComponentCommand.cs
│   │   │   └── BannerPreviewDto.cs
│   │   └── Validators/
│   │       ├── CreateBannerValidator.cs
│   │       ├── AddComponentValidator.cs
│   │       └── ComponentPropertiesValidator.cs
│   │
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── Banner.cs
│   │   │   └── Component.cs
│   │   ├── ValueObjects/
│   │   │   ├── TextComponentProperties.cs
│   │   │   ├── ImageComponentProperties.cs
│   │   │   ├── Position.cs
│   │   │   ├── Size.cs
│   │   │   └── ShopContext.cs
│   │   ├── Services/
│   │   │   ├── BannerDomainService.cs
│   │   │   └── ComponentValidationService.cs
│   │   ├── Events/
│   │   │   ├── BannerCreatedEvent.cs
│   │   │   ├── ComponentAddedEvent.cs
│   │   │   └── ComponentRemovedEvent.cs
│   │   └── Interfaces/
│   │       ├── IBannerRepository.cs
│   │       ├── IComponentRepository.cs
│   │       └── IUnitOfWork.cs
│   │
│   └── Infrastructure/
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   └── Configurations/
│       │       ├── BannerEntityConfiguration.cs
│       │       └── ComponentEntityConfiguration.cs
│       ├── Repositories/
│       │   ├── BannerRepository.cs
│       │   ├── ComponentRepository.cs
│       │   └── UnitOfWork.cs
│       └── Middleware/
│           └── ShopContextAccessor.cs
│
└── tests/
    ├── Domain.Tests/
    │   ├── BannerTests.cs
    │   └── ComponentValidationServiceTests.cs
    ├── Application.Tests/
    │   ├── BannerServiceTests.cs
    │   └── ComponentServiceTests.cs
    └── Integration.Tests/
        ├── BannerRepositoryTests.cs
        └── BannersControllerTests.cs
```

---

## API Design

### Banner Endpoints

| Endpoint | Method | Request | Response | Status |
|----------|--------|---------|----------|--------|
| `/api/banners` | POST | CreateBannerRequest | BannerResponse (201) | 201 Created, 400 Bad Request, 401 Unauthorized |
| `/api/banners/{id}` | GET | None | BannerResponse | 200 OK, 404 Not Found, 401 Unauthorized |
| `/api/banners` | GET | Query: page, limit | BannerResponse[] (paged) | 200 OK, 401 Unauthorized |
| `/api/banners/{id}` | PUT | UpdateBannerRequest | BannerResponse | 200 OK, 404 Not Found, 400 Bad Request |
| `/api/banners/{id}` | DELETE | None | 204 No Content | 204 No Content, 404 Not Found, 409 Conflict (if published) |
| `/api/banners/{id}/preview` | GET | None | PreviewResponse | 200 OK, 404 Not Found |

### Component Endpoints

| Endpoint | Method | Request | Response | Status |
|----------|--------|---------|----------|--------|
| `/api/banners/{bannerId}/components` | POST | AddComponentRequest | ComponentResponse (201) | 201 Created, 400 Bad Request, 404 Banner Not Found |
| `/api/banners/{bannerId}/components/{componentId}` | PUT | UpdateComponentRequest | ComponentResponse | 200 OK, 404 Not Found, 400 Bad Request |
| `/api/banners/{bannerId}/components/{componentId}` | DELETE | None | 204 No Content | 204 No Content, 404 Not Found |

### Request/Response Schemas

**CreateBannerRequest**:
```json
{
  "name": "Summer Sale Banner",
  "description": "Promotional banner for summer sale",
  "width": 1200,
  "height": 600
}
```

**BannerResponse**:
```json
{
  "id": "uuid",
  "shopId": "uuid",
  "name": "Summer Sale Banner",
  "description": "Promotional banner for summer sale",
  "width": 1200,
  "height": 600,
  "isPublished": false,
  "componentCount": 3,
  "createdAt": "2026-09-26T10:00:00Z",
  "updatedAt": "2026-09-26T10:30:00Z"
}
```

**AddComponentRequest**:
```json
{
  "componentType": "Text",
  "position": { "x": 100, "y": 150 },
  "size": { "width": 400, "height": 100 },
  "zIndex": 1,
  "properties": {
    "content": "Welcome!",
    "fontFamily": "Arial",
    "fontSize": 32,
    "color": "#FF0000",
    "fontWeight": "Bold",
    "textAlign": "Center"
  }
}
```

**ComponentResponse**:
```json
{
  "id": "uuid",
  "bannerId": "uuid",
  "componentType": "Text",
  "position": { "x": 100, "y": 150 },
  "size": { "width": 400, "height": 100 },
  "zIndex": 1,
  "properties": { ... },
  "createdAt": "2026-09-26T10:00:00Z"
}
```

**PreviewResponse**:
```json
{
  "bannerId": "uuid",
  "shopId": "uuid",
  "name": "Summer Sale Banner",
  "width": 1200,
  "height": 600,
  "isPublished": false,
  "components": [
    {
      "id": "uuid",
      "componentType": "Text",
      "position": { "x": 100, "y": 150 },
      "size": { "width": 400, "height": 100 },
      "zIndex": 1,
      "properties": { ... }
    }
  ]
}
```

---

## Data Persistence

### Database Schema

**Banners Table**:
```sql
CREATE TABLE Banners (
  Id UNIQUEIDENTIFIER PRIMARY KEY,
  ShopId UNIQUEIDENTIFIER NOT NULL,
  UserId UNIQUEIDENTIFIER NOT NULL,
  Name NVARCHAR(255) NOT NULL,
  Description NVARCHAR(MAX),
  Width INT NOT NULL,
  Height INT NOT NULL,
  IsPublished BIT NOT NULL DEFAULT 0,
  CreatedAt DATETIME2 NOT NULL,
  UpdatedAt DATETIME2 NOT NULL,
  CONSTRAINT FK_Banners_ShopId FOREIGN KEY (ShopId) REFERENCES Shops(Id),
  INDEX IX_Banners_ShopId_Id (ShopId, Id),
  INDEX IX_Banners_ShopId_CreatedAt (ShopId, CreatedAt DESC)
);
```

**Components Table**:
```sql
CREATE TABLE Components (
  Id UNIQUEIDENTIFIER PRIMARY KEY,
  BannerId UNIQUEIDENTIFIER NOT NULL,
  ComponentType NVARCHAR(50) NOT NULL,
  PositionX INT NOT NULL,
  PositionY INT NOT NULL,
  SizeWidth INT NOT NULL,
  SizeHeight INT NOT NULL,
  ZIndex INT NOT NULL,
  Properties NVARCHAR(MAX) NOT NULL,  -- JSON string
  CreatedAt DATETIME2 NOT NULL,
  UpdatedAt DATETIME2 NOT NULL,
  CONSTRAINT FK_Components_BannerId FOREIGN KEY (BannerId) REFERENCES Banners(Id) ON DELETE CASCADE,
  INDEX IX_Components_BannerId_ZIndex (BannerId, ZIndex),
  INDEX IX_Components_ComponentType (ComponentType)
);
```

| Table | Columns | Relationships |
|-------|---------|---------------|
| **Banners** | Id, ShopId, UserId, Name, Description, Width, Height, IsPublished, CreatedAt, UpdatedAt | FK: ShopId → Shops; 1-to-Many: Components |
| **Components** | Id, BannerId, ComponentType, PositionX, PositionY, SizeWidth, SizeHeight, ZIndex, Properties (JSON), CreatedAt, UpdatedAt | FK: BannerId → Banners (cascade delete) |

**Indexes**:
- `IX_Banners_ShopId_Id`: For tenant isolation queries and single banner retrieval
- `IX_Banners_ShopId_CreatedAt`: For listing banners by shop with sorting
- `IX_Components_BannerId_ZIndex`: For efficient component ordering
- `IX_Components_ComponentType`: For component type filtering

**Indexing Strategy**:
- All queries filter by ShopId first (tenant isolation)
- (ShopId, BannerId) composite indexes for fast queries
- ZIndex index for component layering queries
- Supports up to 1000+ shops, 1000s banners per shop, 50+ components per banner

---

## Security Design

| Concern | Approach |
|---------|----------|
| **Authentication** | Bearer JWT token in Authorization header; issued by identity service |
| **Authorization** | Shop-level isolation enforced via ShopContextMiddleware; all queries filter by ShopId from request context |
| **Data Isolation** | Request-scoped ShopContext injected into repositories; all queries WHERE ShopId = CurrentShopId |
| **Data Validation** | Server-side validation of component properties (size, position, colors) in validators |
| **SQL Injection** | Entity Framework Core parameterized queries prevent SQL injection |
| **CORS** | Allow cross-origin requests from Banner Editor UI only (configured in ASP.NET CORS policy) |
| **Input Validation** | FluentValidation rules for all DTOs; max lengths, ranges validated |
| **Error Messages** | Sensitive details not exposed to client; standardized error responses with codes |

---

## NFR Implementation

| Requirement | Design Approach |
|-------------|-----------------|
| **Response Time < 200ms** | Indexed database queries; cached component properties; pre-computed preview; minimal EF Core allocations |
| **Support 50+ Components** | Efficient JSON storage of properties; indexed queries on ComponentType and ZIndex; pagination in list endpoints |
| **Scalability (1000+ Shops)** | Multi-tenant query filters prevent full table scans; database indexes on (ShopId, BannerId); stateless API for horizontal scaling |
| **Query Optimization** | (ShopId, BannerId) composite indexes; avoid N+1 queries via eager loading; SQL query plan analysis in dev |
| **Data Integrity** | Unit of Work pattern ensures atomic banner + component operations; database constraints enforce FK relationships |
| **Reliability** | Exception handling middleware catches all errors and returns standardized responses; structured logging (Serilog) |
| **Availability** | Stateless API enables deployment across multiple instances; no session state; ready for Azure AKS |

---

## Error Handling

| Error Type | Code | HTTP Status | Response |
|------------|------|------------|----------|
| **Validation Error** | VALIDATION_ERROR | 400 | `{ "error": { "code": "VALIDATION_ERROR", "message": "Invalid component size", "details": { "field": "width" } } }` |
| **Not Found** | BANNER_NOT_FOUND | 404 | `{ "error": { "code": "BANNER_NOT_FOUND", "message": "Banner not found" } }` |
| **Unauthorized** | UNAUTHORIZED | 401 | `{ "error": { "code": "UNAUTHORIZED", "message": "Invalid or missing authentication token" } }` |
| **Forbidden** | FORBIDDEN | 403 | `{ "error": { "code": "FORBIDDEN", "message": "Cannot modify published banner" } }` |
| **Conflict** | CONFLICT | 409 | `{ "error": { "code": "CONFLICT", "message": "Banner is published and cannot be deleted" } }` |
| **Server Error** | INTERNAL_ERROR | 500 | `{ "error": { "code": "INTERNAL_ERROR", "message": "An unexpected error occurred" } }` |

**Custom Exceptions**:
- `BannerValidationException` → 400 Bad Request
- `BannerNotFoundException` → 404 Not Found
- `UnauthorizedAccessException` → 401/403 Unauthorized/Forbidden
- `InvalidOperationException` → 409 Conflict
- `Exception` (generic) → 500 Internal Server Error

---

## External Dependencies

| Service | Purpose | Integration | Constraint |
|---------|---------|-------------|-----------|
| **Identity Service** | JWT token validation | HTTP Bearer token | Request header validation in middleware |
| **Shop Service** | Shop context resolution | Injected ShopContext | Implicit via ShopId in request |
| **Event Bus** (Future) | Publish domain events | Event aggregation | Events stored in-process for Bolt 002+ |

**For this bolt (001)**: No external service calls. ShopId extracted from JWT token claims.

---

## Deployment & Infrastructure

### Docker Container

**Dockerfile approach**:
- Base: `mcr.microsoft.com/dotnet/aspnet:8.0-latest`
- Build stage: Build and publish .NET application
- Runtime stage: Run published DLL
- Port: 5000 (HTTP) exposed
- Environment: Accepts `ConnectionString` and `IdentityServiceUrl` environment variables

### Kubernetes Deployment

**Pod Configuration**:
- Resource limits: 512Mi memory, 500m CPU
- Liveness probe: Health check endpoint at `/health`
- Readiness probe: Database connectivity check
- Startup probe: Gradual readiness for cold starts

**Service**: Cluster IP service on port 5000
**Ingress**: Azure Application Gateway routes `/api/banners/*` to this service

---

## Middleware & Observability

### Request Pipeline

1. CORS middleware (allow Banner Editor UI)
2. **ShopContextMiddleware** (extract ShopId from JWT claims)
3. Global exception handling middleware
4. Serilog structured logging enrichment
5. Routing → Controller action

### Logging Strategy

**Log Levels**:
- `Error`: Exceptions, validation failures, database errors
- `Warning`: Deprecated API usage, fallbacks
- `Information`: Banner created, component added, published
- `Debug`: API method entry/exit, state changes

**Structured Properties**:
- `ShopId`: Always included for filtering/correlation
- `BannerId`, `ComponentId`: For operation context
- `UserId`: For audit trail
- `RequestId`: For distributed tracing

**Local**: File-based logs (`logs/banner-{date}.log`)  
**Production**: Migrate to Azure Application Insights via Serilog sink

---

## Design Decisions Summary

✅ **Clean Architecture** separates concerns and enables testing  
✅ **Multi-tenant at repository layer** ensures data isolation  
✅ **Indexed database schema** supports performance targets  
✅ **Global exception middleware** standardizes error responses  
✅ **Stateless API** enables horizontal scaling  
✅ **Value Objects** for immutable component properties  
✅ **Domain events** prepared for downstream services (future bolts)
