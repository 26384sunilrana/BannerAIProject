import { renderHook, act } from '@testing-library/react'
import { useComponentDrag } from '@/hooks/useComponentDrag'
import { BannerComponent } from '@/types/banner'

describe('useComponentDrag', () => {
  const mockComponents: BannerComponent[] = [
    {
      id: '1',
      type: 'text',
      x: 50,
      y: 50,
      width: 200,
      height: 100,
      zIndex: 0,
      rotation: 0,
      opacity: 1,
      isVisible: true,
      data: { content: 'Test' },
    },
  ]

  it('initializes with default drag state', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    expect(result.current.isDragging).toBe(false)
    expect(result.current.draggedComponentId).toBeNull()
    expect(result.current.dragState.dragStartPos).toBeNull()
  })

  it('starts drag with valid component', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startDrag('1', 100, 100)
    })

    expect(result.current.isDragging).toBe(true)
    expect(result.current.draggedComponentId).toBe('1')
    expect(result.current.dragState.dragStartPos).toEqual({ x: 100, y: 100 })
  })

  it('ignores start drag for non-existent component', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startDrag('nonexistent', 100, 100)
    })

    expect(result.current.isDragging).toBe(false)
  })

  it('tracks mouse movement during drag', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startDrag('1', 100, 100)
      result.current.updateDragPosition(150, 150)
    })

    expect(result.current.dragState.currentDragPos).toEqual({ x: 150, y: 150 })
  })

  it('ends drag and resets state', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startDrag('1', 100, 100)
      result.current.endDrag()
    })

    expect(result.current.isDragging).toBe(false)
    expect(result.current.draggedComponentId).toBeNull()
    expect(result.current.dragState.dragStartPos).toBeNull()
  })

  it('calculates final position after drag', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startDrag('1', 100, 100)
      result.current.updateDragPosition(150, 150)
    })

    const finalPos = result.current.getFinalPosition()
    expect(finalPos).not.toBeNull()
    expect(finalPos?.x).toBeGreaterThanOrEqual(0)
    expect(finalPos?.y).toBeGreaterThanOrEqual(0)
  })

  it('snaps to grid when snap is enabled', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600, 8, true)
    )

    act(() => {
      result.current.startDrag('1', 50, 50)
      result.current.updateDragPosition(63, 63)
    })

    const finalPos = result.current.getFinalPosition()
    expect(finalPos?.x).toBe(Math.round(finalPos?.x / 8) * 8)
  })

  it('respects canvas bounds during drag', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startDrag('1', 1150, 550)
      result.current.updateDragPosition(1300, 700)
    })

    const finalPos = result.current.getFinalPosition()
    if (finalPos) {
      expect(finalPos.x + mockComponents[0].width).toBeLessThanOrEqual(1200)
      expect(finalPos.y + mockComponents[0].height).toBeLessThanOrEqual(600)
    }
  })

  it('prevents dragging when not dragging', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    const initialState = result.current.dragState

    act(() => {
      result.current.updateDragPosition(100, 100)
    })

    expect(result.current.dragState).toEqual(initialState)
  })

  it('handles custom grid size', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600, 16, true)
    )

    act(() => {
      result.current.startDrag('1', 50, 50)
      result.current.updateDragPosition(65, 65)
    })

    const finalPos = result.current.getFinalPosition()
    expect(finalPos?.x).toBe(Math.round(finalPos?.x / 16) * 16)
  })

  it('preserves component original position', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startDrag('1', 100, 100)
    })

    expect(result.current.dragState.dragStartComponentPos).toEqual({
      x: mockComponents[0].x,
      y: mockComponents[0].y,
    })
  })

  it('handles multiple components', () => {
    const multipleComponents: BannerComponent[] = [
      { ...mockComponents[0], id: '1' },
      { ...mockComponents[0], id: '2', x: 300, y: 300 },
    ]

    const { result } = renderHook(() =>
      useComponentDrag(multipleComponents, 1200, 600)
    )

    act(() => {
      result.current.startDrag('2', 300, 300)
    })

    expect(result.current.draggedComponentId).toBe('2')
  })

  it('snap disabled allows free positioning', () => {
    const { result } = renderHook(() =>
      useComponentDrag(mockComponents, 1200, 600, 8, false)
    )

    act(() => {
      result.current.startDrag('1', 50, 50)
      result.current.updateDragPosition(67, 73)
    })

    const finalPos = result.current.getFinalPosition()
    expect(finalPos?.x).toBe(67)
    expect(finalPos?.y).toBe(73)
  })
})
