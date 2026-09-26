'use client'

import { useCallback, useState } from 'react'
import { BannerComponent } from '@/types/banner'
import { clampPosition, snapToGrid, Point } from '@/utils/positioning'

export interface DragState {
  isDragging: boolean
  draggedComponentId: string | null
  dragStartPos: Point | null
  dragStartComponentPos: Point | null
  currentDragPos: Point | null
}

export function useComponentDrag(
  components: BannerComponent[],
  canvasWidth: number,
  canvasHeight: number,
  gridSize: number = 8,
  enableSnap: boolean = true
) {
  const [dragState, setDragState] = useState<DragState>({
    isDragging: false,
    draggedComponentId: null,
    dragStartPos: null,
    dragStartComponentPos: null,
    currentDragPos: null,
  })

  const startDrag = useCallback((componentId: string, startX: number, startY: number) => {
    const component = components.find((c) => c.id === componentId)
    if (!component) return

    setDragState({
      isDragging: true,
      draggedComponentId: componentId,
      dragStartPos: { x: startX, y: startY },
      dragStartComponentPos: { x: component.x, y: component.y },
      currentDragPos: { x: startX, y: startY },
    })
  }, [components])

  const updateDragPosition = useCallback((currentX: number, currentY: number) => {
    setDragState((prev) => {
      if (!prev.isDragging || !prev.dragStartPos || !prev.dragStartComponentPos) {
        return prev
      }

      const deltaX = currentX - prev.dragStartPos.x
      const deltaY = currentY - prev.dragStartPos.y

      let newX = prev.dragStartComponentPos.x + deltaX
      let newY = prev.dragStartComponentPos.y + deltaY

      if (enableSnap) {
        newX = snapToGrid(newX, gridSize)
        newY = snapToGrid(newY, gridSize)
      }

      // Get the component to check bounds
      const component = components.find((c) => c.id === prev.draggedComponentId)
      if (component) {
        const clamped = clampPosition(
          { x: newX, y: newY },
          { width: component.width, height: component.height },
          { width: canvasWidth, height: canvasHeight }
        )
        newX = clamped.x
        newY = clamped.y
      }

      return {
        ...prev,
        currentDragPos: { x: currentX, y: currentY },
      }
    })

    return {
      x: dragState.dragStartComponentPos?.x || 0 + ((currentX - (dragState.dragStartPos?.x || 0))),
      y: dragState.dragStartComponentPos?.y || 0 + ((currentY - (dragState.dragStartPos?.y || 0))),
    }
  }, [components, canvasWidth, canvasHeight, gridSize, enableSnap, dragState])

  const endDrag = useCallback(() => {
    setDragState({
      isDragging: false,
      draggedComponentId: null,
      dragStartPos: null,
      dragStartComponentPos: null,
      currentDragPos: null,
    })
  }, [])

  const getFinalPosition = useCallback(() => {
    if (!dragState.isDragging || !dragState.dragStartPos || !dragState.dragStartComponentPos) {
      return null
    }

    const deltaX = (dragState.currentDragPos?.x || dragState.dragStartPos.x) - dragState.dragStartPos.x
    const deltaY = (dragState.currentDragPos?.y || dragState.dragStartPos.y) - dragState.dragStartPos.y

    let finalX = dragState.dragStartComponentPos.x + deltaX
    let finalY = dragState.dragStartComponentPos.y + deltaY

    if (enableSnap) {
      finalX = snapToGrid(finalX, gridSize)
      finalY = snapToGrid(finalY, gridSize)
    }

    const component = components.find((c) => c.id === dragState.draggedComponentId)
    if (component) {
      const clamped = clampPosition(
        { x: finalX, y: finalY },
        { width: component.width, height: component.height },
        { width: canvasWidth, height: canvasHeight }
      )
      return clamped
    }

    return { x: finalX, y: finalY }
  }, [dragState, components, canvasWidth, canvasHeight, gridSize, enableSnap])

  return {
    dragState,
    startDrag,
    updateDragPosition,
    endDrag,
    getFinalPosition,
    isDragging: dragState.isDragging,
    draggedComponentId: dragState.draggedComponentId,
  }
}
