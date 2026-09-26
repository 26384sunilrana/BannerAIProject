# Bolt 007 - Banner Editor UI - Implementation Plan

**Status**: Stages 1-3 Complete (Ready for Stage 4)  
**Created**: 2026-09-26  

---

## Project Setup

### Initialize Next.js Project
```bash
npx create-next-app@latest banner-editor \
  --typescript \
  --tailwind \
  --eslint \
  --no-git
```

### Install Dependencies
```bash
npm install react-beautiful-dnd axios react-hook-form
npm install --save-dev @types/react-beautiful-dnd jest @testing-library/react
```

### Configuration
- Enable TypeScript strict mode
- Configure Tailwind CSS
- Setup Jest test configuration
- Configure environment variables (.env.local)

---

## Stage 4: Implementation Tasks

### Phase 1: Core Infrastructure
1. **EditorContext & Provider**
   - `src/context/EditorContext.tsx`
   - `src/context/EditorProvider.tsx`
   - Global state for banner, components, selection

2. **API Client Layer**
   - `src/api/client.ts` - axios instance with interceptors
   - `src/api/bannerService.ts` - Banner CRUD
   - `src/api/componentService.ts` - Component operations
   - `src/api/layerService.ts` - Layer management
   - `src/api/mediaService.ts` - Media upload
   - `src/api/effectsService.ts` - Effects management

3. **Type Definitions**
   - `src/types/editor.ts` - EditorState, EditorAction
   - `src/types/banner.ts` - Banner, Component types
   - `src/types/api.ts` - API response types

### Phase 2: Custom Hooks
1. **useEditor** - Main editor state hook
2. **useCanvas** - Canvas sizing and transforms
3. **useSelection** - Component selection logic
4. **useSave** - Auto-save and manual save
5. **useComponentDrag** - Drag-drop coordination
6. **useResize** - Component resizing
7. **useUndo** - Undo/Redo history

### Phase 3: React Components
1. **Layout Components**
   - `Header.tsx` - Top navigation
   - `Toolbar.tsx` - Component palette
   - `PropertyPanel.tsx` - Property editor
   - `Canvas.tsx` - Main editing area

2. **Canvas Subcomponents**
   - `PlacedComponent.tsx` - Individual component
   - `ResizeHandles.tsx` - Resize handles
   - `Grid.tsx` - Grid overlay
   - `Guidelines.tsx` - Alignment guides

3. **Property Panel Subcomponents**
   - `TextProperties.tsx` - Text editing
   - `ImageProperties.tsx` - Image editing
   - `VideoProperties.tsx` - Video editing
   - `GraphicsProperties.tsx` - Graphics editing
   - `LayerProperties.tsx` - Z-index controls
   - `EffectsList.tsx` - Effects management

4. **Common Components**
   - `Button.tsx` - Reusable button
   - `Input.tsx` - Text input
   - `Select.tsx` - Dropdown
   - `Toast.tsx` - Notification
   - `ConfirmDialog.tsx` - Confirmation

5. **Page Component**
   - `app/banners/[bannerId]/editor/page.tsx`

### Phase 4: Testing
1. **Component Tests**
   - Canvas.test.tsx
   - PlacedComponent.test.tsx
   - PropertyPanel.test.tsx
   - Toolbar.test.tsx

2. **Hook Tests**
   - useEditor.test.ts
   - useSelection.test.ts
   - useSave.test.ts

3. **Integration Tests**
   - Full editor workflow
   - API integration
   - Drag-drop functionality

---

## Implementation Order (Recommended)

**Week 1: Foundation**
1. Setup Next.js project ✓
2. Create types and interfaces ✓
3. Implement EditorContext
4. Create API client layer
5. Implement core hooks (useEditor, useCanvas)

**Week 2: Core UI**
1. Implement Canvas component
2. Implement PlacedComponent
3. Implement Toolbar
4. Implement Header
5. Wire up selection logic

**Week 3: Property Editing**
1. Implement PropertyPanel
2. Create property component variations
3. Implement update callbacks
4. Wire up API integration

**Week 4: Polish & Testing**
1. Implement auto-save
2. Add undo/redo
3. Create tests
4. Performance optimization
5. Accessibility review

---

## API Integration Checklist

- [ ] Implement banner loading (GET /api/banners/{bannerId})
- [ ] Implement component CRUD
- [ ] Integrate layer management
- [ ] Implement media upload
- [ ] Integrate effects API
- [ ] Implement version history
- [ ] Error handling for all APIs
- [ ] Loading states
- [ ] Optimistic updates

---

## Features Checklist

- [ ] Canvas rendering with grid
- [ ] Component drag-drop
- [ ] Component selection
- [ ] Property editing
- [ ] Z-index management
- [ ] Auto-save (30s interval)
- [ ] Manual save
- [ ] Undo/Redo
- [ ] Preview mode
- [ ] Zoom controls
- [ ] Version history
- [ ] Media upload

---

## Testing Checklist

- [ ] Component unit tests
- [ ] Hook unit tests
- [ ] API integration tests
- [ ] E2E tests (Cypress/Playwright)
- [ ] Accessibility tests
- [ ] Performance tests

---

## Performance Targets

- **Canvas initial load**: < 2 seconds
- **Component drag-drop**: 60 FPS
- **Property updates**: < 300ms debounce
- **API response**: < 1 second
- **Bundle size**: < 500KB (gzipped)

---

## Browser Support

- Chrome/Edge (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Mobile browsers (iOS Safari, Chrome Android)

---

## Accessibility Requirements

- WCAG 2.1 Level AA
- Keyboard navigation
- Screen reader support
- Color contrast ≥ 4.5:1
- Focus indicators
- ARIA labels

---

## Next Steps

1. **Immediate**: Create Next.js project with dependencies
2. **Phase 1**: Implement EditorContext and API layer (2-3 days)
3. **Phase 2**: Implement custom hooks (2 days)
4. **Phase 3**: Build React components (4-5 days)
5. **Phase 4**: Testing and polish (2-3 days)

**Estimated total**: 10-14 days for complete implementation

---

## Known Challenges

1. **Drag-drop performance** - Optimize component rendering
2. **Zoom + drag precision** - Coordinate system conversion
3. **API synchronization** - Handle concurrent updates
4. **Image/video upload** - Large file handling
5. **Responsive canvas** - Mobile editing experience

---

## Success Criteria

✅ All 4 stories completed (S19-S22)  
✅ All CRUD operations functional  
✅ Drag-drop working smoothly  
✅ Property editing complete  
✅ Save/preview working  
✅ 80%+ test coverage  
✅ Performance targets met  
✅ Accessibility compliance verified  

---

## Notes for Implementation Team

- Reuse backend validation logic on frontend where possible
- Implement loading states for all async operations
- Use optimistic updates to improve perceived performance
- Keep API calls batched where possible
- Monitor bundle size carefully
- Consider code-splitting for property panels
- Plan for offline capability in future iteration

---

## Documentation

After implementation, create:
1. Component storybook (Storybook.js)
2. User guide for editor
3. Architecture documentation
4. API integration guide
5. Testing strategy document
