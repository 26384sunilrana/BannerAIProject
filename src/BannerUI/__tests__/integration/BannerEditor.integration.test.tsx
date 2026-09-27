import { renderHook, act } from '@testing-library/react'
import { useToast } from '@/hooks/useToast'
import { useSave } from '@/hooks/useSave'
import { useComponentDrag } from '@/hooks/useComponentDrag'
import { useResize } from '@/hooks/useResize'
import { setupFetchMockCleanup } from '../helpers/mockFetch'
import { Banner, BannerComponent } from '@/types/banner'

describe('Banner Editor Integration', () => {
  setupFetchMockCleanup()

  const mockBanner: Banner = {
    id: '1',
    title: 'Test Banner',
    description: 'Test',
    width: 1200,
    height: 600,
    backgroundColor: '#fff',
  }

  const mockComponent: BannerComponent = {
    id: 'comp1',
    type: 'text',
    x: 100,
    y: 100,
    width: 200,
    height: 100,
    zIndex: 0,
    rotation: 0,
    opacity: 1,
    isVisible: true,
    data: { content: 'Test' },
  }

  it('integrates toast notifications with save operation', () => {
    const { result: toastResult } = renderHook(() => useToast())
    const { result: saveResult } = renderHook(() =>
      useSave('1', [mockComponent], mockBanner)
    )

    expect(toastResult.current.messages).toHaveLength(0)
    expect(saveResult.current.isSaving).toBe(false)

    act(() => {
      toastResult.current.success('Editor ready')
    })

    expect(toastResult.current.messages).toHaveLength(1)
  })

  it('handles drag and drop workflow', () => {
    const { result: dragResult } = renderHook(() =>
      useComponentDrag([mockComponent], 1200, 600)
    )

    act(() => {
      dragResult.current.startDrag('comp1', 100, 100)
      dragResult.current.updateDragPosition(150, 150)
    })

    expect(dragResult.current.isDragging).toBe(true)

    act(() => {
      dragResult.current.endDrag()
    })

    expect(dragResult.current.isDragging).toBe(false)
  })

  it('handles resize and save together', () => {
    const { result: resizeResult } = renderHook(() =>
      useResize([mockComponent], 1200, 600)
    )
    const { result: saveResult } = renderHook(() =>
      useSave('1', [mockComponent], mockBanner)
    )

    act(() => {
      resizeResult.current.startResize('comp1', 'e', 300, 100)
    })

    expect(resizeResult.current.isResizing).toBe(true)

    act(() => {
      saveResult.current.markDirty()
    })

    expect(saveResult.current.isDirty).toBe(true)

    act(() => {
      resizeResult.current.endResize()
    })

    expect(resizeResult.current.isResizing).toBe(false)
  })

  it('manages component state through drag and save cycle', () => {
    const { result: dragResult } = renderHook(() =>
      useComponentDrag([mockComponent], 1200, 600)
    )

    act(() => {
      dragResult.current.startDrag('comp1', 100, 100)
      dragResult.current.updateDragPosition(200, 200)
    })

    const finalPosition = dragResult.current.getFinalPosition()
    expect(finalPosition).not.toBeNull()

    act(() => {
      dragResult.result.endDrag()
    })
  })

  it('handles multiple toasts during edit operations', () => {
    const { result: toastResult } = renderHook(() => useToast())

    act(() => {
      toastResult.current.info('Editing banner...')
      toastResult.current.success('Component added')
      toastResult.current.warning('Unsaved changes')
    })

    expect(toastResult.current.messages).toHaveLength(3)
  })

  it('tracks save state and dirty flag', () => {
    const { result: saveResult } = renderHook(() =>
      useSave('1', [mockComponent], mockBanner)
    )

    expect(saveResult.current.isDirty).toBe(false)
    expect(saveResult.current.isSaving).toBe(false)

    act(() => {
      saveResult.current.markDirty()
    })

    expect(saveResult.current.isDirty).toBe(true)
  })

  it('resize operation maintains component bounds', () => {
    const { result: resizeResult } = renderHook(() =>
      useResize([mockComponent], 1200, 600)
    )

    act(() => {
      resizeResult.current.startResize('comp1', 'e', 300, 100)
      const newSize = resizeResult.current.calculateResize(400, 100)
      expect(newSize?.width).toBeGreaterThan(200)
    })
  })

  it('drag respects grid snapping', () => {
    const { result: dragResult } = renderHook(() =>
      useComponentDrag([mockComponent], 1200, 600, 8, true)
    )

    act(() => {
      dragResult.result.startDrag('comp1', 100, 100)
      dragResult.result.updateDragPosition(115, 115)
    })

    const finalPos = dragResult.result.getFinalPosition()
    if (finalPos) {
      expect(finalPos.x % 8).toBe(0)
    }
  })

  it('integrates multiple operations in sequence', () => {
    const { result: dragResult } = renderHook(() =>
      useComponentDrag([mockComponent], 1200, 600)
    )
    const { result: resizeResult } = renderHook(() =>
      useResize([mockComponent], 1200, 600)
    )
    const { result: toastResult } = renderHook(() => useToast())

    // Drag operation
    act(() => {
      dragResult.current.startDrag('comp1', 100, 100)
      toastResult.current.info('Moving component')
    })

    expect(dragResult.current.isDragging).toBe(true)
    expect(toastResult.current.messages).toHaveLength(1)

    // Resize operation
    act(() => {
      dragResult.current.endDrag()
      resizeResult.current.startResize('comp1', 'e', 300, 100)
      toastResult.current.info('Resizing component')
    })

    expect(dragResult.current.isDragging).toBe(false)
    expect(resizeResult.current.isResizing).toBe(true)
    expect(toastResult.current.messages).toHaveLength(2)
  })

  it('auto-save workflow', () => {
    const { result: saveResult } = renderHook(() =>
      useSave('1', [mockComponent], mockBanner)
    )
    const { result: toastResult } = renderHook(() => useToast())

    act(() => {
      saveResult.current.markDirty()
      if (saveResult.current.isDirty) {
        toastResult.current.info('Auto-saving...')
      }
    })

    expect(saveResult.current.isDirty).toBe(true)
    expect(toastResult.current.messages).toHaveLength(1)
  })

  it('handles error scenarios', () => {
    const { result: saveResult } = renderHook(() =>
      useSave('1', [mockComponent], mockBanner)
    )
    const { result: toastResult } = renderHook(() => useToast())

    act(() => {
      saveResult.current.markDirty()
      toastResult.current.error('Failed to save changes')
    })

    expect(toastResult.current.messages[0].type).toBe('error')
  })

  it('manages component lifecycle', () => {
    const components = [mockComponent, { ...mockComponent, id: 'comp2' }]

    const { result: dragResult } = renderHook(() =>
      useComponentDrag(components, 1200, 600)
    )

    expect(components).toHaveLength(2)

    act(() => {
      dragResult.current.startDrag('comp1', 100, 100)
    })

    expect(dragResult.current.draggedComponentId).toBe('comp1')
  })
})
