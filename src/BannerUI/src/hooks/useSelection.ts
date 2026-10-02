'use client'

import { useCallback, useState } from 'react'

export function useSelection() {
  const [selectedComponentId, setSelectedComponentId] = useState<string | null>(null)
  const [multiSelectIds, setMultiSelectIds] = useState<Set<string>>(new Set())

  const selectComponent = useCallback((id: string) => {
    setSelectedComponentId(id)
    setMultiSelectIds(new Set())
  }, [])

  const deselectComponent = useCallback(() => {
    setSelectedComponentId(null)
    setMultiSelectIds(new Set())
  }, [])

  const toggleMultiSelect = useCallback((id: string, isCtrlKey: boolean) => {
    if (!isCtrlKey) {
      selectComponent(id)
      return
    }

    setMultiSelectIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
    setSelectedComponentId(id)
  }, [selectComponent])

  const addToMultiSelect = useCallback((id: string) => {
    setMultiSelectIds((prev) => new Set(prev).add(id))
    setSelectedComponentId((current) => current ?? id)
  }, [])

  const removeFromMultiSelect = useCallback((id: string) => {
    setMultiSelectIds((prev) => {
      const next = new Set(prev)
      next.delete(id)
      return next
    })
  }, [])

  const isSelected = useCallback((id: string) => {
    return selectedComponentId === id
  }, [selectedComponentId])

  const isMultiSelected = useCallback((id: string) => {
    return multiSelectIds.has(id)
  }, [multiSelectIds])

  const getAllSelected = useCallback(() => {
    const all = new Set(multiSelectIds)
    if (selectedComponentId) {
      all.add(selectedComponentId)
    }
    return Array.from(all)
  }, [selectedComponentId, multiSelectIds])

  return {
    selectedComponentId,
    multiSelectIds: Array.from(multiSelectIds),
    selectComponent,
    deselectComponent,
    toggleMultiSelect,
    addToMultiSelect,
    removeFromMultiSelect,
    isSelected,
    isMultiSelected,
    getAllSelected,
  }
}
