---
unit: 001-banner-service
bolt: 003-banner-service
stage: model
status: complete
created: 2026-09-26T00:00:00Z
---

# Static Model - Banner Service (Bolt 003: Z-index & Preview)

## Bounded Context

Completes Banner Service component system by adding z-index reordering operations and comprehensive preview functionality. Builds on Bolts 001-002 (Banner, Component entities; Text, Image, Video, Graphics types). This bolt adds explicit layer management and preview generation operations.

**Scope**: Z-index reordering, preview configuration generation, component layering operations  
**Builds On**: Bolts 001-002 (all 4 component types)  
**Out of Scope**: Rendering, visual effects (Effects Engine)

---

## Domain Entities

### Banner (Existing - Enhanced)
- Properties: All from Bolt 001
- New behavior: Supports z-index reordering operations
- New invariants: Component z-index values must remain unique during reordering

### Component (Existing - Reused)
- Properties: All from Bolts 001-002
- No changes to structure
- Used as input/output for z-index and preview operations

---

## Value Objects

### Existing Value Objects
- **Position, Size** (Bolt 001)
- **TextComponentProperties, ImageComponentProperties** (Bolt 001)
- **VideoComponentProperties, GraphicsComponentProperties** (Bolt 002)

### New Value Objects

**LayerOrder**
- Properties:
  - ComponentId (Guid)
  - OldZIndex (int) - Previous z-index value
  - NewZIndex (int) - New z-index value
  - Reason (string) - Why layer changed (e.g., "move-forward", "move-backward", "send-to-front", "send-to-back")
- Constraints:
  - OldZIndex ≠ NewZIndex
  - Both within 0-100 range
  - Reason must be one of: move-forward, move-backward, send-to-front, send-to-back

**PreviewConfiguration**
- Properties:
  - BannerId (Guid)
  - ShopId (Guid)
  - Name (string)
  - Dimensions (Size)
  - Components (List of PreviewComponent) - Sorted by z-index ascending
  - GeneratedAt (DateTime)
- Constraints:
  - Components immutable (read-only list)
  - Always sorted by z-index (0 at bottom, 100 at top)
  - Includes all component types with their properties

**PreviewComponent** (Value Object)
- Properties:
  - ComponentId (Guid)
  - ComponentType (enum)
  - Position (Position value object)
  - Size (Size value object)
  - ZIndex (int)
  - Properties (object) - Generic properties based on type
- Constraints:
  - Represents component state at time of preview generation

---

## Aggregates

**Banner** (Aggregate Root - Enhanced)
- Root: Banner entity
- Members: Component collection
- New operations:
  - ReorderComponent(componentId, newZIndex) - Change component layer
  - MoveForward(componentId) - Increment z-index
  - MoveBackward(componentId) - Decrement z-index
  - SendToFront(componentId) - Set z-index to 100
  - SendToBack(componentId) - Set z-index to 0
- Invariants:
  - Existing: component count ≤ 50, unique z-index, shop isolation
  - New: z-index reordering maintains uniqueness

---

## Domain Events

### Existing Events (Bolts 001-002)
- BannerCreated, ComponentAdded, ComponentUpdated, ComponentRemoved
- BannerPublished, BannerUnpublished, PreviewGenerated
- VideoComponentAdded, GraphicsComponentAdded

### New Events (Bolt 003)

**ComponentLayerChanged**
- Trigger: Z-index reordering completes
- Payload: BannerId, ComponentId, OldZIndex, NewZIndex, ChangedAt

**ComponentMovedForward**
- Trigger: MoveForward operation
- Payload: BannerId, ComponentId, OldZIndex, NewZIndex

**ComponentMovedBackward**
- Trigger: MoveBackward operation
- Payload: BannerId, ComponentId, OldZIndex, NewZIndex

**ComponentSentToFront**
- Trigger: SendToFront operation
- Payload: BannerId, ComponentId, PreviousZIndex, NewZIndex

**ComponentSentToBack**
- Trigger: SendToBack operation
- Payload: BannerId, ComponentId, PreviousZIndex, NewZIndex

**PreviewGenerated** (Enhanced)
- Trigger: GetBannerPreview called
- Payload: BannerId, ShopId, ComponentCount, GeneratedAt, ComponentsSortedByZIndex

---

## Domain Services

### Existing Services (Bolts 001-002)
- **BannerDomainService** - CRUD operations
- **ComponentValidationService** - Type-specific validation

