'use client'

import { useCallback, useState, useRef, useEffect } from 'react'
import { screenToCanvas, canvasToScreen, Point } from '@/utils/positioning'

export interface CanvasState {
  scale: number
  offsetX: number
  offsetY: number
  width: number
  height: number
}

export function useCanvas(
  initialWidth: number = 1200,
  initialHeight: number = 600
) {
  const [scale, setScale] = useState(1)
  const [offset, setOffset] = useState({ x: 0, y: 0 })
  const canvasRef = useRef<HTMLDivElement>(null)

  const zoomIn = useCallback(() => {
    setScale((prev) => Math.min(prev + 0.1, 2))
  }, [])

  const zoomOut = useCallback(() => {
    setScale((prev) => Math.max(prev - 0.1, 0.5))
  }, [])

  const fitToCanvas = useCallback(() => {
    if (!canvasRef.current) return
    const container = canvasRef.current.parentElement
    if (!container) return

    const scaleX = container.clientWidth / initialWidth
    const scaleY = container.clientHeight / initialHeight
    const newScale = Math.min(scaleX, scaleY) * 0.9
    setScale(newScale)
    setOffset({ x: 0, y: 0 })
  }, [initialWidth, initialHeight])

  const reset = useCallback(() => {
    setScale(1)
    setOffset({ x: 0, y: 0 })
  }, [])

  const screenToCanvasCoords = useCallback(
    (screenX: number, screenY: number): Point => {
      if (!canvasRef.current) return { x: screenX, y: screenY }
      const rect = canvasRef.current.getBoundingClientRect()
      return screenToCanvas(screenX, screenY, rect, scale)
    },
    [scale]
  )

  const canvasToScreenCoords = useCallback(
    (canvasX: number, canvasY: number): Point => {
      if (!canvasRef.current) return { canvasX, canvasY }
      const rect = canvasRef.current.getBoundingClientRect()
      return canvasToScreen(canvasX, canvasY, rect, scale)
    },
    [scale]
  )

  const pan = useCallback((deltaX: number, deltaY: number) => {
    setOffset((prev) => ({
      x: prev.x + deltaX,
      y: prev.y + deltaY,
    }))
  }, [])

  const setZoom = useCallback((newScale: number) => {
    setScale(Math.max(0.1, Math.min(newScale, 3)))
  }, [])

  useEffect(() => {
    const handleWheel = (e: WheelEvent) => {
      if (!canvasRef.current?.contains(e.target as Node)) return
      if (!e.ctrlKey && !e.metaKey) return

      e.preventDefault()
      const delta = e.deltaY > 0 ? -0.1 : 0.1
      setScale((prev) => Math.max(0.5, Math.min(prev + delta, 2)))
    }

    window.addEventListener('wheel', handleWheel, { passive: false })
    return () => window.removeEventListener('wheel', handleWheel)
  }, [])

  return {
    canvasRef,
    scale,
    offset,
    width: initialWidth,
    height: initialHeight,
    zoomIn,
    zoomOut,
    fitToCanvas,
    reset,
    setZoom,
    pan,
    screenToCanvasCoords,
    canvasToScreenCoords,
  }
}
