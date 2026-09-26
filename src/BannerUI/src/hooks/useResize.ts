'use client'

import { useCallback, useState } from 'react'
import { BannerComponent } from '@/types/banner'
import { validation } from '@/utils/validation'

export type ResizeHandle = 'nw' | 'n' | 'ne' | 'w' | 'e' | 'sw' | 's' | 'se'

export interface ResizeState {
  isResizing: boolean
  resizedComponentId: string | null
  handle: ResizeHandle | null
  startX: number
  startY: number
  startWidth: number
  startHeight: number
  minWidth: number
  minHeight: number
}

const MIN_SIZE = 20

export function useResize(
  components: BannerComponent[],
  canvasWidth: number,
  canvasHeight: number
) {
  const [resizeState, setResizeState] = useState<ResizeState>({
    isResizing: false,
    resizedComponentId: null,
    handle: null,
    startX: 0,
    startY: 0,
    startWidth: 0,
    startHeight: 0,
    minWidth: MIN_SIZE,
    minHeight: MIN_SIZE,
  })

  const startResize = useCallback(
    (componentId: string, handle: ResizeHandle, startX: number, startY: number) => {
      const component = components.find((c) => c.id === componentId)
      if (!component) return

      setResizeState({
        isResizing: true,
        resizedComponentId: componentId,
        handle,
        startX,
        startY,
        startWidth: component.width,
        startHeight: component.height,
        minWidth: MIN_SIZE,
        minHeight: MIN_SIZE,
      })
    },
    [components]
  )

  const calculateResize = useCallback(
    (currentX: number, currentY: number) => {
      if (!resizeState.isResizing || !resizeState.handle) {
        return null
      }

      const component = components.find((c) => c.id === resizeState.resizedComponentId)
      if (!component) return null

      const deltaX = currentX - resizeState.startX
      const deltaY = currentY - resizeState.startY

      let newWidth = resizeState.startWidth
      let newHeight = resizeState.startHeight
      let newX = component.x
      let newY = component.y

      const handle = resizeState.handle

      // Horizontal resize
      if (handle.includes('e')) {
        newWidth = Math.max(resizeState.minWidth, resizeState.startWidth + deltaX)
      } else if (handle.includes('w')) {
        newWidth = Math.max(resizeState.minWidth, resizeState.startWidth - deltaX)
        if (newWidth !== resizeState.startWidth) {
          newX = component.x + (resizeState.startWidth - newWidth)
        }
      }

      // Vertical resize
      if (handle.includes('s')) {
        newHeight = Math.max(resizeState.minHeight, resizeState.startHeight + deltaY)
      } else if (handle.includes('n')) {
        newHeight = Math.max(resizeState.minHeight, resizeState.startHeight - deltaY)
        if (newHeight !== resizeState.startHeight) {
          newY = component.y + (resizeState.startHeight - newHeight)
        }
      }

      // Clamp to canvas bounds
      if (newX < 0) newX = 0
      if (newY < 0) newY = 0
      if (newX + newWidth > canvasWidth) {
        newWidth = canvasWidth - newX
      }
      if (newY + newHeight > canvasHeight) {
        newHeight = canvasHeight - newY
      }

      // Validate
      const sizeValidation = validation.isValidComponentSize(newWidth, newHeight)
      if (!sizeValidation.valid) {
        return null
      }

      return { x: newX, y: newY, width: newWidth, height: newHeight }
    },
    [resizeState, components, canvasWidth, canvasHeight]
  )

  const endResize = useCallback(() => {
    setResizeState({
      isResizing: false,
      resizedComponentId: null,
      handle: null,
      startX: 0,
      startY: 0,
      startWidth: 0,
      startHeight: 0,
      minWidth: MIN_SIZE,
      minHeight: MIN_SIZE,
    })
  }, [])

  const getResizeStyle = useCallback((component: BannerComponent) => {
    if (resizeState.resizedComponentId !== component.id) {
      return {}
    }

    const newSize = calculateResize(
      resizeState.startX + (component.x - resizeState.startX),
      resizeState.startY + (component.y - resizeState.startY)
    )

    if (!newSize) return {}

    return {
      x: newSize.x,
      y: newSize.y,
      width: newSize.width,
      height: newSize.height,
    }
  }, [resizeState, calculateResize])

  return {
    resizeState,
    startResize,
    calculateResize,
    endResize,
    getResizeStyle,
    isResizing: resizeState.isResizing,
    resizedComponentId: resizeState.resizedComponentId,
  }
}
