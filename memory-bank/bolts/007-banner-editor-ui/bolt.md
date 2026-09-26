---
id: 007-banner-editor-ui
unit: 005-banner-editor-ui
intent: 001-banner-editor-core
type: simple-construction-bolt
status: planned
stories: [001-canvas-foundation, 002-drag-drop, 003-component-properties, 004-save-preview]
created: 2026-09-26T00:00:00Z

requires_bolts: [003-banner-service, 004-version-control-service, 005-effects-engine, 006-media-service]
enables_bolts: []
requires_units: [001-banner-service, 002-version-control-service, 003-effects-engine, 004-media-service]
blocks: false

complexity:
  avg_complexity: 2
  avg_uncertainty: 2
  max_dependencies: 3
  testing_scope: 2
---

# Bolt: 007-banner-editor-ui

## Objective

Build Next.js drag-and-drop editor UI - foundation canvas, component toolbox, property editing, and save/preview.

## Stories Included

- [ ] **001-canvas-foundation**: Canvas component with grid, guidelines, responsive layout
- [ ] **002-drag-drop**: Drag components from toolbox to canvas, resize, reposition
- [ ] **003-component-properties**: Property inspector for text, image, video, graphics components
- [ ] **004-save-preview**: Save button with auto-save, preview mode toggle

## Technology

- Next.js with TypeScript (strict)
- Drag-drop: react-dnd or react-beautiful-dnd
- State: React Context or Zustand
- Styling: Tailwind CSS

## Dependencies

### Requires All Backend Units
- **003-banner-service**: Banner CRUD APIs
- **004-version-control-service**: Version APIs
- **005-effects-engine**: Effects library API
- **006-media-service**: Media upload API

## Estimated Duration

**4-5 days** (includes drag-drop library setup, API integration, state management)
