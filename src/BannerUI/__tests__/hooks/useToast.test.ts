import { renderHook, act } from '@testing-library/react'
import { useToast } from '@/hooks/useToast'

describe('useToast', () => {
  beforeEach(() => {
    jest.useFakeTimers()
  })

  afterEach(() => {
    jest.useRealTimers()
  })

  it('initializes with empty messages', () => {
    const { result } = renderHook(() => useToast())
    expect(result.current.messages).toEqual([])
  })

  it('adds success toast', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.success('Operation successful')
    })

    expect(result.current.messages).toHaveLength(1)
    expect(result.current.messages[0].type).toBe('success')
    expect(result.current.messages[0].message).toBe('Operation successful')
  })

  it('adds error toast', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.error('Something went wrong')
    })

    expect(result.current.messages).toHaveLength(1)
    expect(result.current.messages[0].type).toBe('error')
    expect(result.current.messages[0].message).toBe('Something went wrong')
  })

  it('adds warning toast', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.warning('Please note')
    })

    expect(result.current.messages).toHaveLength(1)
    expect(result.current.messages[0].type).toBe('warning')
  })

  it('adds info toast', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.info('FYI')
    })

    expect(result.current.messages).toHaveLength(1)
    expect(result.current.messages[0].type).toBe('info')
  })

  it('uses show method with custom type', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.show('Custom message', 'success')
    })

    expect(result.current.messages).toHaveLength(1)
    expect(result.current.messages[0].message).toBe('Custom message')
  })

  it('removes toast by id', () => {
    const { result } = renderHook(() => useToast())

    let toastId = ''

    act(() => {
      toastId = result.current.success('Message')
    })

    expect(result.current.messages).toHaveLength(1)

    act(() => {
      result.current.remove(toastId)
    })

    expect(result.current.messages).toHaveLength(0)
  })

  it('auto-dismisses success toast', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.success('Success', 3000)
    })

    expect(result.current.messages).toHaveLength(1)

    act(() => {
      jest.advanceTimersByTime(3000)
    })

    expect(result.current.messages).toHaveLength(0)
  })

  it('auto-dismisses error toast with default duration', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.error('Error')
    })

    expect(result.current.messages).toHaveLength(1)

    act(() => {
      jest.advanceTimersByTime(7000)
    })

    expect(result.current.messages).toHaveLength(0)
  })

  it('auto-dismisses with custom duration', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.show('Custom', 'info', 1000)
    })

    act(() => {
      jest.advanceTimersByTime(1000)
    })

    expect(result.current.messages).toHaveLength(0)
  })

  it('handles multiple toasts', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.success('First')
      result.current.error('Second')
      result.current.warning('Third')
    })

    expect(result.current.messages).toHaveLength(3)
  })

  it('removes specific toast from multiple', () => {
    const { result } = renderHook(() => useToast())

    let id1 = '', id2 = '', id3 = ''

    act(() => {
      id1 = result.current.success('First')
      id2 = result.current.error('Second')
      id3 = result.current.warning('Third')
    })

    act(() => {
      result.current.remove(id2)
    })

    expect(result.current.messages).toHaveLength(2)
    expect(result.current.messages.find((m) => m.id === id2)).toBeUndefined()
    expect(result.current.messages.find((m) => m.id === id1)).toBeDefined()
    expect(result.current.messages.find((m) => m.id === id3)).toBeDefined()
  })

  it('generates unique toast ids', () => {
    const { result } = renderHook(() => useToast())

    let id1 = '', id2 = ''

    act(() => {
      id1 = result.current.success('First')
      id2 = result.current.success('Second')
    })

    expect(id1).not.toBe(id2)
  })

  it('handles zero duration for persistent toast', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      result.current.show('Persistent', 'info', 0)
    })

    expect(result.current.messages).toHaveLength(1)

    act(() => {
      jest.advanceTimersByTime(10000)
    })

    expect(result.current.messages).toHaveLength(1)
  })

  it('error toast has longer default duration than others', () => {
    const { result: successResult } = renderHook(() => useToast())
    const { result: errorResult } = renderHook(() => useToast())

    act(() => {
      successResult.current.success('Success')
    })

    act(() => {
      errorResult.current.error('Error')
    })

    // Success auto-dismisses at 5000
    act(() => {
      jest.advanceTimersByTime(5000)
    })

    expect(successResult.current.messages).toHaveLength(0)

    // Error should still be there at 5000
    expect(errorResult.current.messages).toHaveLength(1)

    // Error auto-dismisses at 7000
    act(() => {
      jest.advanceTimersByTime(2000)
    })

    expect(errorResult.current.messages).toHaveLength(0)
  })

  it('supports rapid toast additions', () => {
    const { result } = renderHook(() => useToast())

    act(() => {
      for (let i = 0; i < 10; i++) {
        result.current.info(`Message ${i}`)
      }
    })

    expect(result.current.messages).toHaveLength(10)
  })

  it('returns toast id from all methods', () => {
    const { result } = renderHook(() => useToast())

    let successId = '', errorId = '', warningId = '', infoId = ''

    act(() => {
      successId = result.current.success('S')
      errorId = result.current.error('E')
      warningId = result.current.warning('W')
      infoId = result.current.info('I')
    })

    expect(successId).toBeTruthy()
    expect(errorId).toBeTruthy()
    expect(warningId).toBeTruthy()
    expect(infoId).toBeTruthy()
  })
})
