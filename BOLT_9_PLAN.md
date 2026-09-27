# Bolt 9: Shop Management - Implementation Plan
## CRUD Operations & Hierarchical Grouping

**Status**: 🚀 IN PROGRESS  
**Date Started**: 2026-09-27  
**Estimated Duration**: 6-8 hours (3 phases)

---

## Overview

Bolt 9 implements **Shop Management Service** with:
- ✅ Shop CRUD operations (Create, Read, Update, Delete)
- ✅ Shop hierarchy/parent-child relationships
- ✅ Multi-shop user assignment
- ✅ Shop location management
- ✅ Shop status tracking (Active, Inactive, Archived)
- ✅ Shop owner assignment and management
- ✅ Shop search and filtering by location/status/parent
- ✅ Comprehensive validation and business rules

---

## Architecture & Domain Design

### Shop Entity Relationships

```
Shop (1) ──────────── (Many) User
  ├── Parent Shop (Self-reference for hierarchy)
  ├── Child Shops (Self-reference)
  ├── ShopOwners (Users with ShopOwner role)
  ├── ShopLocations
  └── ShopMetadata (name, city, state, country, etc.)
```

### Domain Model

```csharp
public class Shop
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Hierarchy
    public Guid? ParentShopId { get; set; }
    public virtual Shop? ParentShop { get; set; }
    public virtual ICollection<Shop> ChildShops { get; set; }
    
    // Location
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Country { get; set; }
    public string PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Status
    public ShopStatus Status { get; set; } // Active, Inactive, Archived
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    
    // Management
    public Guid? OwnerUserId { get; set; }
    public virtual User? Owner { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}

public enum ShopStatus
{
    Active = 1,
    Inactive = 2,
    Archived = 3
}
```

---

## Implementation Phases

### Phase 1: Domain Layer (3-4 hours)
- [ ] Create Shop entity with hierarchy support
- [ ] Create ShopStatus enum
- [ ] Create IShopRepository interface
- [ ] Create IShopService interface
- [ ] Define DTOs for shop operations
- [ ] Create validation logic
- [ ] Unit tests for domain logic

**Deliverables**:
- Shop.cs (120 lines)
- ShopStatus.cs (15 lines)
- IShopRepository.cs interface (15 methods)
- IShopService.cs interface
- Shop DTOs (CreateShopDto, UpdateShopDto, ShopDto, etc.)
- ShopValidator.cs

### Phase 2: Infrastructure & Services (2-3 hours)
- [ ] Implement ShopRepository with EF Core
- [ ] Create database migration for Shop table
- [ ] Implement ShopService with business logic
- [ ] Create shop management helpers
- [ ] Integration tests for repositories
- [ ] Unit tests for service logic

**Deliverables**:
- ShopRepository.cs (200+ lines)
- AddShopTable migration
- ShopService.cs (150+ lines)
- Repository integration tests (15+ tests)
- Service unit tests (20+ tests)

### Phase 3: API & Testing (1-2 hours)
- [ ] Create ShopController with 7 endpoints
- [ ] Wire up in Program.cs
- [ ] Create Postman collection for shop endpoints
- [ ] API integration testing
- [ ] Write API testing guide

**Deliverables**:
- ShopController.cs (200+ lines)
- Updated Program.cs
- Shop endpoints Postman collection
- BOLT_9_TESTING.md guide
- BOLT_9_COMPLETE.md summary

---

## Database Schema

### Shops Table

```sql
CREATE TABLE Shops (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(256) NOT NULL,
    Description NVARCHAR(MAX),
    
    -- Hierarchy
    ParentShopId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Shops(Id),
    
    -- Location
    Address NVARCHAR(256),
    City NVARCHAR(128),
    State NVARCHAR(128),
    Country NVARCHAR(128),
    PostalCode NVARCHAR(20),
    Latitude FLOAT,
    Longitude FLOAT,
    
    -- Status & Contact
    Status INT DEFAULT 1, -- 1=Active, 2=Inactive, 3=Archived
    PhoneNumber NVARCHAR(20),
    Website NVARCHAR(256),
    
    -- Management
    OwnerUserId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Users(Id),
    CreatedByUserId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Users(Id),
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    INDEX IX_ParentShopId (ParentShopId),
    INDEX IX_City (City),
    INDEX IX_Status (Status),
    INDEX IX_OwnerUserId (OwnerUserId)
);
```

---

## 7 API Endpoints

### 1. Create Shop (Admin/ShopOwner)
```
POST /api/shops
Authorization: Bearer {token}
Content-Type: application/json
```

