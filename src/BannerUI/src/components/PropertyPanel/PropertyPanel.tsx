'use client'

import React, { useState } from 'react'
import { BannerComponent } from '@/types/banner'
import { Input, Select, Button } from '@/components/Common'
import { validation } from '@/utils/validation'

export interface PropertyPanelProps {
  selectedComponent: BannerComponent | null
  onPropertyChange: (property: string, value: any) => void
  onDeleteComponent: () => void
}

export function PropertyPanel({
  selectedComponent,
  onPropertyChange,
  onDeleteComponent,
}: PropertyPanelProps) {
  const [errors, setErrors] = useState<Record<string, string>>({})

  const handlePropertyChange = (property: string, value: any) => {
    const newErrors = { ...errors }

    // Validate based on property
    if (property === 'x' || property === 'y') {
      const result = validation.isValidComponentPosition(
        property === 'x' ? Number(value) : selectedComponent?.x || 0,
        property === 'y' ? Number(value) : selectedComponent?.y || 0
      )
      if (!result.valid) {
        newErrors[property] = result.error ?? 'Invalid value'
        setErrors(newErrors)
        return
      }
      delete newErrors[property]
    }

    if (property === 'width' || property === 'height') {
      const result = validation.isValidComponentSize(
        property === 'width' ? Number(value) : selectedComponent?.width || 0,
        property === 'height' ? Number(value) : selectedComponent?.height || 0
      )
      if (!result.valid) {
        newErrors[property] = result.error ?? 'Invalid value'
        setErrors(newErrors)
        return
      }
      delete newErrors[property]
    }

    if (property === 'zIndex') {
      const result = validation.isValidZIndex(Number(value))
      if (!result.valid) {
        newErrors[property] = result.error ?? 'Invalid value'
        setErrors(newErrors)
        return
      }
      delete newErrors[property]
    }

    setErrors(newErrors)
    onPropertyChange(property, value)
  }

  if (!selectedComponent) {
    return (
      <div className="w-80 bg-white border-l border-gray-200 p-6 flex items-center justify-center">
        <p className="text-gray-500 text-center">Select a component to edit</p>
      </div>
    )
  }

  return (
    <div className="w-80 bg-white border-l border-gray-200 overflow-y-auto">
      <div className="p-6 space-y-6">
        <div>
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Properties</h3>
          <div className="text-xs text-gray-500 mb-4">
            Type: <span className="font-medium capitalize">{selectedComponent.type}</span>
          </div>
        </div>

        {/* Position & Size Section */}
        <div className="space-y-3">
          <h4 className="text-sm font-semibold text-gray-700">Position & Size</h4>

          <Input
            label="X"
            type="number"
            value={selectedComponent.x}
            onChange={(e) => handlePropertyChange('x', Number(e.target.value))}
            error={errors.x}
          />

          <Input
            label="Y"
            type="number"
            value={selectedComponent.y}
            onChange={(e) => handlePropertyChange('y', Number(e.target.value))}
            error={errors.y}
          />

          <Input
            label="Width"
            type="number"
            value={selectedComponent.width}
            onChange={(e) => handlePropertyChange('width', Number(e.target.value))}
            error={errors.width}
          />

          <Input
            label="Height"
            type="number"
            value={selectedComponent.height}
            onChange={(e) => handlePropertyChange('height', Number(e.target.value))}
            error={errors.height}
          />
        </div>

        {/* Layer & Appearance Section */}
        <div className="space-y-3">
          <h4 className="text-sm font-semibold text-gray-700">Appearance</h4>

          <Input
            label="Z-Index"
            type="number"
            min="0"
            max="100"
            value={selectedComponent.zIndex}
            onChange={(e) => handlePropertyChange('zIndex', Number(e.target.value))}
            error={errors.zIndex}
          />

          <Input
            label="Rotation (°)"
            type="number"
            min="0"
            max="360"
            step="15"
            value={selectedComponent.rotation}
            onChange={(e) => handlePropertyChange('rotation', Number(e.target.value))}
          />

          <Input
            label="Opacity"
            type="number"
            min="0"
            max="1"
            step="0.1"
            value={selectedComponent.opacity}
            onChange={(e) => handlePropertyChange('opacity', Number(e.target.value))}
          />
        </div>

        {/* Type-Specific Properties */}
        {selectedComponent.type === 'text' && (
          <div className="space-y-3">
            <h4 className="text-sm font-semibold text-gray-700">Text</h4>

            <Input
              label="Content"
              value={(selectedComponent.data as any).content}
              onChange={(e) => handlePropertyChange('content', e.target.value)}
              maxLength={5000}
            />

            <Input
              label="Font Size"
              type="number"
              min="8"
              max="200"
              value={(selectedComponent.data as any).fontSize}
              onChange={(e) => handlePropertyChange('fontSize', Number(e.target.value))}
            />

            <Select
              label="Font Family"
              value={(selectedComponent.data as any).fontFamily}
              onChange={(e) => handlePropertyChange('fontFamily', e.target.value)}
              options={[
                { value: 'Arial', label: 'Arial' },
                { value: 'Helvetica', label: 'Helvetica' },
                { value: 'Georgia', label: 'Georgia' },
                { value: 'Times New Roman', label: 'Times New Roman' },
                { value: 'Courier New', label: 'Courier New' },
              ]}
            />

            <Input
              label="Color"
              type="color"
              value={(selectedComponent.data as any).color}
              onChange={(e) => handlePropertyChange('color', e.target.value)}
            />

            <Select
              label="Text Align"
              value={(selectedComponent.data as any).textAlign}
              onChange={(e) => handlePropertyChange('textAlign', e.target.value)}
              options={[
                { value: 'left', label: 'Left' },
                { value: 'center', label: 'Center' },
                { value: 'right', label: 'Right' },
              ]}
            />
          </div>
        )}

        {selectedComponent.type === 'graphics' && (
          <div className="space-y-3">
            <h4 className="text-sm font-semibold text-gray-700">Shape</h4>

            <Input
              label="Fill Color"
              type="color"
              value={(selectedComponent.data as any).fillColor}
              onChange={(e) => handlePropertyChange('fillColor', e.target.value)}
            />

            <Input
              label="Stroke Color"
              type="color"
              value={(selectedComponent.data as any).strokeColor}
              onChange={(e) => handlePropertyChange('strokeColor', e.target.value)}
            />

            <Input
              label="Stroke Width"
              type="number"
              min="0"
              max="20"
              value={(selectedComponent.data as any).strokeWidth}
              onChange={(e) => handlePropertyChange('strokeWidth', Number(e.target.value))}
            />
          </div>
        )}

        {/* Visibility & Actions */}
        <div className="space-y-3 pt-4 border-t border-gray-200">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={selectedComponent.isVisible}
              onChange={(e) => handlePropertyChange('isVisible', e.target.checked)}
              className="w-4 h-4"
            />
            <span className="text-sm text-gray-700">Visible</span>
          </label>

          <Button
            variant="danger"
            size="sm"
            onClick={onDeleteComponent}
            className="w-full"
          >
            Delete Component
          </Button>
        </div>
      </div>
    </div>
  )
}
