# DDD-02: Technical Design — Banner Editor UI

**Status**: Stage 2 of 5 (Technical Design)  
**Bolt**: 007-banner-editor-ui  
**Created**: 2026-09-26  

---

## Tech Stack

- **Framework**: Next.js 14 (App Router)
- **Language**: TypeScript (strict mode)
- **Styling**: Tailwind CSS + custom CSS modules
- **Drag-Drop**: react-beautiful-dnd (or react-dnd)
- **State Management**: React Context + hooks
- **HTTP Client**: axios with interceptors
- **Form Handling**: React Hook Form
- **UI Components**: Headless UI + custom components
- **Testing**: Jest + React Testing Library

---

## Project Structure

```
banner-editor/
├── src/
│   ├── app/
│   │   ├── layout.tsx
│   │   ├── page.tsx
│   │   ├── banners/
│   │   │   └── [bannerId]/
│   │   │       └── editor/
│   │   │           └── page.tsx
│   │   └── globals.css
│   ├── components/
│   │   ├── Canvas/
│   │   │   ├── Canvas.tsx
│   │   │   ├── Grid.tsx
│   │   │   ├── PlacedComponent.tsx
│   │   │   ├── ResizeHandles.tsx
│   │   │   └── Canvas.module.css
│   │   ├── Toolbar/
│   │   │   ├── Toolbar.tsx
│   │   │   ├── ComponentButton.tsx
│   │   │   └── Toolbar.module.css
│   │   ├── PropertyPanel/
│   │   │   ├── PropertyPanel.tsx
│   │   │   ├── TextProperties.tsx
│   │   │   ├── ImageProperties.tsx
│   │   │   ├── VideoProperties.tsx
│   │   │   ├── GraphicsProperties.tsx
│   │   │   ├── LayerProperties.tsx
│   │   │   └── PropertyPanel.module.css
│   │   ├── Header/
│   │   │   ├── Header.tsx
│   │   │   ├── SaveButton.tsx
│   │   │   └── Header.module.css
│   │   └── Common/
│   │       ├── Input.tsx
│   │       ├── Button.tsx
│   │       ├── Select.tsx
│   │       └── ConfirmDialog.tsx
│   ├── hooks/
│   │   ├── useEditor.ts
│   │   ├── useCanvas.ts
│   │   ├── useSelection.ts
│   │   ├── useComponentDrag.ts
│   │   ├── useResize.ts
│   │   ├── useSave.ts
│   │   └── useUndo.ts
│   ├── context/
│   │   ├── EditorContext.tsx
│   │   └── EditorProvider.tsx
│   ├── api/
│   │   ├── client.ts
│   │   ├── bannerService.ts
│   │   ├── componentService.ts
│   │   ├── layerService.ts
│   │   ├── mediaService.ts
│   │   └── effectsService.ts
│   ├── types/
│   │   ├── editor.ts
│   │   ├── banner.ts
│   │   ├── component.ts
│   │   └── api.ts
│   ├── utils/
│   │   ├── validation.ts
│   │   ├── positioning.ts
│   │   ├── grid.ts
│   │   └── transforms.ts
│   └── styles/
│       ├── colors.css
│       ├── layout.css
│       └── animations.css
├── __tests__/
│   ├── components/
│   │   ├── Canvas.test.tsx
│   │   ├── PlacedComponent.test.tsx
│   │   ├── PropertyPanel.test.tsx
│   │   └── Toolbar.test.tsx
│   └── hooks/
│       ├── useEditor.test.ts
│       ├── useSelection.test.ts
│       └── useSave.test.ts
├── package.json
├── tsconfig.json
├── tailwind.config.js
├── next.config.js
└── .env.local
```

---

## Core Components

### Canvas Component
Main editing surface where components are placed.

```typescript
interface CanvasProps {
  bannerId: string
  canvasWidth: number
  canvasHeight: number
}

<Canvas
  bannerId={bannerId}
  canvasWidth={1200}
  canvasHeight={600}
/>
```

**Features**:
- Renders banner frame
- Shows all placed components
- Handles component selection (click)
- Handles component drag/drop
- Shows grid/guidelines
- Zoom controls
- Snap-to-grid functionality

**Interactions**:
- Click component → select
- Click background → deselect
- Drag component → move (with grid snap)
- Drag resize handle → resize

---

### PlacedComponent
Renders a single component on canvas.

```typescript
interface PlacedComponentProps {
  component: PlacedComponent
  isSelected: boolean
  onSelect: () => void
  onPositionChange: (x: number, y: number) => void
  onSizeChange: (width: number, height: number) => void
}
```

**Renders**:
- Component content (text, image, video preview, shape)
- Selection box (when selected)
- Resize handles (when selected)
- Z-index indicator
- Component type icon

---

### PropertyPanel
Edit properties of selected component.

```typescript
interface PropertyPanelProps {
  selectedComponentId: string | null
  onPropertyChange: (property: string, value: any) => void
}
```

**Sections**:
- Position & Size (X, Y, Width, Height)
- Layer (Z-Index with up/down buttons)
- Type-specific properties (Text, Image, Video, Graphics)
- Effects (list, add, edit, delete)
- History (version timeline)

**Validation**:
- X, Y: 0 to canvas bounds
- Width, Height: 10px minimum
- Backend constraint checking

---

### Toolbar
Component type selector.

```typescript
<Toolbar onAddComponent={(type) => {}} />
```

**Components**:
- Text button → Add text component
- Image button → Add image component
- Video button → Add video component
- Graphics button → Add graphics component

**Behavior**:
- Click creates new component at canvas center
- Opens property panel
- Component selected automatically

