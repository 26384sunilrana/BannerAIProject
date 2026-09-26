---
unit: 001-banner-service
bolt: 001-banner-service
stage: model
status: complete
created: 2026-09-26T00:00:00Z
---

# Static Model - Banner Service

## Bounded Context

The Banner Service manages the lifecycle of digital banners within a multi-tenant e-commerce platform. It provides core CRUD operations, component management, and preview functionality. Each banner is scoped to a specific shop (tenant), ensuring data isolation and compliance with multi-tenant architecture.

**Scope**: Banner creation, component management, preview configuration
**Out of Scope**: Version history, visual effects, media storage, UI rendering

---

## Domain Entities

| Entity | Properties | Business Rules |
|--------|------------|----------------|
| **Banner** | BannerId (Guid), ShopId (Guid), UserId (Guid), Name (string), Description (string), Width (int), Height (int), Components (Collection), CreatedAt (DateTime), UpdatedAt (DateTime), IsPublished (bool) | Banner must belong to a shop; cannot modify shop context after creation; component count cannot exceed 50; cannot delete if published |
| **Component** | ComponentId (Guid), ComponentType (enum: Text, Image, Video, Graphics), Position (int x, int y), Size (int width, int height), ZIndex (int), Properties (object), CreatedAt (DateTime), UpdatedAt (DateTime) | Each component must have valid position/size; ZIndex must be within valid range (0-100); component type determines available properties |

---

## Value Objects

| Value Object | Properties | Constraints |
|--------------|------------|-------------|
| **TextComponentProperties** | Content (string), FontFamily (string), FontSize (int), Color (hex string), FontWeight (enum), TextAlign (enum) | Content length max 500 chars; FontSize range 8-72px; Color must be valid hex |
| **ImageComponentProperties** | ImageReference (string URL), FilterEffect (string), Opacity (float), RotationDegrees (float) | ImageReference must be valid; Opacity range 0-1; RotationDegrees range -360 to 360 |
| **Position** | X (int), Y (int) | X, Y must be non-negative; must fit within banner boundaries |
| **Size** | Width (int), Height (int) | Width, Height must be positive; max 5000px each |
| **ShopContext** | ShopId (Guid) | Immutable; set at entity creation |

---

## Aggregates

| Aggregate Root | Members | Invariants |
|----------------|---------|------------|
| **Banner** | Components (Collection of Component), ShopContext | All components belong to this banner; total components ≤ 50; all components must have unique ZIndex in range; no circular references; ShopId consistency across all operations |
| **Shop** | BannerId (implicit via queries) | Data isolation: queries for banner CRUD MUST filter by ShopId; no cross-shop data access |

---

## Domain Events

| Event | Trigger | Payload |
|-------|---------|---------|
| **BannerCreated** | CreateBanner operation succeeds | BannerId, ShopId, UserId, Name, CreatedAt |
| **ComponentAdded** | AddComponent operation succeeds | BannerId, ComponentId, ComponentType, Position, Size |
| **ComponentUpdated** | UpdateComponent operation succeeds | BannerId, ComponentId, UpdatedProperties, UpdatedAt |
| **ComponentRemoved** | RemoveComponent operation succeeds | BannerId, ComponentId, RemovedAt |
| **BannerPublished** | Banner marked as published | BannerId, ShopId, PublishedAt |
| **BannerUnpublished** | Banner marked as unpublished | BannerId, ShopId, UnpublishedAt |
| **PreviewGenerated** | GetBannerPreview called | BannerId, PreviewConfig (complete banner configuration snapshot) |

---

## Domain Services

| Service | Operations | Dependencies |
|---------|------------|--------------|
| **BannerDomainService** | CreateBanner(shopId, userId, metadata), AddComponent(bannerId, component), UpdateComponent(bannerId, componentId, properties), RemoveComponent(bannerId, componentId), ValidateComponentZIndex(components), ValidateComponentCount(components) | IBannerRepository |
| **ComponentValidationService** | ValidateTextComponent(properties), ValidateImageComponent(properties), ValidateComponentBoundaries(position, size, bannerDimensions) | None (pure domain logic) |
| **PreviewService** | GeneratePreview(banner), BuildComponentStack(components) | None |

---

## Repository Interfaces

| Repository | Entity | Methods |
|------------|--------|---------|
| **IBannerRepository** | Banner | CreateAsync(banner), GetByIdAsync(bannerId, shopId), UpdateAsync(banner), DeleteAsync(bannerId, shopId), GetAllByShopAsync(shopId), GetByIdAndShopAsync(bannerId, shopId), ExistsAsync(bannerId, shopId) |
| **IComponentRepository** | Component | GetByBannerIdAsync(bannerId), CreateAsync(component), UpdateAsync(component), DeleteAsync(componentId), DeleteByBannerIdAsync(bannerId) |
| **IUnitOfWork** | Aggregate coordination | BeginTransactionAsync(), CommitAsync(), RollbackAsync(), SaveChangesAsync() |

---

## Ubiquitous Language

| Term | Definition |
|------|------------|
| **Banner** | A digital canvas containing positioned components (text, images, videos, graphics) that can be published and previewed |
| **Component** | A single element (text, image, video, or graphic) positioned on a banner with properties specific to its type |
| **Shop** | A tenant/account context within the multi-tenant system; provides data isolation boundary |
| **ShopContext** | The implicit multi-tenant boundary; all queries must filter by ShopId to ensure data isolation |
| **Component Type** | Classification of component content (Text, Image, Video, Graphics) determining available properties |
| **ZIndex** | Layering order of components on the banner; determines rendering stacking order |
| **Published** | State where banner is locked and ready for distribution/preview; prevents modifications |
| **Preview** | Complete configuration snapshot of a banner including all components and their properties |
| **Aggregate Root** | Entity that serves as the entry point for transactional boundaries (Banner for this context) |
| **Value Object** | Immutable object identified by its properties, not a unique identity (Position, Size, TextComponentProperties) |
| **Domain Service** | Operation that doesn't naturally belong to an entity (component validation, preview generation) |
| **Repository** | Data access abstraction providing collection-like interface to aggregates |
| **Unit of Work** | Transaction coordination across multiple repositories |

---

## Coverage Against Stories

✅ **Story 001-create-banner**: Banner entity with metadata (name, description, dimensions), CreateBanner operation defined  
✅ **Story 002-add-text-component**: TextComponentProperties value object with text-specific fields, AddComponent operation defined  
✅ **Story 003-add-image-component**: ImageComponentProperties value object with image-specific fields, Component entity supports multiple types

---

## Design Notes

- **Multi-tenancy**: ShopId is enforced at repository level; all queries implicitly filter by ShopId
- **Entity vs Value Object**: Components modeled as entities (have identity via ComponentId) rather than pure value objects, enabling independent updates
- **Aggregate Boundary**: Banner is the aggregate root; components are children within the aggregate
- **Extension Point**: ComponentType enum allows future addition of Video and Graphics without structural changes
- **Events**: Domain events capture state transitions for downstream services (version control, effects engine)
- **Validation**: Separated into ComponentValidationService for reuse across create/update paths
- **Immutability**: ZIndex, ComponentType, and Position designed for immutability; updates create new instances