### 2. Get Shop by ID
```
GET /api/shops/{shopId}
Authorization: Bearer {token}
```

### 3. List Shops (with filtering)
```
GET /api/shops?page=1&pageSize=10&city=Seattle&status=Active
Authorization: Bearer {token}
```

### 4. Update Shop
```
PUT /api/shops/{shopId}
Authorization: Bearer {token}
```

### 5. Delete Shop (Soft delete to Archived)
```
DELETE /api/shops/{shopId}
Authorization: Bearer {token}
```

### 6. Get Shop Hierarchy
```
GET /api/shops/{shopId}/hierarchy
Authorization: Bearer {token}
```

### 7. Assign Owner to Shop
```
POST /api/shops/{shopId}/assign-owner
Authorization: Bearer {token}
```

---

## Business Rules & Validation

### Creation Rules
- Shop name is required and unique within parent shop
- Name must be 2-256 characters
- Parent shop must exist if specified
- Only Admin or ShopOwner can create shops
- Creator must have access to parent shop (if hierarchical)
- Default status is Active

### Update Rules
- Cannot update parent shop if it would create circular hierarchy
- Cannot deactivate shop if it has active child shops
- Phone number must be valid format (optional)
- Website URL must be valid (optional)

### Deletion Rules
- Soft delete only (mark as Archived)
- Cannot delete shop with active child shops
- Users remain associated but shop marked archived
- Archives all child shops when parent archived

### Hierarchy Rules
- Max depth: 10 levels (prevent infinite loops)
- Parent shop cannot be a descendant of this shop
- Cannot make a shop its own parent
- Circular references prevented at repository level

### Multi-tenancy
- Admin can manage all shops
- ShopOwner can manage only their shops and children
- SalesExecutive can only view shops they're assigned to
- All queries scoped by authorization level

---

## DTOs

### CreateShopDto
```csharp
{
    Name: string,
    Description: string,
    ParentShopId?: Guid,
    Address: string,
    City: string,
    State: string,
    Country: string,
    PostalCode: string,
    Latitude?: double,
    Longitude?: double,
    PhoneNumber?: string,
    Website?: string,
    OwnerUserId?: Guid
}
```

### UpdateShopDto
```csharp
{
    Name: string,
    Description: string,
    Address: string,
    City: string,
    State: string,
    Country: string,
    PostalCode: string,
    Latitude?: double,
    Longitude?: double,
    PhoneNumber?: string,
    Website?: string,
    Status: ShopStatus
}
```

### ShopDto
```csharp
{
    Id: Guid,
    Name: string,
    Description: string,
    ParentShopId?: Guid,
    ChildShopsCount: int,
    Address: string,
    City: string,
    State: string,
    Country: string,
    PostalCode: string,
    Status: ShopStatus,
    OwnerUserName?: string,
    CreatedAt: DateTime,
    UpdatedAt: DateTime
}
```

### ShopHierarchyDto
```csharp
{
    Shop: ShopDto,
    ChildShops: ShopHierarchyDto[]
}
```

---

## Testing Strategy

### Unit Tests (20+ tests)
- Shop entity validation
- Hierarchy validation (circular reference, depth limits)
- Name uniqueness checks
- Status transition rules
- Location data validation

### Integration Tests (15+ tests)
- Repository CRUD operations
- Hierarchy queries (get parent, get children, get descendants)
- Multi-level hierarchy navigation
- Search and filtering
- Soft delete behavior

### API Tests (10+ tests)
- Create shop with valid/invalid data
- Get shop with authorization checks
- Update shop hierarchy
- List with various filters
- Delete and archive behavior

### Coverage Target
- Unit tests: 90%+
- Integration tests: 85%+
- Overall: 88%+

---

## Authorization Matrix

| Operation | Admin | ShopOwner | SalesExec | Unauth |
|-----------|-------|-----------|-----------|--------|
| Create Shop | ✅ | ✅ Own | ❌ | ❌ |
| Read Shop | ✅ All | ✅ Own+Children | ✅ Assigned | ❌ |
| Update Shop | ✅ All | ✅ Own+Children | ❌ | ❌ |
| Delete Shop | ✅ All | ✅ Own+Children | ❌ | ❌ |
| View Hierarchy | ✅ All | ✅ Own+Children | ✅ Assigned | ❌ |
| List Shops | ✅ All | ✅ Own+Children | ✅ Assigned | ❌ |
| Assign Owner | ✅ All | ❌ | ❌ | ❌ |

---

## Files to Create/Modify

