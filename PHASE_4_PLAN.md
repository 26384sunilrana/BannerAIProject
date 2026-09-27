# Phase 4: Frontend Legacy Coverage Plan

## Scope

Complete testing for all frontend legacy components, hooks, API services, and pages not covered in Phase 1 (new features) testing.

**Target**: 140-180 additional tests across 20-25 test files

## Components to Test (7 total, ~50-70 tests)

All in `src/components/Common/`:

### 1. **ConfirmDialog.test.tsx** (8-10 tests)
- Rendering with title, message, buttons
- Button click handlers (onConfirm, onCancel, onDismiss)
- Modal behavior (backdrop click, escape key)
- Different button configurations (dangerous actions, disabled states)
- Loading state during async confirmation

### 2. **ErrorBoundary.test.tsx** (10-12 tests)
- Renders children successfully when no error
- Catches JavaScript errors in child components
- Shows error UI with error message
- Provides reset button to clear error state
- Logs errors to console/error service
- Handles error recovery after reset
- Graceful degradation (doesn't break entire app)

### 3. **Input.test.tsx** (8-10 tests)
- Renders with label, placeholder, value
- Handles onChange events
- Applies CSS classes for states (disabled, error, focus)
- Validation error display
- Input masking/formatting if applicable
- Required field indicator
- Disabled input behavior

### 4. **LoadingOverlay.test.tsx** (6-8 tests)
- Shows loading spinner when visible
- Hidden when not visible
- Backdrop behavior (clickable or not)
- Custom loading messages
- Spinner animation/styling
- Z-index layering (appears over content)

### 5. **Select.test.tsx** (8-10 tests)
- Renders with options, selected value
- onChange handler on selection
- Handles empty state (no options)
- Disabled option rendering
- Multi-select functionality if supported
- Placeholder text
- Keyboard navigation (arrow keys)
- Option grouping

### 6. **Toast.test.tsx** (8-10 tests)
- Shows toast messages with type (success, error, warning, info)
- Auto-dismisses after timeout
- Manual dismiss via close button
- Multiple toasts stacking
- Icon rendering based on type
- Action buttons (if applicable)
- Toast styling by type
- Accessibility (ARIA live region)

### 7. **index.ts** (2-4 tests)
- Exports all components correctly
- No missing exports
- Types are exported properly

## Hooks to Test (8 total, ~60-80 tests)

All in `src/hooks/`:

### 1. **useComponentDrag.test.ts** (8-10 tests)
- Initializes drag state with position
- Handles mouseDown event (start drag)
- Tracks mouse movement during drag
- Calculates delta position correctly
- Handles mouseUp event (end drag)
- Updates position on drag
- Resets state after drag complete
- Prevents default drag behavior

### 2. **useDebounce.test.ts** (6-8 tests)
- Returns unchanged value initially
- Debounces value changes
- Updates value after delay
- Cancels previous debounce on rapid changes
- Handles cleanup on unmount
- Respects custom delay parameter
- Works with different data types

### 3. **useEditor.test.ts** (10-12 tests)
- Initializes editor state
- Handles canvas operations
- Updates banner properties
- Manages component selection
- Provides undo/redo functionality
- Validates state transitions
- Handles error scenarios
- Provides editor context to children

### 4. **useKeyboardShortcuts.test.ts** (8-10 tests)
- Registers keyboard shortcuts
- Executes correct handler on key press
- Handles modifier keys (Ctrl, Shift, Alt)
- Supports multiple shortcuts
- Unregisters on unmount
- Prevents default browser behavior when appropriate
- Works with different key combinations
- Ignores shortcuts in input fields (if applicable)

### 5. **useMediaUpload.test.ts** (10-12 tests)
- Initiates upload with file
- Handles upload progress
- Updates progress percentage
- Completes upload successfully
- Handles upload errors
- Provides retry mechanism
- Cancels in-progress upload
- Validates file type/size
- Uses mockFetch helper (no real uploads)

### 6. **useResize.test.ts** (8-10 tests)
- Tracks window resize events
- Updates dimensions on resize
- Debounces resize handler
- Unregisters on unmount
- Handles multiple components
- Provides current dimensions
- Works with custom hooks

### 7. **useSave.test.ts** (8-10 tests)
- Initiates save operation
- Tracks save progress
- Handles save success
- Handles save errors
- Prevents duplicate saves
- Provides save status (idle, pending, success, error)
- Auto-save functionality if implemented
- Uses mockFetch for API calls

### 8. **useToast.test.ts** (8-10 tests)
- Adds toast messages
- Removes toast by ID
- Handles different toast types
- Auto-dismisses toasts
- Provides toast state
- Works without Hook context (standalone)
- Handles rapid toast additions
- Manages toast queue

## API Services to Test (6 total, ~30-40 tests)

All in `src/api/`:

### 1. **bannerService.test.ts** (6-8 tests)
- createBanner with valid data
- getBanner by ID
- updateBanner
- deleteBanner
- listBanners with pagination
- Uses mockFetch helper
- Handles API errors properly
- Validates request/response shapes

### 2. **client.test.ts** (4-6 tests)
- Creates HTTP client correctly
- Sets default headers (auth token)
- Handles request interceptors
- Handles response interceptors
- Includes error handling
- Provides retry logic if applicable

### 3. **effectsService.test.ts** (5-7 tests)
- listEffects
- applyEffect
- removeEffect
- updateEffect
- Uses mockFetch
- Validates effect parameters

### 4. **layerService.test.ts** (5-7 tests)
- reorderLayers
- moveForward/moveBackward
- sendToFront/sendToBack
- getLayerOrder
- Uses mockFetch
- Validates Z-index updates

### 5. **mediaService.test.ts** (6-8 tests)
- initializeUpload
- uploadChunk
- completeUpload
- getMediaUrl
- cancelUpload
- Uses mockFetch
- Tracks upload progress

### 6. **versionService.test.ts** (4-6 tests)
- listVersions
- getVersion
- restoreVersion
- Uses mockFetch
- Validates version restoration

## Pages to Test (3-4 total, ~15-25 tests)

### 1. **app/banners/[bannerId]/editor/page.test.tsx** (8-12 tests)
- Renders editor with banner loaded
- Initializes editor context
- Provides editor tools/panels
- Handles banner save
- Handles banner publish
- Handles banner deletion
- Shows loading state while fetching
- Handles errors gracefully

### 2. **app/layout.test.tsx** (4-6 tests)
- Renders header and navigation
- Renders children/page content
- Applies global styles
- Handles authentication state
- Shows user menu when logged in
- Responsive layout

### 3. **app/page.test.tsx** (3-5 tests)
- Landing page renders
- Shows CTA buttons
- Displays feature highlights
- Responsive design

### 4. **Authentication Flow** (optional, 4-6 tests)
- Login page renders
- Register page renders
- Form validation
- Submission handling

## Test Pattern Standards (Consistent with Phase 1-3)

### Hook Testing Pattern
```typescript
import { renderHook, act } from '@testing-library/react';
import { mockFetchOnce, setupFetchMockCleanup } from '../helpers/mockFetch';

describe('useCustomHook', () => {
  setupFetchMockCleanup();
  
  it('should...', () => {
    const { result } = renderHook(() => useCustomHook());
    act(() => {
      // Test hook behavior
    });
    expect(result.current).toBe(...);
  });
});
```

### Component Testing Pattern
```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Component } from '@/components/...';

describe('Component', () => {
  it('should...', () => {
    render(<Component prop="value" />);
    expect(screen.getByText('...')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button'));
    expect(...).toBe(...);
  });
});
```

### API Service Testing Pattern
```typescript
import { mockFetchOnce, setupFetchMockCleanup } from '__tests__/helpers/mockFetch';
import { apiService } from '@/api/service';

describe('apiService', () => {
  setupFetchMockCleanup();
  
  it('should call endpoint correctly', async () => {
    mockFetchOnce(200, { data: 'response' });
    const result = await apiService.method();
    expect(result).toEqual({ data: 'response' });
  });
});
```

## Implementation Strategy

1. **Quick Wins** (Components & Simple Hooks): Start with Toast, LoadingOverlay, Input (easy to test)
2. **Medium Complexity** (Other Hooks): useDebounce, useResize (no external dependencies)
3. **Complex** (Stateful Hooks & Pages): useEditor, useMediaUpload, page integration tests
4. **API Services** (Fetch Mocking): Use established mockFetch pattern from Phase 1

## Success Criteria

- ✅ All 8 components have >10 tests each (55-70 tests)
- ✅ All 8 hooks have >8 tests each (60-80 tests)
- ✅ All 6 API services have >5 tests each (30-40 tests)
- ✅ All 3-4 pages have >4 tests each (15-25 tests)
- ✅ Total: 140-180 tests across 20-25 files
- ✅ Follow React Testing Library best practices
- ✅ Use mockFetch helper (no MSW)
- ✅ Clear test names and AAA structure

## Phase 4 Execution Notes

- Estimate ~200-300 lines per test file (5-10 tests × 20-40 lines each)
- Use renderHook for hook testing (not component wrapper)
- Use React Testing Library for component testing (no Enzyme)
- Mock fetch calls, not component dependencies
- Test user interactions, not implementation details
- Organize tests by feature/endpoint in regions

## Timeline

- **Phase 3**: In progress (30 files, ~180-230 tests) — Est. 6-8 hours
- **Phase 4**: Planned (25 files, ~150-180 tests) — Est. 8-10 hours
- **Phase 5**: Enforcement (tooling + gates) — Est. 1-2 hours

## After Phase 4 Complete

- Backend coverage: ~400+ tests (Phase 1-3)
- Frontend coverage: ~150+ tests (Phase 1 + Phase 4)
- **Total**: 550-600+ tests across entire codebase
- Ready for Phase 5: Coverage measurement & 80% enforcement
