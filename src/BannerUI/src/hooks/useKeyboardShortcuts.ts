'use client'

import { useEffect, useCallback } from 'react'

export interface KeyboardShortcuts {
  onSave?: () => void
  onUndo?: () => void
  onRedo?: () => void
  onDelete?: () => void
  onSelectAll?: () => void
  onCopy?: () => void
  onPaste?: () => void
  onDuplicate?: () => void
  onZoomIn?: () => void
  onZoomOut?: () => void
}

export function useKeyboardShortcuts({
  onSave,
  onUndo,
  onRedo,
  onDelete,
  onSelectAll,
  onCopy,
  onPaste,
  onDuplicate,
  onZoomIn,
  onZoomOut,
}: KeyboardShortcuts) {
  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      // Don't trigger shortcuts when typing in input
      const target = event.target as HTMLElement
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA') {
        return
      }

      const isMac = /Mac|iPhone|iPad|iPod/.test(navigator.platform)
      const cmdKey = isMac ? event.metaKey : event.ctrlKey

      // Ctrl/Cmd + S: Save
      if (cmdKey && event.key === 's') {
        event.preventDefault()
        onSave?.()
      }

      // Ctrl/Cmd + Z: Undo
      if (cmdKey && event.key === 'z' && !event.shiftKey) {
        event.preventDefault()
        onUndo?.()
      }

      // Ctrl/Cmd + Shift + Z: Redo
      if (cmdKey && event.key === 'z' && event.shiftKey) {
        event.preventDefault()
        onRedo?.()
      }

      // Ctrl/Cmd + Y: Redo (alternative)
      if (cmdKey && event.key === 'y') {
        event.preventDefault()
        onRedo?.()
      }

      // Delete: Delete selected component
      if (event.key === 'Delete' || event.key === 'Backspace') {
        event.preventDefault()
        onDelete?.()
      }

      // Ctrl/Cmd + A: Select all
      if (cmdKey && event.key === 'a') {
        event.preventDefault()
        onSelectAll?.()
      }

      // Ctrl/Cmd + C: Copy
      if (cmdKey && event.key === 'c') {
        event.preventDefault()
        onCopy?.()
      }

      // Ctrl/Cmd + V: Paste
      if (cmdKey && event.key === 'v') {
        event.preventDefault()
        onPaste?.()
      }

      // Ctrl/Cmd + D: Duplicate
      if (cmdKey && event.key === 'd') {
        event.preventDefault()
        onDuplicate?.()
      }

      // Ctrl/Cmd + Plus: Zoom in
      if ((cmdKey || event.ctrlKey) && (event.key === '+' || event.key === '=')) {
        event.preventDefault()
        onZoomIn?.()
      }

      // Ctrl/Cmd + Minus: Zoom out
      if ((cmdKey || event.ctrlKey) && event.key === '-') {
        event.preventDefault()
        onZoomOut?.()
      }
    },
    [onSave, onUndo, onRedo, onDelete, onSelectAll, onCopy, onPaste, onDuplicate, onZoomIn, onZoomOut]
  )

  useEffect(() => {
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [handleKeyDown])
}
