---
unit: 005-banner-editor-ui
intent: 001-banner-editor-core
phase: inception
status: draft
created: 2026-09-26T00:00:00Z
unit_type: frontend
default_bolt_type: simple-construction-bolt
---

# Unit Brief: Banner Editor UI (Frontend)

## Purpose

Next.js web application providing drag-and-drop interface for creating and editing digital banners. Serves all user-facing features for no-code banner design.

## Scope

### In Scope
- Drag-and-drop canvas
- Component toolbox (text, image, video, graphics)
- Component property inspector
- Visual effects selector
- Carousel configuration UI
- Layer management (z-index)
- Save/auto-save with versioning
- Preview mode
- Version history browser
- Rollback functionality
- Multi-user session management
- Shop-level data filtering

### Out of Scope
- Backend API implementation (Units 001-004)
- Video transcoding
- Media hosting

---

## Assigned Requirements

All user-facing aspects:

| FR | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Drag-and-drop canvas | Must |
| FR-2 | Component system | Must |
| FR-3 | Component layering | Must |
| FR-4 | Visual effects | Must |
| FR-5 | Carousel/rotation | Must |
| FR-6 | Video handling UI | Must |
| FR-7 | Version history UI | Must |
| FR-8 | Preview functionality | Must |
| FR-10 | Multi-user access | Must |

---

## Domain Concepts

### Key Components
| Component | Description |
|-----------|-------------|
| Canvas | Editable drag-drop area |
| Toolbox | Component library picker |
| Inspector | Property editor panel |
| EffectSelector | Effects configuration UI |
| LayerPanel | Z-index/stacking visualization |
| VersionHistory | Version list and rollback |
| PreviewMode | Full-screen banner preview |

### Key Operations
| Operation | Description |
|-----------|-------------|
| DragComponent | Drag from toolbox to canvas |
| ResizeComponent | Change component size |
| EditProperties | Modify color, text, etc. |
| ApplyEffect | Select and apply effect |
| SaveBanner | Save to backend (creates version) |
| PreviewBanner | Switch to preview mode |
| RollbackVersion | Restore previous version |

---

## Story Summary

| Metric | Count |
|--------|-------|
| Total Stories | 10 |
| Must Have | 10 |

### Stories (Summary)

- S1: Canvas foundation and component grid
- S2: Drag component from toolbox to canvas
- S3: Resize and reposition components
- S4: Edit component properties (text, color)
- S5: Component z-index/layer management
- S6: Visual effects selector UI
- S7: Carousel/rotation configuration UI
- S8: Save banner (auto-save + version create)
- S9: Preview mode
- S10: Version history browser + rollback

---

## Dependencies

### Depends On (All Backend Units)
- Unit 001: Banner Service (core CRUD)
- Unit 002: Version Control Service (versions)
- Unit 003: Effects Engine (effects library)
- Unit 004: Media Service (uploads)

### Depended By
- None

---

## Technical Context

### Technology
- Language: TypeScript (strict mode)
- Framework: Next.js
- Styling: Tailwind CSS or CSS Modules
- State: React Context or Zustand
- Drag-drop: react-dnd or react-beautiful-dnd
- Testing: Jest + React Testing Library
- Linting: ESLint strict + Prettier

### Integration Points
| Integration | Type | Protocol |
|-------------|------|----------|
| Banner Service | API | REST/JSON |
| Version Control Service | API | REST/JSON |
| Effects Engine | API | REST/JSON |
| Media Service | API | REST/multipart |
| Auth | Token | JWT |

### Page Structure
```
/editor
  /[bannerId]
    - Canvas page with all editing tools
    - Auto-save on changes
    - Real-time state sync
/versions
  /[bannerId]
    - Version history sidebar
/preview
  /[bannerId]
    - Full-screen banner view
```

---

## Constraints

- **Ease of use** - Low learning curve (critical)
- **Performance** - Canvas load < 2s, interactions < 100ms
- **Data isolation** - Display only user's shop banners
- **Responsiveness** - Mobile-responsive (tablet+)
- **Accessibility** - WCAG 2.1 AA

---

## Success Criteria

### Functional
- [x] Drag-drop canvas works smoothly
- [x] All component types editable
- [x] Properties apply correctly
- [x] Effects preview in real-time
- [x] Save creates version
- [x] Rollback works
- [x] Multi-user sessions isolated

### Non-Functional
- [x] Canvas load < 2 seconds
- [x] Drag interaction < 100ms lag
- [x] Smooth 60 FPS animations
- [x] Mobile responsive
- [x] WCAG AA compliant
- [x] > 80% test coverage

### UX Quality
- [x] Intuitive navigation
- [x] Clear component tools
- [x] Easy property editing
- [x] Responsive feedback on actions

---

## Bolt Plan

| Bolt | Stories | Focus |
|------|---------|-------|
| Bolt-005 | S1, S2, S3 | Canvas foundation + drag |
| Bolt-005b | S4, S5 | Properties + z-index |
| Bolt-005c | S6, S7 | Effects + carousel |
| Bolt-005d | S8, S9, S10 | Save, preview, versions |

---

## Notes

- Start with simple components (text), add image/video after
- Use performance profiling for canvas rendering
- Test with realistic banner complexity (50 components)
- Plan for easy drag-drop library swap
- Consider component presets/templates (future)
- Accessibility important for all user types
