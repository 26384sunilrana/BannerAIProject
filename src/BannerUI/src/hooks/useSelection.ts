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

    const newSet = new Set(multiSelectIds)
    if (newSet.has(id)) {
      newSet.delete(id)
    } else {
      newSet.add(id)
    }
    setMultiSelectIds(newSet)
    setSelectedComponentId(id)
  }, [multiSelectIds, selectComponent])

  const addToMultiSelect = useCallback((id: string) => {
    const newSet = new Set(multiSelectIds)
    newSet.add(id)
    setMultiSelectIds(newSet)
    if (!selectedComponentId) {
      setSelectedComponentId(id)
    }
  }, [multiSelectIds, selectedComponentId])

  const removeFromMultiSelect = useCallback((id: string) => {
    const newSet = new Set(multiSelectIds)
    newSet.delete(id)
    setMultiSelectIds(newSet)
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
