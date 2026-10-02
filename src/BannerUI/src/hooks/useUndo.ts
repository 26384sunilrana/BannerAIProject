'use client'

import { useCallback, useState } from 'react'

export interface HistoryEntry<T> {
  state: T
  timestamp: number
  description: string
}

const MAX_HISTORY = 50

export function useUndo<T>(initialState: T) {
  // entries and position live together so several changes made in one batch cannot overwrite each other
  const [timeline, setTimeline] = useState<{ history: HistoryEntry<T>[]; currentIndex: number }>({
    history: [{ state: initialState, timestamp: Date.now(), description: 'Initial state' }],
    currentIndex: 0,
  })
  const { history, currentIndex } = timeline

  const push = useCallback((newState: T, description: string = 'Change') => {
    setTimeline((prev) => {
      // anything after the current position is dropped, then the new entry is added
      let updated = [
        ...prev.history.slice(0, prev.currentIndex + 1),
        { state: newState, timestamp: Date.now(), description },
      ]
      if (updated.length > MAX_HISTORY) updated = updated.slice(updated.length - MAX_HISTORY)
      return { history: updated, currentIndex: updated.length - 1 }
    })
  }, [])

  const undo = useCallback(() => {
    setTimeline((prev) => ({ ...prev, currentIndex: Math.max(0, prev.currentIndex - 1) }))
  }, [])

  const redo = useCallback(() => {
    setTimeline((prev) => ({ ...prev, currentIndex: Math.min(prev.currentIndex + 1, prev.history.length - 1) }))
  }, [])

  const canUndo = currentIndex > 0
  const canRedo = currentIndex < history.length - 1

  const getCurrentState = useCallback(() => {
    return history[currentIndex]?.state || initialState
  }, [history, currentIndex, initialState])

  const clear = useCallback(() => {
    setTimeline({ history: [{ state: initialState, timestamp: Date.now(), description: 'Initial state' }], currentIndex: 0 })
  }, [initialState])

  const getHistory = useCallback(() => {
    return history.map((entry, index) => ({
      ...entry,
      isCurrent: index === currentIndex,
    }))
  }, [history, currentIndex])

  return {
    push,
    undo,
    redo,
    canUndo,
    canRedo,
    getCurrentState,
    clear,
    getHistory,
    currentIndex,
    historyLength: history.length,
  }
}
