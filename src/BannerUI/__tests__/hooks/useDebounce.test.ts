import { renderHook, act } from '@testing-library/react'
import { useDebounce, useDebouncedCallback } from '@/hooks/useDebounce'

describe('useDebounce', () => {
  beforeEach(() => {
    jest.useFakeTimers()
  })

  afterEach(() => {
    jest.useRealTimers()
  })

  it('returns initial value immediately', () => {
    const { result } = renderHook(() => useDebounce('initial', 300))
    expect(result.current).toBe('initial')
  })

  it('debounces value changes', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    )

    expect(result.current).toBe('initial')

    rerender({ value: 'updated' })
    expect(result.current).toBe('initial')

    act(() => {
      jest.advanceTimersByTime(300)
    })

    expect(result.current).toBe('updated')
  })

  it('updates value after delay', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 500),
      { initialProps: { value: 'value1' } }
    )

    rerender({ value: 'value2' })
    act(() => {
      jest.advanceTimersByTime(500)
    })

    expect(result.current).toBe('value2')
  })

  it('cancels previous debounce on rapid changes', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    )

    rerender({ value: 'first' })
    act(() => {
      jest.advanceTimersByTime(150)
    })

    rerender({ value: 'second' })
    act(() => {
      jest.advanceTimersByTime(150)
    })

    expect(result.current).toBe('initial')

    act(() => {
      jest.advanceTimersByTime(150)
    })

    expect(result.current).toBe('second')
  })

  it('uses custom delay parameter', () => {
    const { result, rerender } = renderHook(
      ({ value, delay }) => useDebounce(value, delay),
      { initialProps: { value: 'initial', delay: 1000 } }
    )

    rerender({ value: 'updated', delay: 1000 })

    act(() => {
      jest.advanceTimersByTime(500)
    })

    expect(result.current).toBe('initial')

    act(() => {
      jest.advanceTimersByTime(500)
    })

    expect(result.current).toBe('updated')
  })

  it('handles cleanup on unmount', () => {
    const { unmount, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    )

    rerender({ value: 'updated' })
    unmount()

    // Should not throw when advancing timers
    act(() => {
      jest.advanceTimersByTime(300)
    })
  })

  it('debounces numeric values', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 1 } }
    )

    rerender({ value: 2 })
    expect(result.current).toBe(1)

    act(() => {
      jest.advanceTimersByTime(300)
    })

    expect(result.current).toBe(2)
  })

  it('debounces object values', () => {
    const obj1 = { name: 'test' }
    const obj2 = { name: 'updated' }

    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: obj1 } }
    )

    rerender({ value: obj2 })
    expect(result.current).toBe(obj1)

    act(() => {
      jest.advanceTimersByTime(300)
    })

    expect(result.current).toBe(obj2)
  })
})

describe('useDebouncedCallback', () => {
  beforeEach(() => {
    jest.useFakeTimers()
  })

  afterEach(() => {
    jest.useRealTimers()
  })

  it('debounces callback execution', () => {
    const callback = jest.fn()
    const { result } = renderHook(() =>
      useDebouncedCallback(callback, 300)
    )

    act(() => {
      result.current('arg1')
    })

    expect(callback).not.toHaveBeenCalled()

    act(() => {
      jest.advanceTimersByTime(300)
    })

    expect(callback).toHaveBeenCalledWith('arg1')
  })

  it('cancels previous callback on rapid calls', () => {
    const callback = jest.fn()
    const { result } = renderHook(() =>
      useDebouncedCallback(callback, 300)
    )

    act(() => {
      result.current('first')
    })

    act(() => {
      jest.advanceTimersByTime(150)
    })

    act(() => {
      result.current('second')
    })

    act(() => {
      jest.advanceTimersByTime(300)
    })

    expect(callback).toHaveBeenCalledTimes(1)
    expect(callback).toHaveBeenCalledWith('second')
  })

  it('uses custom delay for callback', () => {
    const callback = jest.fn()
    const { result } = renderHook(() =>
      useDebouncedCallback(callback, 500)
    )

    act(() => {
      result.current()
    })

    act(() => {
      jest.advanceTimersByTime(300)
    })

    expect(callback).not.toHaveBeenCalled()

    act(() => {
      jest.advanceTimersByTime(200)
    })

    expect(callback).toHaveBeenCalledTimes(1)
  })

  it('handles multiple arguments', () => {
    const callback = jest.fn()
    const { result } = renderHook(() =>
      useDebouncedCallback(callback, 300)
    )

    act(() => {
      result.current('arg1', 'arg2', { key: 'value' })
    })

    act(() => {
      jest.advanceTimersByTime(300)
    })

    expect(callback).toHaveBeenCalledWith('arg1', 'arg2', { key: 'value' })
  })

  it('cleans up timeout on unmount', () => {
    const callback = jest.fn()
    const { unmount, result } = renderHook(() =>
      useDebouncedCallback(callback, 300)
    )

    act(() => {
      result.current()
    })

    unmount()

    act(() => {
      jest.advanceTimersByTime(300)
    })

    expect(callback).not.toHaveBeenCalled()
  })
})
