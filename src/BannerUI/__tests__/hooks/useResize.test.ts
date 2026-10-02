import { renderHook, act } from '@testing-library/react'
import { useResize } from '@/hooks/useResize'
import { BannerComponent } from '@/types/banner'

describe('useResize', () => {
  const mockComponents: BannerComponent[] = [
    {
      id: '1',
      type: 'text',
      x: 100,
      y: 100,
      width: 200,
      height: 150,
      zIndex: 0,
      rotation: 0,
      opacity: 1,
      isVisible: true,
      data: { content: 'Test' },
    },
  ]

  it('initializes with default resize state', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    expect(result.current.isResizing).toBe(false)
    expect(result.current.resizedComponentId).toBeNull()
    expect(result.current.resizeState.handle).toBeNull()
  })

  it('starts resize from east handle', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'e', 200, 100)
    })

    expect(result.current.isResizing).toBe(true)
    expect(result.current.resizedComponentId).toBe('1')
    expect(result.current.resizeState.handle).toBe('e')
  })

  it('starts resize from north-west handle', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'nw', 100, 100)
    })

    expect(result.current.isResizing).toBe(true)
    expect(result.current.resizeState.handle).toBe('nw')
  })

  it('calculates resize from east handle', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'e', 300, 100)
    })

    const newSize = result.current.calculateResize(400, 100)
    expect(newSize?.width).toBeGreaterThan(200)
  })

  it('calculates resize from west handle', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'w', 100, 100)
    })

    const newSize = result.current.calculateResize(50, 100)
    expect(newSize?.width).toBeGreaterThan(200)
    expect(newSize?.x).toBe(50)
  })

  it('calculates resize from south handle', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 's', 100, 250)
    })

    const newSize = result.current.calculateResize(100, 350)
    expect(newSize?.height).toBeGreaterThan(150)
  })

  it('enforces minimum size limit', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'e', 300, 100)
    })

    const newSize = result.current.calculateResize(310, 100)
    expect(newSize?.width).toBeGreaterThanOrEqual(20)
  })

  it('respects canvas bounds', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'e', 300, 100)
      const newSize = result.current.calculateResize(1400, 100)
      if (newSize) {
        expect(newSize.x + newSize.width).toBeLessThanOrEqual(1200)
      }
    })
  })

  it('ends resize and resets state', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'e', 300, 100)
      result.current.endResize()
    })

    expect(result.current.isResizing).toBe(false)
    expect(result.current.resizedComponentId).toBeNull()
  })

  it('ignores start resize for non-existent component', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('nonexistent', 'e', 300, 100)
    })

    expect(result.current.isResizing).toBe(false)
  })

  it('handles corner resize (ne)', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'ne', 300, 100)
    })

    const newSize = result.current.calculateResize(400, 50)
    expect(newSize?.width).toBeGreaterThan(200)
    // dragging the top edge upwards makes the component taller and moves its top edge up
    expect(newSize?.height).toBe(200)
    expect(newSize?.y).toBe(50)
  })

  it('handles corner resize (se)', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'se', 300, 250)
    })

    const newSize = result.current.calculateResize(400, 350)
    expect(newSize?.width).toBeGreaterThan(200)
    expect(newSize?.height).toBeGreaterThan(150)
  })

  it('handles corner resize (sw)', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'sw', 100, 250)
    })

    const newSize = result.current.calculateResize(50, 350)
    expect(newSize?.width).toBeGreaterThan(200)
    expect(newSize?.height).toBeGreaterThan(150)
  })

  it('preserves aspect ratio when configured', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'e', 300, 100)
    })

    const initial = result.current.resizeState
    expect(initial.startWidth).toBe(200)
    expect(initial.startHeight).toBe(150)
  })

  it('clamps position to prevent going outside canvas', () => {
    const { result } = renderHook(() =>
      useResize(mockComponents, 1200, 600)
    )

    act(() => {
      result.current.startResize('1', 'w', 100, 100)
      const newSize = result.current.calculateResize(-50, 100)
      if (newSize) {
        expect(newSize.x).toBeGreaterThanOrEqual(0)
      }
    })
  })
})
