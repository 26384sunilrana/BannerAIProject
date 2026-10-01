'use client'

import React, { useCallback, useRef } from 'react'
import { BannerComponent } from '@/types/banner'
import { useCanvas } from '@/hooks/useCanvas'
import { useSelection } from '@/hooks/useSelection'
import { useComponentDrag } from '@/hooks/useComponentDrag'
import { useResize } from '@/hooks/useResize'
import { Button } from '@/components/Common'
import styles from './Canvas.module.css'

export interface CanvasProps {
  components: BannerComponent[]
  backgroundColor: string
  width: number
  height: number
  isPreviewMode?: boolean
  selectedComponentId?: string | null
  onComponentSelect: (id: string | null) => void
  onComponentMove: (id: string, x: number, y: number) => void
  onComponentResize: (id: string, width: number, height: number) => void
}

export const Canvas = React.forwardRef<HTMLDivElement, CanvasProps>(
  (
    {
      components,
      backgroundColor,
      width,
      height,
      isPreviewMode = false,
      selectedComponentId,
      onComponentSelect,
      onComponentMove,
      onComponentResize,
    },
    ref
  ) => {
    const canvas = useCanvas(width, height)
    const selection = useSelection()
    const drag = useComponentDrag(components, width, height, 8, true)
    const resize = useResize(components, width, height)
    const containerRef = useRef<HTMLDivElement>(null)

    const handleCanvasClick = useCallback(
      (e: React.MouseEvent) => {
        if (e.target === containerRef.current) {
          selection.deselectComponent()
          onComponentSelect(null)
        }
      },
      [selection, onComponentSelect]
    )

    const handleMouseMove = useCallback(
      (e: React.MouseEvent) => {
        // Pointer positions are divided by the zoom so one screen pixel moved is the right number of canvas pixels
        const x = e.clientX / canvas.scale
        const y = e.clientY / canvas.scale

        if (drag.isDragging) {
          drag.updateDragPosition(x, y)
        }

        if (resize.isResizing && resize.resizedComponentId) {
          const next = resize.calculateResize(x, y)
          if (next) {
            onComponentMove(resize.resizedComponentId, next.x, next.y)
            onComponentResize(resize.resizedComponentId, next.width, next.height)
          }
        }
      },
      [drag, resize, canvas.scale, onComponentMove, onComponentResize]
    )

    // Move the dragged component whenever the pointer position changes
    const { dragState, isDragging, draggedComponentId, getFinalPosition } = drag
    React.useEffect(() => {
      if (!isDragging || !draggedComponentId) return
      const position = getFinalPosition()
      if (position) onComponentMove(draggedComponentId, position.x, position.y)
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [dragState.currentDragPos])

    const handleMouseUp = useCallback(() => {
      drag.endDrag()
      resize.endResize()
    }, [drag, resize])

    React.useEffect(() => {
      window.addEventListener('mouseup', handleMouseUp)
      return () => window.removeEventListener('mouseup', handleMouseUp)
    }, [handleMouseUp])

    if (isPreviewMode) {
      return (
        <div
          ref={ref}
          className="flex-1 flex items-center justify-center bg-gray-100 p-8"
        >
          <div
            className={styles.canvas}
            style={{
              backgroundColor,
              width: `${width}px`,
              height: `${height}px`,
              boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
            }}
          >
            {components
              .filter((c) => c.isVisible)
              .map((component) => (
                <div
                  key={component.id}
                  data-component-id={component.id}
                  style={{
                    position: 'absolute',
                    left: `${component.x}px`,
                    top: `${component.y}px`,
                    width: `${component.width}px`,
                    height: `${component.height}px`,
                    zIndex: component.zIndex,
                    opacity: component.opacity,
                    transform: `rotate(${component.rotation}deg)`,
                  }}
                  className={styles.component}
                >
                  {component.type === 'text' && (
                    <div
                      style={{
                        color: (component.data as any).color,
                        fontSize: `${(component.data as any).fontSize}px`,
                        fontFamily: (component.data as any).fontFamily,
                      }}
                    >
                      {(component.data as any).content}
                    </div>
                  )}
                  {component.type === 'image' && (
                    <img
                      src={(component.data as any).mediaUrl}
                      alt={(component.data as any).alt}
                      style={{ width: '100%', height: '100%', objectFit: (component.data as any).objectFit }}
                    />
                  )}
                  {component.type === 'video' && (component.data as any).mediaUrl && (
                  <video
                    src={(component.data as any).mediaUrl}
                    muted={(component.data as any).muted !== false}
                    loop={Boolean((component.data as any).loop)}
                    autoPlay={Boolean((component.data as any).autoPlay)}
                    playsInline
                    style={{ width: '100%', height: '100%', objectFit: 'cover', pointerEvents: 'none' }}
                  />
                )}
                {component.type === 'graphics' && (
                    <div
                      style={{
                        width: '100%',
                        height: '100%',
                        backgroundColor: (component.data as any).fillColor,
                        borderStyle: 'solid',
                        borderRadius: (component.data as any).shapeType === 'circle' ? '50%' : undefined,
                        borderColor: (component.data as any).strokeColor,
                        borderWidth: (component.data as any).strokeWidth,
                      }}
                    />
                  )}
                </div>
              ))}
          </div>
        </div>
      )
    }

    return (
      <div ref={ref} className={styles.wrapper}>
        <div className={styles.controls}>
          <Button variant="ghost" size="sm" onClick={() => canvas.zoomOut()}>
            −
          </Button>
          <span className="text-sm font-medium w-12 text-center">
            {Math.round(canvas.scale * 100)}%
          </span>
          <Button variant="ghost" size="sm" onClick={() => canvas.zoomIn()}>
            +
          </Button>
          <div className="w-px h-6 bg-gray-300" />
          <Button variant="ghost" size="sm" onClick={() => canvas.fitToCanvas()}>
            Fit
          </Button>
          <Button variant="ghost" size="sm" onClick={() => canvas.reset()}>
            Reset
          </Button>
        </div>

        <div
          ref={containerRef}
          className={styles.container}
          onClick={handleCanvasClick}
          onMouseMove={handleMouseMove}
          style={{
            backgroundColor: '#f5f5f5',
          }}
        >
          <div
            ref={canvas.canvasRef}
            className={styles.canvas}
            style={{
              backgroundColor,
              width: `${width}px`,
              height: `${height}px`,
              transform: `scale(${canvas.scale})`,
              transformOrigin: 'top left',
            }}
          >
            {components.map((component) => (
              <div
                key={component.id}
                  data-component-id={component.id}
                className={`
                  ${styles.component}
                  ${selectedComponentId === component.id ? styles.selected : ''}
                `}
                style={{
                  left: `${component.x}px`,
                  top: `${component.y}px`,
                  width: `${component.width}px`,
                  height: `${component.height}px`,
                  zIndex: component.zIndex,
                  opacity: component.opacity,
                  transform: `rotate(${component.rotation}deg)`,
                  cursor: drag.isDragging ? 'grabbing' : 'grab',
                }}
                onClick={() => {
                  selection.selectComponent(component.id)
                  onComponentSelect(component.id)
                }}
                onMouseDown={(e) => {
                  e.stopPropagation()
                  selection.selectComponent(component.id)
                  onComponentSelect(component.id)
                  drag.startDrag(component.id, e.clientX / canvas.scale, e.clientY / canvas.scale)
                }}
              >
                {component.type === 'text' && (
                  <div
                    style={{
                      color: (component.data as any).color,
                      fontSize: `${(component.data as any).fontSize}px`,
                      fontFamily: (component.data as any).fontFamily,
                      width: '100%',
                      height: '100%',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      whiteSpace: 'pre-wrap',
                      wordBreak: 'break-word',
                    }}
                  >
                    {(component.data as any).content}
                  </div>
                )}
                {component.type === 'image' && (
                  <img
                    src={(component.data as any).mediaUrl}
                    alt={(component.data as any).alt}
                    style={{
                      width: '100%',
                      height: '100%',
                      objectFit: (component.data as any).objectFit,
                      pointerEvents: 'none',
                    }}
                  />
                )}
                {component.type === 'video' && (component.data as any).mediaUrl && (
                  <video
                    src={(component.data as any).mediaUrl}
                    muted={(component.data as any).muted !== false}
                    loop={Boolean((component.data as any).loop)}
                    autoPlay={Boolean((component.data as any).autoPlay)}
                    playsInline
                    style={{ width: '100%', height: '100%', objectFit: 'cover', pointerEvents: 'none' }}
                  />
                )}
                {component.type === 'graphics' && (
                  <div
                    style={{
                      width: '100%',
                      height: '100%',
                      backgroundColor: (component.data as any).fillColor,
                        borderStyle: 'solid',
                        borderRadius: (component.data as any).shapeType === 'circle' ? '50%' : undefined,
                      borderColor: (component.data as any).strokeColor,
                      borderWidth: `${(component.data as any).strokeWidth}px`,
                    }}
                  />
                )}

                {selectedComponentId === component.id && (
                  <div className={styles.selectionBox}>
                    {['nw', 'n', 'ne', 'w', 'e', 'sw', 's', 'se'].map((handle) => (
                      <div
                        key={handle}
                        className={`${styles.handle} ${styles[`handle-${handle}`]}`}
                        onMouseDown={(e) => {
                          e.stopPropagation()
                          resize.startResize(component.id, handle as any, e.clientX / canvas.scale, e.clientY / canvas.scale)
                        }}
                      />
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>
    )
  }
)

Canvas.displayName = 'Canvas'
