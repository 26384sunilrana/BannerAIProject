'use client'

import React from 'react'
import { Button } from '@/components/Common'
import { ComponentType } from '@/types/banner'

export interface ToolbarProps {
  onAddComponent: (type: ComponentType) => void
  maxComponents?: number
  currentComponentCount?: number
}

const componentTypes: Array<{
  type: ComponentType
  label: string
  icon: string
  description: string
}> = [
  {
    type: 'text',
    label: 'Text',
    icon: 'T',
    description: 'Add text element',
  },
  {
    type: 'image',
    label: 'Image',
    icon: '🖼',
    description: 'Add image element',
  },
  {
    type: 'video',
    label: 'Video',
    icon: '▶',
    description: 'Add video element',
  },
  {
    type: 'graphics',
    label: 'Shape',
    icon: '●',
    description: 'Add shape element',
  },
]

export function Toolbar({
  onAddComponent,
  maxComponents = 50,
  currentComponentCount = 0,
}: ToolbarProps) {
  const canAddMore = currentComponentCount < maxComponents

  return (
    <div className="flex items-center gap-2 p-4 bg-gray-50 border-r border-gray-200">
      <div className="w-full">
        <h3 className="text-xs font-semibold text-gray-700 mb-3 px-2">Components</h3>

        <div className="grid grid-cols-2 gap-2">
          {componentTypes.map((item) => (
            <Button
              key={item.type}
              variant="secondary"
              size="sm"
              onClick={() => onAddComponent(item.type)}
              disabled={!canAddMore}
              title={item.description}
              className="justify-start text-left"
            >
              <span className="text-base">{item.icon}</span>
              <span className="text-xs">{item.label}</span>
            </Button>
          ))}
        </div>

        {!canAddMore && (
          <div className="mt-3 p-2 bg-yellow-50 border border-yellow-200 rounded text-xs text-yellow-700">
            Max {maxComponents} components reached
          </div>
        )}

        <div className="mt-4 pt-4 border-t border-gray-200 text-xs text-gray-600 px-2">
          <p>Components: {currentComponentCount}/{maxComponents}</p>
        </div>
      </div>
    </div>
  )
}
