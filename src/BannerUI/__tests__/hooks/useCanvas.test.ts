import { renderHook, act } from '@testing-library/react'
import { useCanvas } from '@/hooks/useCanvas'

describe('useCanvas', () => {
  it('initializes with default values', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    expect(result.current.scale).toBe(1)
    expect(result.current.width).toBe(1200)
    expect(result.current.height).toBe(600)
    expect(result.current.offset).toEqual({ x: 0, y: 0 })
  })

  it('zooms in', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    act(() => {
      result.current.zoomIn()
    })

    expect(result.current.scale).toBeGreaterThan(1)
    expect(result.current.scale).toBeLessThanOrEqual(1.1)
  })

  it('zooms out', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    act(() => {
      result.current.zoomIn()
      result.current.zoomOut()
    })

    expect(result.current.scale).toBe(1)
  })

  it('limits zoom range', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    act(() => {
      for (let i = 0; i < 20; i++) {
        result.current.zoomIn()
      }
    })

    expect(result.current.scale).toBeLessThanOrEqual(2)

    act(() => {
      for (let i = 0; i < 20; i++) {
        result.current.zoomOut()
      }
    })

    expect(result.current.scale).toBeGreaterThanOrEqual(0.5)
  })

  it('sets zoom directly', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    act(() => {
      result.current.setZoom(1.5)
    })

    expect(result.current.scale).toBe(1.5)
  })

  it('resets zoom and offset', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    act(() => {
      result.current.zoomIn()
      result.current.pan(50, 50)
    })

    act(() => {
      result.current.reset()
    })

    expect(result.current.scale).toBe(1)
    expect(result.current.offset).toEqual({ x: 0, y: 0 })
  })

  it('pans canvas', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    act(() => {
      result.current.pan(50, 100)
    })

    expect(result.current.offset).toEqual({ x: 50, y: 100 })
  })

  it('converts screen to canvas coordinates', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    const canvasCoords = result.current.screenToCanvasCoords(100, 100)
    expect(typeof canvasCoords.x).toBe('number')
    expect(typeof canvasCoords.y).toBe('number')
  })

  it('converts canvas to screen coordinates', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))

    const screenCoords = result.current.canvasToScreenCoords(50, 50)
    expect(typeof screenCoords.x).toBe('number')
    expect(typeof screenCoords.y).toBe('number')
  })

  it('has canvas ref', () => {
    const { result } = renderHook(() => useCanvas(1200, 600))
    expect(result.current.canvasRef).toBeDefined()
    expect(result.current.canvasRef.current).toBeNull() // No DOM attached
  })
})
