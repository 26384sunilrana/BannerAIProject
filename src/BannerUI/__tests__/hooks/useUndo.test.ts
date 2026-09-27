import { renderHook, act } from '@testing-library/react'
import { useUndo } from '@/hooks/useUndo'

interface TestState {
  value: number
}

describe('useUndo', () => {
  const initialState: TestState = { value: 0 }

  it('initializes with initial state', () => {
    const { result } = renderHook(() => useUndo(initialState))

    expect(result.current.getCurrentState()).toEqual(initialState)
    expect(result.current.canUndo).toBe(false)
    expect(result.current.canRedo).toBe(false)
  })

  it('pushes new states', () => {
    const { result } = renderHook(() => useUndo(initialState))

    act(() => {
      result.current.push({ value: 1 }, 'increment')
    })

    expect(result.current.getCurrentState()).toEqual({ value: 1 })
    expect(result.current.canUndo).toBe(true)
    expect(result.current.canRedo).toBe(false)
  })

  it('supports undo', () => {
    const { result } = renderHook(() => useUndo(initialState))

    act(() => {
      result.current.push({ value: 1 }, 'increment')
    })

    act(() => {
      result.current.undo()
    })

    expect(result.current.getCurrentState()).toEqual(initialState)
    expect(result.current.canUndo).toBe(false)
    expect(result.current.canRedo).toBe(true)
  })

  it('supports redo', () => {
    const { result } = renderHook(() => useUndo(initialState))

    act(() => {
      result.current.push({ value: 1 }, 'increment')
    })

    act(() => {
      result.current.undo()
    })

    act(() => {
      result.current.redo()
    })

    expect(result.current.getCurrentState()).toEqual({ value: 1 })
    expect(result.current.canUndo).toBe(true)
    expect(result.current.canRedo).toBe(false)
  })

  it('clears future history when pushing after undo', () => {
    const { result } = renderHook(() => useUndo(initialState))

    act(() => {
      result.current.push({ value: 1 }, 'first')
      result.current.push({ value: 2 }, 'second')
    })

    act(() => {
      result.current.undo()
    })

    act(() => {
      result.current.push({ value: 3 }, 'third')
    })

    expect(result.current.getCurrentState()).toEqual({ value: 3 })
    expect(result.current.canRedo).toBe(false)
  })

  it('limits history size', () => {
    const { result } = renderHook(() => useUndo(initialState))

    // Push more than MAX_HISTORY (50) items
    act(() => {
      for (let i = 0; i < 60; i++) {
        result.current.push({ value: i }, `push ${i}`)
      }
    })

    expect(result.current.historyLength).toBeLessThanOrEqual(50)
  })

  it('clears history', () => {
    const { result } = renderHook(() => useUndo(initialState))

    act(() => {
      result.current.push({ value: 1 }, 'first')
      result.current.push({ value: 2 }, 'second')
    })

    act(() => {
      result.current.clear()
    })

    expect(result.current.getCurrentState()).toEqual(initialState)
    expect(result.current.canUndo).toBe(false)
    expect(result.current.historyLength).toBe(1)
  })

  it('provides history entries', () => {
    const { result } = renderHook(() => useUndo(initialState))

    act(() => {
      result.current.push({ value: 1 }, 'first')
      result.current.push({ value: 2 }, 'second')
    })

    const history = result.current.getHistory()
    expect(history).toHaveLength(3)
    expect(history[0].description).toBe('Initial state')
    expect(history[1].description).toBe('first')
    expect(history[2].description).toBe('second')
    expect(history[2].isCurrent).toBe(true)
  })
})
