# DDD-01: Domain Model — Banner Editor UI

**Status**: Stage 1 of 5 (Domain Model)  
**Bolt**: 007-banner-editor-ui  
**Created**: 2026-09-26  
**Technology**: Next.js 14, TypeScript, React 18, Tailwind CSS, react-dnd

---

## Problem Statement

Need a visual interface for non-technical users to:
1. Create and edit banners with drag-and-drop
2. Add, position, and resize components
3. Configure component properties (text, image, video, graphics)
4. Manage z-index and effects
5. Preview and save changes

---

## Domain Concepts

### Core UI Models

#### Canvas State
Represents the editing workspace state.

**State**:
- `bannerId`: Current banner being edited
- `canvasWidth`: Physical canvas width (e.g., 1200px)
- `canvasHeight`: Physical canvas height (e.g., 600px)
- `scale`: Zoom level (0.5 to 2.0, default 1.0)
- `selectedComponentId`: Currently selected component (for property editing)
- `isDirty`: Has unsaved changes
- `isPreviewMode`: Toggle between edit and preview
- `gridSize`: Grid snap size (8px, 16px, 32px)
- `showGrid`: Whether to display grid
- `showGuidelines`: Whether to show alignment guides

**Behavior**:
- Tracks which component is selected
- Maintains zoom level
- Detects unsaved changes
- Toggles between edit and preview modes

---

#### Component Placement
Represents a component on the canvas.

**Fields**:
- `id`: Component ID (from backend)
- `type`: ComponentType (1=Text, 2=Image, 3=Video, 4=Graphics)
- `x`: Position X (pixels)
- `y`: Position Y (pixels)
- `width`: Width (pixels)
- `height`: Height (pixels)
- `zIndex`: Layer order (0-100)
- `rotation`: Rotation in degrees
- `opacity`: Opacity 0-1
- `properties`: Type-specific properties (from backend)

**Invariants**:
- Position within canvas bounds
- Size within reasonable limits
- Z-index 0-100

---

#### Toolbar
Component types available for addition to canvas.

**Components**:
1. **Text Component**
   - Icon: "A"
   - Color: Blue
   - Default size: 200×50px
   - Editable properties: content, fontSize, color, fontFamily

2. **Image Component**
   - Icon: Image/picture icon
   - Color: Green
   - Default size: 300×300px
   - Editable properties: URL, opacity, rotation

3. **Video Component**
   - Icon: Play button
   - Color: Purple
   - Default size: 400×300px
   - Editable properties: URL, autoplay, muted, volume

4. **Graphics Component**
   - Icon: Shape/polygon icon
   - Color: Orange
   - Default size: 250×250px
   - Editable properties: shape, fillColor, borderColor

**Behavior**:
- Drag from toolbar to canvas creates component
- Each type has default properties
- Properties validated against backend constraints

---

#### Property Inspector
Interface for editing selected component properties.

**States**:
1. **Nothing Selected** → "Select a component to edit"
2. **Text Selected** → Show text properties (content, fontSize, color, fontFamily)
3. **Image Selected** → Show image properties (URL, opacity, rotation)
4. **Video Selected** → Show video properties (URL, autoplay, muted, volume)
5. **Graphics Selected** → Show graphics properties (shape, colors, borders)

**Common Properties** (all components):
- Position X, Y (pixels)
- Width, Height (pixels)
- Z-Index (0-100 with up/down buttons)
- Effects (list of applied effects)
- Rotation (if applicable)

**Validation**:
- X, Y: 0 to canvas dimensions
- Width, Height: 10 to canvas dimensions
- Z-Index: 0-100
- Properties: Match backend constraints

---

#### Edit Workflows

##### Add Component
1. User clicks toolbar component icon
2. Default component created on canvas (centered)
3. Component selected automatically
4. Property inspector opens
5. User adjusts properties

##### Reposition Component
1. User clicks component on canvas
2. Component selected (shows resize handles)
3. User drags component to new position
4. Canvas snaps to grid if enabled
5. Position values update in property inspector

##### Resize Component
1. Component selected (shows resize handles)
2. User drags corner or edge handle
3. Dimensions update in real-time
4. Aspect ratio can be locked
5. Property inspector shows new dimensions

##### Edit Properties
1. Component selected
2. User edits values in property inspector
3. Changes reflect on canvas in real-time
4. Validation runs on blur
5. Backend constraints enforced

##### Delete Component
1. Component selected
2. User clicks Delete button or presses Delete key
3. Component removed from canvas
4. Selection cleared
5. Change marked as unsaved

##### Layer Management
1. User selects component
2. Clicks layer buttons in property inspector:
   - Up arrow: Move forward (increment z-index)
   - Down arrow: Move backward (decrement z-index)
   - Top arrow: Send to front (z-index = 100)
   - Bottom arrow: Send to back (z-index = 0)

---

## UI Component Hierarchy