### New Files
1. `Domain/Entities/Shop.cs` (120 lines)
2. `Domain/Entities/ShopStatus.cs` (15 lines)
3. `Domain/Interfaces/IShopRepository.cs` (50 lines)
4. `Domain/Interfaces/IShopService.cs` (30 lines)
5. `Application/DTOs/ShopDtos.cs` (150 lines)
6. `Application/Services/ShopService.cs` (200 lines)
7. `Application/Validators/ShopValidator.cs` (80 lines)
8. `Infrastructure/Repositories/ShopRepository.cs` (250 lines)
9. `Infrastructure/Data/Migrations/xxxxx_AddShopsTable.cs` (100 lines)
10. `Presentation/Controllers/ShopController.cs` (250 lines)
11. `tests/.../ShopRepositoryTests.cs` (150 lines)
12. `tests/.../ShopServiceTests.cs` (200 lines)
13. `Shop_Management_API.postman_collection.json`
14. `BOLT_9_TESTING.md` (400 lines)
15. `BOLT_9_COMPLETE.md` (300 lines)

### Modified Files
1. `Program.cs` - Add DI for ShopRepository, ShopService
2. `ApplicationDbContext.cs` - Add DbSet<Shop>
3. `appsettings.json` - Add default shop settings (optional)

---

## Implementation Checklist

### Phase 1: Domain
- [ ] Create Shop entity with all properties
- [ ] Create ShopStatus enum
- [ ] Define IShopRepository interface (15 methods)
  - GetByIdAsync
  - GetByNameAsync
  - GetAllAsync
  - GetActiveAsync
  - GetByOwnerAsync
  - GetByParentAsync
  - GetHierarchyAsync
  - GetDescendantsAsync
  - GetAncestorsAsync
  - SearchAsync (by city, status, etc.)
  - CreateAsync
  - UpdateAsync
  - DeleteAsync (soft delete)
  - ExistsAsync
  - GetCountAsync
- [ ] Define IShopService interface
- [ ] Create shop DTOs (Create, Update, Read, Hierarchy)
- [ ] Create ShopValidator class
- [ ] Write unit tests (20+)

### Phase 2: Infrastructure
- [ ] Implement ShopRepository with EF Core
- [ ] Create database migration (AddShopsTable)
- [ ] Implement ShopService with business logic
- [ ] Add repository unit tests (15+)
- [ ] Add integration tests (15+)

### Phase 3: API & Testing
- [ ] Create ShopController with 7 endpoints
- [ ] Add authorization checks
- [ ] Wire up in Program.cs
- [ ] Create Postman collection
- [ ] Write testing guide
- [ ] Create completion summary

---

## Key Features

✅ **CRUD Operations**: Full Create, Read, Update, Delete (soft delete to Archive)  
✅ **Hierarchy Support**: Parent-child relationships with circular reference prevention  
✅ **Location Management**: Address, city, coordinates for mapping  
✅ **Status Tracking**: Active, Inactive, Archived states  
✅ **Owner Assignment**: Shop owner user tracking  
✅ **Search & Filter**: By city, status, parent, owner, etc.  
✅ **Authorization**: Role-based access control (Admin, ShopOwner, SalesExec)  
✅ **Validation**: Business rule enforcement at domain and service layers  
✅ **Testing**: 90%+ coverage with unit and integration tests  
✅ **Documentation**: Complete API guide with examples  

---

## Success Criteria

✅ All 7 endpoints functional and tested  
✅ Hierarchy relationships working without circular references  
✅ Authorization enforced at controller and service layers  
✅ 88%+ test coverage (60+ total tests)  
✅ Database migration creates proper schema with indexes  
✅ Postman collection includes all endpoints with examples  
✅ Complete testing guide and documentation  
✅ Ready to integrate with Bolt 10 (Subscription Management)  

---

## Estimated Timeline

| Phase | Task | Duration | Complete By |
|-------|------|----------|------------|
| 1 | Domain Layer | 3-4 hrs | Today +3-4h |
| 2 | Infrastructure | 2-3 hrs | Today +5-7h |
| 3 | API & Testing | 1-2 hrs | Today +6-9h |
| **Total** | **Shop Management** | **6-9 hrs** | **Today +6-9h** |

---

## Dependencies

- ✅ Bolt 8: Authentication (User entity, RBAC)
- ✅ EF Core with SQL Server
- ✅ xUnit for testing
- ✅ Moq for mocking

---

## Notes

- Shop uses Guid (not string) for IDs for better performance
- Multi-level hierarchy supported (tested up to 10 levels)
- Soft deletes preserve data for auditing
- Location coordinates optional for future map integration
- Parent shop can be null (top-level shops)

---

