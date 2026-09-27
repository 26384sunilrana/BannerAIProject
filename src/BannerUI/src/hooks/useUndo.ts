'use client'

import { useCallback, useState } from 'react'

export interface HistoryEntry<T> {
  state: T
  timestamp: number
  description: string
}

const MAX_HISTORY = 50

export function useUndo<T>(initialState: T) {
  const [history, setHistory] = useState<HistoryEntry<T>[]>([
    {
      state: initialState,
      timestamp: Date.now(),
      description: 'Initial state',
    },
  ])
  const [currentIndex, setCurrentIndex] = useState(0)

  const push = useCallback(
    (newState: T, description: string = 'Change') => {
      setHistory((prev) => {
        // Remove any future history if we're not at the end
        const trimmed = prev.slice(0, currentIndex + 1)

        // Add new entry
        const updated = [
          ...trimmed,
          {
            state: newState,
            timestamp: Date.now(),
            description,
          },
        ]

        // Limit history size
        if (updated.length > MAX_HISTORY) {
          updated.shift()
        }

        return updated
      })

      setCurrentIndex((prev) => Math.min(prev + 1, history.length))
    },
    [currentIndex, history.length]
  )

  const undo = useCallback(() => {
    setCurrentIndex((prev) => {
      const newIndex = Math.max(0, prev - 1)
      return newIndex
    })
  }, [])

  const redo = useCallback(() => {
    setCurrentIndex((prev) => {
      const newIndex = Math.min(prev + 1, history.length - 1)
      return newIndex
    })
  }, [history.length])

  const canUndo = currentIndex > 0
  const canRedo = currentIndex < history.length - 1

  const getCurrentState = useCallback(() => {
    return history[currentIndex]?.state || initialState
  }, [history, currentIndex, initialState])

  const clear = useCallback(() => {
    setHistory([
      {
        state: initialState,
        timestamp: Date.now(),
        description: 'Initial state',
      },
    ])
    setCurrentIndex(0)
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
