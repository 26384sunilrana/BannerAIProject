# DDD-03: ADR Analysis — Banner Editor UI

**Status**: Stage 3 of 5 (ADR Analysis)  
**Bolt**: 007-banner-editor-ui  
**Created**: 2026-09-26  

---

## ADR-001: State Management Approach

**Status**: ✅ ACCEPTED

### Decision
**React Context + Custom Hooks** for state management (not Redux/Zustand).

### Rationale
- Simpler, less boilerplate for moderate complexity
- No additional dependency
- Context sufficient for editor state
- Custom hooks encapsulate logic
- Easy to test

### Consequences
- ✅ Reduced bundle size
- ✅ Simpler debugging
- ⚠️ Context re-renders all consumers
- ⚠️ Performance optimization needed with React.memo

### Memoization Strategy
- Wrap components with React.memo
- Use useCallback for handlers
- Split context into smaller contexts if needed

---

## ADR-002: Drag-Drop Library

**Status**: ✅ ACCEPTED

### Decision
**react-beautiful-dnd** for drag-drop (not react-dnd or native HTML5).

### Rationale

| Aspect | react-beautiful-dnd | react-dnd | Native |
|--------|-------------------|-----------|--------|
| **Ease** | ✅ Very easy | ⚠️ Complex | ⚠️ Limited |
| **Performance** | ✅ Great | ✅ Great | ⚠️ Good |
| **Animations** | ✅ Built-in | ⚠️ Custom | ❌ None |
| **Mobile** | ✅ Good | ✅ Good | ❌ Poor |
| **Learning** | ✅ Quick | ❌ Steep | ⚠️ Medium |

### Consequences
- ✅ Smooth drag-drop UX
- ✅ Good performance
- ✅ Mobile support
- ⚠️ Library size (40KB gzipped)
- ⚠️ React 18 compatibility needs care

### Usage
```typescript
<DragDropContext onDragEnd={onDragEnd}>
  <Droppable droppableId="canvas">
    {(provided) => (
      <Canvas {...provided.droppableProps}>
        {components.map((c) => (
          <Draggable key={c.id} draggableId={c.id} index={index}>
            {(provided) => (
              <PlacedComponent {...provided.dragHandleProps} />
            )}
          </Draggable>
        ))}
      </Canvas>
    )}
  </Droppable>
</DragDropContext>
```

---

## ADR-003: Styling Approach

**Status**: ✅ ACCEPTED

### Decision
**Tailwind CSS + CSS Modules** (not CSS-in-JS like styled-components).

### Rationale
- Tailwind: Utility-first for rapid UI development
- CSS Modules: Component-scoped styles for canvas/animations
- No runtime overhead
- Good performance
- Easy maintenance

### When to Use What
**Tailwind**: Layout, spacing, typography, standard components  
**CSS Modules**: Canvas, animations, complex component-specific styles

### Example

```typescript
// Component with Tailwind
<div className="flex items-center gap-4 p-6 bg-white rounded-lg">
  {/* Tailwind utilities */}
</div>

// Component with CSS Modules for canvas
<canvas className={styles.editor} />
```

---

## ADR-004: Auto-Save Strategy

**Status**: ✅ ACCEPTED

### Decision
**Auto-save every 30 seconds** (if dirty) + manual save button.

### Rationale
- 30s interval balances responsiveness and API calls
- Prevents data loss
- Manual save for immediate feedback
- Background, non-blocking
- Keeps API usage reasonable

### Implementation
```typescript
useEffect(() => {
  if (!isDirty) return
  
  const interval = setInterval(() => {
    save() // silent save
  }, 30000)
  
  return () => clearInterval(interval)
}, [isDirty])
```

---

## ADR-005: Component Type Handling

**Status**: ✅ ACCEPTED

### Decision
**One PropertyPanel** with conditional rendering based on component type (not separate panels per type).

### Rationale
- Simpler state management
- Less component duplication
- Cleaner code
- Easier to maintain
- Better UX (consistent location)

### Implementation
```typescript
{type === 'text' && <TextProperties />}
{type === 'image' && <ImageProperties />}
{type === 'video' && <VideoProperties />}
{type === 'graphics' && <GraphicsProperties />}
```

---

## ADR-006: Real-Time Updates

**Status**: ✅ ACCEPTED

### Decision
**Optimistic UI updates** - update locally, then API call, revert on error.

### Rationale
- Faster perceived performance
- Better UX (no lag)
- Recoverable on error
- Standard pattern

### Example: Drag Component
```typescript
// Local update (instant)
setComponent(id, { x, y })

// API call (async)
api.updateComponent(id, { x, y })
  .catch(() => {
    // Revert on error
    setComponent(id, { x: oldX, y: oldY })
    showError('Failed to save')
  })
```

---

## ADR-007: Canvas Coordinate System

**Status**: ✅ ACCEPTED

### Decision
**Canvas-relative coordinates** (0,0 is top-left of canvas, not screen).

### Rationale
- Simpler math for positioning
- Independent of zoom
- Matches backend API
- No translation layer needed

### Screen → Canvas Conversion
```typescript
const screenToCanvas = (screenX, screenY) => {
  const rect = canvas.getBoundingClientRect()
  const x = (screenX - rect.left) / scale
  const y = (screenY - rect.top) / scale
  return { x, y }
}
```

---

## ADR-008: Zoom Implementation

**Status**: ✅ ACCEPTED

### Decision
**Canvas transform scale** (not zoom on components individually).

### Rationale
- One point of control
- Affects entire viewport
- Uses CSS transform (GPU accelerated)
- Consistent across canvas

### CSS
```css
.canvas {
  transform: scale(var(--scale));
  transform-origin: top left;
}
```

---

## ADR-009: Error Handling UI

**Status**: ✅ ACCEPTED

### Decision
**Toast notifications** for transient errors, **inline errors** for form validation.

### Rationale
- Toast: Non-blocking, dismissible, temporary
- Inline: Context-aware, immediate feedback
- Combination provides best UX

### Example
```typescript
// Toast for network errors
showToast('Failed to save', 'error')

// Inline for validation
<input className={error ? 'border-red-500' : ''} />
{error && <span className="text-red-500">{error}</span>}
```

---

## ADR-010: TypeScript Strictness

**Status**: ✅ ACCEPTED

### Decision
**TypeScript strict mode** enabled, no `any` types.

### Rationale
- Catch errors at compile time
- Better IDE support
- Self-documenting code
- Easier refactoring

### Configuration
```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true
  }
}
```

---

## Decision Summary

| ADR | Decision | Status | Risk |
|-----|----------|--------|------|
| ADR-001 | Context + Hooks | ✅ ACCEPTED | Low |
| ADR-002 | react-beautiful-dnd | ✅ ACCEPTED | Low |
| ADR-003 | Tailwind + CSS Modules | ✅ ACCEPTED | Low |
| ADR-004 | Auto-save 30s | ✅ ACCEPTED | Low |
| ADR-005 | Single PropertyPanel | ✅ ACCEPTED | Low |
| ADR-006 | Optimistic UI | ✅ ACCEPTED | Low |
| ADR-007 | Canvas-relative coords | ✅ ACCEPTED | Low |
| ADR-008 | Transform scale zoom | ✅ ACCEPTED | Low |
| ADR-009 | Toast + inline errors | ✅ ACCEPTED | Low |
| ADR-010 | TypeScript strict | ✅ ACCEPTED | Low |

---

## Next: Implementation (Stage 4)

Ready to implement React components and hooks.