---

### Header
Top navigation and controls.

```typescript
<Header
  bannerTitle={title}
  isDirty={isDirty}
  onSave={handleSave}
  onTogglePreview={handleTogglePreview}
  isPreviewMode={isPreviewMode}
/>
```

**Controls**:
- Banner title
- Save button (disabled if not dirty)
- Preview toggle
- Undo/Redo buttons
- Help link

---

## Hooks Design

### useEditor
Main hook for editor state management.

```typescript
const {
  state,
  selectComponent,
  addComponent,
  deleteComponent,
  updateComponentProperty,
  saveChanges,
  undo,
  redo
} = useEditor(bannerId)
```

### useCanvas
Canvas-specific logic.

```typescript
const {
  canvasWidth,
  canvasHeight,
  scale,
  setScale,
  screenToCanvas,
  canvasToScreen
} = useCanvas()
```

### useSelection
Component selection management.

```typescript
const {
  selectedComponentId,
  selectComponent,
  deselectComponent,
  isSelected
} = useSelection()
```

### useSave
Auto-save and manual save logic.

```typescript
const {
  isSaving,
  error,
  save,
  isDirty,
  markDirty
} = useSave(bannerId)
```

### useComponentDrag
Drag-drop state for components.

```typescript
const {
  isDragging,
  dragStart,
  dragEnd,
  setDragPosition
} = useComponentDrag()
```

---

## API Client

### bannerService.ts

```typescript
export const bannerService = {
  getBanner: (bannerId: string) => GET /api/banners/{bannerId},
  updateBanner: (bannerId: string, data) => PUT /api/banners/{bannerId},
  addComponent: (bannerId: string, data) => POST /api/banners/{bannerId}/components,
  updateComponent: (bannerId: string, componentId: string, data) => PUT,
  deleteComponent: (bannerId: string, componentId: string) => DELETE
}
```

### layerService.ts

```typescript
export const layerService = {
  reorderComponent: (bannerId, componentId, newZIndex) => POST /reorder,
  moveForward: (bannerId, componentId) => POST /move-forward,
  moveBackward: (bannerId, componentId) => POST /move-backward,
  sendToFront: (bannerId, componentId) => POST /send-to-front,
  sendToBack: (bannerId, componentId) => POST /send-to-back
}
```

### mediaService.ts

```typescript
export const mediaService = {
  initializeUpload: (file) => POST /api/media/upload/initialize,
  uploadChunk: (mediaFileId, chunkNumber, data) => PUT /api/media/{mediaFileId}/chunks,
  completeUpload: (mediaFileId) => POST /api/media/{mediaFileId}/complete,
  getMediaUrl: (mediaFileId) => GET /api/media/{mediaFileId}/url
}
```

---

## State Management

### EditorContext

```typescript
interface EditorContextType {
  state: EditorState
  selectComponent: (id: string | null) => void
  addComponent: (type: ComponentType) => void
  updateComponent: (id: string, updates: Partial<PlacedComponent>) => void
  deleteComponent: (id: string) => void
  saveBanner: () => Promise<void>
  loadBanner: (id: string) => Promise<void>
  undo: () => void
  redo: () => void
}
```

**State**:
- `components`: PlacedComponent[]
- `selectedComponentId`: string | null
- `isDirty`: boolean
- `isLoading`: boolean
- `error`: string | null
- `canvasWidth/Height`: number
- `scale`: number
- `isPreviewMode`: boolean

---

## Key Features

### Auto-Save
- Saves every 30 seconds if dirty
- Shows save status in header
- Prevents data loss
- Non-blocking (background)

### Undo/Redo
- Up to 50 history states
- Keyboard shortcuts: Ctrl+Z, Ctrl+Shift+Z
- Button controls in header
- History cleared on successful save

### Grid & Guides
- Optional grid overlay
- Snap-to-grid on drag (8px, 16px, 32px)
- Alignment guides on edges
- Toggle in canvas controls

### Zoom
- Zoom buttons (%, +, -)
- Zoom range: 50% to 200%
- Fit-to-canvas option
- Keyboard shortcuts (Ctrl+Scroll)

### Preview Mode
- Toggle between edit and preview
- Shows component exactly as it will appear
- No editing UI elements
- Easy switch back to edit

---

## Styling Strategy

### Tailwind + CSS Modules

**Tailwind**: Layout, spacing, typography, colors  
**CSS Modules**: Component-specific styles, animations

```css
/* Canvas.module.css */
.canvas {
  @apply border-2 border-gray-300 relative overflow-hidden bg-white;
  aspect-ratio: var(--canvas-width) / var(--canvas-height);
}

.placedComponent {
  @apply absolute cursor-move;
  transform: translate(var(--x), var(--y));
}

.selected {
  @apply outline-2 outline-blue-500;
}
```

---

## Error Handling

**Network Errors**:
- Show toast notification
- Keep dirty state
- Allow retry

**Validation Errors**:
- Show inline error messages
- Highlight invalid field
- Prevent save

**API Errors**:
- Log to console
- Show user-friendly message
- Revert optimistic updates

---

## Performance Considerations

- Lazy load property panel
- Virtualize component list if > 50 components
- Debounce property changes (300ms)
- Memoize context consumers
- Code-split routes
- Image lazy loading
- Video preview thumbnails

---

## Accessibility

- Keyboard navigation (Tab, Arrow keys)
- Keyboard shortcuts (Ctrl+Z, etc.)
- ARIA labels on all interactive elements
- Color contrast WCAG AA
- Focus indicators
- Screen reader friendly

---

## Next: ADR Analysis (Stage 3)

Key architecture decisions for UI implementation.
