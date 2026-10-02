import { renderHook } from '@testing-library/react'
import { useKeyboardShortcuts } from '@/hooks/useKeyboardShortcuts'

describe('useKeyboardShortcuts', () => {
  const originalPlatform = navigator.platform

  beforeEach(() => {
    jest.clearAllMocks()
  })

  afterEach(() => {
    Object.defineProperty(navigator, 'platform', { value: originalPlatform, configurable: true })
  })

  it('registers keyboard shortcuts on mount', () => {
    const addEventListenerSpy = jest.spyOn(window, 'addEventListener')
    const onSave = jest.fn()

    renderHook(() => useKeyboardShortcuts({ onSave }))

    expect(addEventListenerSpy).toHaveBeenCalledWith('keydown', expect.any(Function))
    addEventListenerSpy.mockRestore()
  })

  it('unregisters keyboard shortcuts on unmount', () => {
    const removeEventListenerSpy = jest.spyOn(window, 'removeEventListener')
    const onSave = jest.fn()

    const { unmount } = renderHook(() => useKeyboardShortcuts({ onSave }))
    unmount()

    expect(removeEventListenerSpy).toHaveBeenCalledWith('keydown', expect.any(Function))
    removeEventListenerSpy.mockRestore()
  })

  it('calls onSave for Ctrl+S', () => {
    const onSave = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onSave }))

    const event = new KeyboardEvent('keydown', {
      key: 's',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onSave).toHaveBeenCalled()
  })

  it('calls onSave for Cmd+S on Mac', () => {
    const onSave = jest.fn()
    Object.defineProperty(navigator, 'platform', {
      value: 'MacIntel',
      configurable: true,
    })

    renderHook(() => useKeyboardShortcuts({ onSave }))

    const event = new KeyboardEvent('keydown', {
      key: 's',
      metaKey: true,
    })
    window.dispatchEvent(event)

    expect(onSave).toHaveBeenCalled()
  })

  it('calls onUndo for Ctrl+Z', () => {
    const onUndo = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onUndo }))

    const event = new KeyboardEvent('keydown', {
      key: 'z',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onUndo).toHaveBeenCalled()
  })

  it('calls onRedo for Ctrl+Shift+Z', () => {
    const onRedo = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onRedo }))

    const event = new KeyboardEvent('keydown', {
      key: 'z',
      ctrlKey: true,
      shiftKey: true,
    })
    window.dispatchEvent(event)

    expect(onRedo).toHaveBeenCalled()
  })

  it('calls onRedo for Ctrl+Y', () => {
    const onRedo = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onRedo }))

    const event = new KeyboardEvent('keydown', {
      key: 'y',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onRedo).toHaveBeenCalled()
  })

  it('calls onDelete for Delete key', () => {
    const onDelete = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onDelete }))

    const event = new KeyboardEvent('keydown', {
      key: 'Delete',
    })
    window.dispatchEvent(event)

    expect(onDelete).toHaveBeenCalled()
  })

  it('calls onDelete for Backspace key', () => {
    const onDelete = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onDelete }))

    const event = new KeyboardEvent('keydown', {
      key: 'Backspace',
    })
    window.dispatchEvent(event)

    expect(onDelete).toHaveBeenCalled()
  })

  it('calls onSelectAll for Ctrl+A', () => {
    const onSelectAll = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onSelectAll }))

    const event = new KeyboardEvent('keydown', {
      key: 'a',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onSelectAll).toHaveBeenCalled()
  })

  it('calls onCopy for Ctrl+C', () => {
    const onCopy = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onCopy }))

    const event = new KeyboardEvent('keydown', {
      key: 'c',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onCopy).toHaveBeenCalled()
  })

  it('calls onPaste for Ctrl+V', () => {
    const onPaste = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onPaste }))

    const event = new KeyboardEvent('keydown', {
      key: 'v',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onPaste).toHaveBeenCalled()
  })

  it('calls onDuplicate for Ctrl+D', () => {
    const onDuplicate = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onDuplicate }))

    const event = new KeyboardEvent('keydown', {
      key: 'd',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onDuplicate).toHaveBeenCalled()
  })

  it('calls onZoomIn for Ctrl+Plus', () => {
    const onZoomIn = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onZoomIn }))

    const event = new KeyboardEvent('keydown', {
      key: '+',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onZoomIn).toHaveBeenCalled()
  })

  it('calls onZoomOut for Ctrl+Minus', () => {
    const onZoomOut = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onZoomOut }))

    const event = new KeyboardEvent('keydown', {
      key: '-',
      ctrlKey: true,
    })
    window.dispatchEvent(event)

    expect(onZoomOut).toHaveBeenCalled()
  })

  it('does not trigger shortcuts in input fields', () => {
    const onSave = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onSave }))

    const input = document.createElement('input')
    document.body.appendChild(input)
    input.focus()

    input.dispatchEvent(new KeyboardEvent('keydown', { key: 's', ctrlKey: true, bubbles: true }))

    expect(onSave).not.toHaveBeenCalled()
    document.body.removeChild(input)
  })

  it('does not trigger shortcuts in textarea', () => {
    const onDelete = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onDelete }))

    const textarea = document.createElement('textarea')
    document.body.appendChild(textarea)
    textarea.focus()

    textarea.dispatchEvent(new KeyboardEvent('keydown', { key: 'Delete', bubbles: true }))

    expect(onDelete).not.toHaveBeenCalled()
    document.body.removeChild(textarea)
  })

  it('handles multiple shortcuts in sequence', () => {
    const onSave = jest.fn()
    const onUndo = jest.fn()
    const onRedo = jest.fn()

    renderHook(() => useKeyboardShortcuts({ onSave, onUndo, onRedo }))

    // Save
    window.dispatchEvent(
      new KeyboardEvent('keydown', { key: 's', ctrlKey: true })
    )
    expect(onSave).toHaveBeenCalledTimes(1)

    // Undo
    window.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'z', ctrlKey: true })
    )
    expect(onUndo).toHaveBeenCalledTimes(1)

    // Redo
    window.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'z', ctrlKey: true, shiftKey: true })
    )
    expect(onRedo).toHaveBeenCalledTimes(1)
  })

  it('prevents default browser behavior', () => {
    const onSave = jest.fn()
    renderHook(() => useKeyboardShortcuts({ onSave }))

    const event = new KeyboardEvent('keydown', {
      key: 's',
      ctrlKey: true,
    })
    const preventDefaultSpy = jest.spyOn(event, 'preventDefault')
    window.dispatchEvent(event)

    expect(preventDefaultSpy).toHaveBeenCalled()
  })
})