### New Service (Bolt 003)

**LayerManagementService**
- Operations:
  - **ReorderComponent**(banner, componentId, newZIndex) → LayerOrder
    - Validates newZIndex uniqueness
    - Swaps z-index with existing component if needed
    - Returns LayerOrder describing change
    
  - **MoveComponentForward**(banner, componentId) → LayerOrder
    - Finds next available z-index above current
    - Reorders to move component forward one layer
    - Returns LayerOrder with delta
    
  - **MoveComponentBackward**(banner, componentId) → LayerOrder
    - Finds next available z-index below current
    - Reorders to move component backward one layer
    - Returns LayerOrder with delta
    
  - **SendComponentToFront**(banner, componentId) → LayerOrder
    - Sets z-index to 100
    - Shifts other components if needed
    - Returns LayerOrder
    
  - **SendComponentToBack**(banner, componentId) → LayerOrder
    - Sets z-index to 0
    - Shifts other components if needed
    - Returns LayerOrder

**PreviewService** (Enhanced from Bolt 001)
- Operations:
  - **GeneratePreview**(banner) → PreviewConfiguration
    - Sorts components by z-index (ascending: 0 to 100)
    - Creates PreviewConfiguration with all component data
    - Returns immutable preview snapshot
    - Includes all 4 component types with properties

---

## Repository Interfaces

No new interfaces. Existing repositories handle all operations via Component entity and Banner aggregate updates.

---

## Ubiquitous Language (Extended)

### New Terms

| Term | Definition |
|------|-----------|
| **Z-index** | Numeric value (0-100) determining component rendering order; 0 = back, 100 = front |
| **Layer** | Conceptual position in component stack determined by z-index |
| **Move Forward** | Increment component's z-index to move it one layer toward front |
| **Move Backward** | Decrement component's z-index to move it one layer toward back |
| **Send to Front** | Set component's z-index to 100, bringing it to front |
| **Send to Back** | Set component's z-index to 0, sending it to back |
| **Reorder** | Change component's z-index (from any value to any other valid value) |
| **Layer Conflict** | Situation where two components would have same z-index (prevented by validation) |
| **Preview Configuration** | Snapshot of banner state with all components sorted by z-index for rendering |
| **Component Stack** | Ordered collection of components from z-index 0 (bottom) to 100 (top) |

---

## Design Notes

- **Immutable Preview**: PreviewConfiguration is read-only snapshot; changes to banner don't affect existing previews
- **Z-index Gaps**: Allowing gaps in z-index sequence (0, 1, 50, 100) for flexibility
- **Reordering Strategy**: When setting z-index to occupied value, lower the component that was there (shift down)
- **Layer Operations**: MoveForward/Backward/SendToFront/SendToBack are convenience operations using ReorderComponent
- **Preview Sorting**: Always sort by z-index ascending (0 at index 0, 100 at index 50)
- **No Circular References**: Preview contains snapshots, not references to live components

---

## Coverage Against Stories

✅ **Story 007-manage-component-z-index**: Z-index management operations (Move, Send, Reorder)  
✅ **Story 008-preview-banner-configuration**: PreviewConfiguration generation with sorted components  

---

## Relationship to Bolts 001-002

**Builds On**:
- Banner entity and aggregate root (Bolt 001)
- Component entity with all 4 types (Bolts 001-002)
- Existing repositories and services

**Completes**:
- Component lifecycle (add, update, reorder, remove)
- Banner preview functionality
- Multi-type component system

**Maintains**:
- Multi-tenant isolation
- Component validation
- 50-component limit
- JSON property storage (ADR-002)

---

## Acceptance Criteria Validation

### Story 007: Manage Component Z-index
- [ ] MoveForward operation increases z-index
- [ ] MoveBackward operation decreases z-index
- [ ] SendToFront sets z-index to 100
- [ ] SendToBack sets z-index to 0
- [ ] Z-index uniqueness maintained after reordering
- [ ] Components cannot occupy same z-index

### Story 008: Preview Banner Configuration
- [ ] PreviewConfiguration includes all components
- [ ] Components sorted by z-index (ascending)
- [ ] All component types (Text, Image, Video, Graphics) included
- [ ] Properties included for each component type
- [ ] Preview is immutable snapshot
- [ ] Timestamp indicates when preview was generated