```
App
├── Header
│   ├── BannerTitle
│   ├── SaveButton
│   └── PreviewToggle
├── MainLayout
│   ├── Sidebar (Toolbar)
│   │   ├── TextComponentButton
│   │   ├── ImageComponentButton
│   │   ├── VideoComponentButton
│   │   └── GraphicsComponentButton
│   ├── CanvasArea
│   │   ├── Canvas
│   │   │   ├── Grid (optional)
│   │   │   ├── Guidelines (optional)
│   │   │   └── PlacedComponent (repeating)
│   │   │       ├── ResizeHandles
│   │   │       └── SelectionBox
│   │   └── ZoomControls
│   └── PropertyPanel
│       ├── PropertyInspector
│       │   ├── PositionProperties
│       │   ├── SizeProperties
│       │   ├── LayerProperties
│       │   ├── CommonProperties
│       │   └── TypeSpecificProperties
│       └── VersionHistory (collapsible)
├── ConfirmDialog (for unsaved changes)
└── NotificationToast (for save feedback)
```

---

## State Management Model

### EditorContext
Global state for banner editor.

```typescript
interface EditorState {
  // Canvas
  bannerId: string
  canvasWidth: number
  canvasHeight: number
  scale: number
  gridSize: number
  showGrid: boolean
  showGuidelines: boolean
  
  // Selection
  selectedComponentId: string | null
  
  // Editing
  isDirty: boolean
  isPreviewMode: boolean
  isLoading: boolean
  error: string | null
  
  // Components
  components: PlacedComponent[]
  
  // Undo/Redo
  history: EditorState[]
  historyIndex: number
}
```

### Actions
- `selectComponent(id)`
- `addComponent(type)`
- `deleteComponent(id)`
- `updateComponentPosition(id, x, y)`
- `updateComponentSize(id, width, height)`
- `updateComponentProperty(id, property, value)`
- `reorderComponent(id, newZIndex)`
- `saveBanner()`
- `loadBanner(id)`
- `togglePreviewMode()`
- `undo()`
- `redo()`

---

## API Integration Points

### Backend Services Consumed

**Banner Service**:
- GET `/api/banners/{bannerId}` — Load banner
- PUT `/api/banners/{bannerId}` — Save banner
- POST `/api/banners/{bannerId}/components` — Add component
- PUT `/api/banners/{bannerId}/components/{componentId}` — Update component
- DELETE `/api/banners/{bannerId}/components/{componentId}` — Remove component

**Layer Management**:
- POST `/api/banners/{bannerId}/components/{componentId}/reorder` — Change z-index
- POST `/api/banners/{bannerId}/components/{componentId}/move-forward` — Layer up
- POST `/api/banners/{bannerId}/components/{componentId}/move-backward` — Layer down
- POST `/api/banners/{bannerId}/components/{componentId}/send-to-front` — To front
- POST `/api/banners/{bannerId}/components/{componentId}/send-to-back` — To back

**Version Control**:
- GET `/api/banners/{bannerId}/versions` — Version history
- POST `/api/banners/{bannerId}/versions/{versionNumber}/restore` — Restore version

**Media Service**:
- POST `/api/media/upload/initialize` — Start upload
- PUT `/api/media/{mediaFileId}/chunks/{chunkNumber}` — Upload chunk
- POST `/api/media/{mediaFileId}/complete` — Finish upload

**Effects**:
- POST `/api/banners/{bannerId}/components/{componentId}/effects` — Add effect
- PUT `/api/banners/{bannerId}/components/{componentId}/effects/{effectId}` — Update effect
- DELETE `/api/banners/{bannerId}/components/{componentId}/effects/{effectId}` — Remove effect

---

## Interaction Flows

### F1: Creating a Banner Component
```
1. User clicks "Text" button in toolbar
2. Dialog or form appears with defaults
3. User adjusts default properties if needed
4. Clicks "Add" → API call to POST /api/banners/{bannerId}/components
5. Component appears on canvas, selected
6. Property inspector opens
```

### F2: Moving Component with Drag-Drop
```
1. User clicks component on canvas
2. Component selected (shows handles)
3. User drags component
4. Position updates in real-time (optimistic UI)
5. On drop, API call to PUT /api/banners/{bannerId}/components/{componentId}
6. If error, position reverts
7. Property inspector updates with new coordinates
```

### F3: Resizing Component
```
1. Component selected (shows resize handles)
2. User drags resize handle
3. Width/height update in real-time
4. On release, API call to PUT /api/banners/{bannerId}/components/{componentId}
5. Property inspector reflects new size
```

### F4: Saving Banner
```
1. User clicks "Save" button
2. Button shows loading state
3. All changes batched into one API call
4. Success: dismiss loading, mark as clean
5. Error: show error toast, keep dirty state
6. Auto-save every 30s if dirty
```

### F5: Version History
```
1. User clicks "History" in property panel
2. List of versions appears with timestamps
3. User selects version to restore
4. Confirmation dialog appears
5. User confirms → API call to restore
6. Canvas reloads with restored state
```

---

## Design Principles

✅ **Real-time Feedback** — Changes visible immediately on canvas  
✅ **Drag-and-Drop** — Familiar, intuitive interactions  
✅ **Property Inspector** — All editing in one panel  
✅ **Grid & Guides** — Precise alignment and positioning  
✅ **Undo/Redo** — Easy recovery from mistakes  
✅ **Auto-save** — Prevent data loss  
✅ **Responsive Canvas** — Zoom and pan support  
✅ **Validation** — Backend constraints enforced in UI  
✅ **Optimistic Updates** — Fast UI feedback  
✅ **Error Handling** — Clear error messages  

---

## Next: Technical Design (Stage 2)

Component architecture, state management, API client, hooks design.
