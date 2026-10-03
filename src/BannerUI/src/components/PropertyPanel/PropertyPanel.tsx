'use client'

import React, { useState } from 'react'
import { BannerComponent } from '@/types/banner'
import { Input, Select, Button } from '@/components/Common'
import { validation } from '@/utils/validation'
import { EffectControls, PlaylistControls, SlideControls } from './RotationControls'

export interface PropertyPanelProps {
  selectedComponent: BannerComponent | null
  onPropertyChange: (property: string, value: any) => void
  onDeleteComponent: () => void
  /** Uploads a chosen file and puts it in the selected component. */
  onUploadMedia?: (file: File) => void
  /** The banner's size, for "Fit to banner". */
  bannerSize?: { width: number; height: number }
  /** Opens the shop's own files to pick one that is already uploaded. */
  onChooseFromLibrary?: () => void
  /** 0-100 while a file is uploading, otherwise null. */
  uploadProgress?: number | null
  uploadError?: string | null
}

function UploadControl({
  accept,
  label,
  onUploadMedia,
  onChooseFromLibrary,
  progress,
  error,
}: {
  accept: string
  label: string
  onUploadMedia?: (file: File) => void
  onChooseFromLibrary?: () => void
  progress?: number | null
  error?: string | null
}) {
  if (!onUploadMedia) return null
  const uploading = progress !== null && progress !== undefined

  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-gray-700" htmlFor="media-upload">
        {label}
      </label>
      <input
        id="media-upload"
        type="file"
        accept={accept}
        disabled={uploading}
        onChange={(e) => {
          const file = e.target.files?.[0]
          if (file) onUploadMedia(file)
          e.target.value = ''
        }}
        className="block w-full text-sm text-gray-700 file:mr-3 file:rounded-lg file:border-0 file:bg-blue-50 file:px-3 file:py-2 file:text-blue-700"
      />
      {onChooseFromLibrary && (
        <Button type="button" size="sm" variant="secondary" disabled={uploading} onClick={onChooseFromLibrary}>
          Choose from my files
        </Button>
      )}
      {uploading && (
        <div role="progressbar" aria-valuenow={progress ?? 0} aria-valuemin={0} aria-valuemax={100} className="h-2 w-full rounded bg-gray-200">
          <div className="h-2 rounded bg-blue-600" style={{ width: `${progress}%` }} />
        </div>
      )}
      {error && (
        <p role="alert" className="text-sm text-red-600">
          {error}
        </p>
      )}
    </div>
  )
}

export function PropertyPanel({
  selectedComponent,
  onPropertyChange,
  onDeleteComponent,
  onUploadMedia,
  onChooseFromLibrary,
  bannerSize,
  uploadProgress,
  uploadError,
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

          {bannerSize && (
            <Button
              type="button"
              size="sm"
              variant="secondary"
              onClick={() => {
                // a background that covers the whole banner
                onPropertyChange('x', 0)
                onPropertyChange('y', 0)
                onPropertyChange('width', bannerSize.width)
                onPropertyChange('height', bannerSize.height)
              }}
            >
              Fit to banner
            </Button>
          )}
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

        {selectedComponent.type === 'image' && (
          <div className="space-y-3">
            <h4 className="text-sm font-semibold text-gray-700">Image</h4>

            <UploadControl
              accept="image/png,image/jpeg,image/gif,image/webp"
              label="Upload an image"
              onUploadMedia={onUploadMedia}
              onChooseFromLibrary={onChooseFromLibrary}
              progress={uploadProgress}
              error={uploadError}
            />

            <Input
              label="Image address (URL)"
              type="url"
              placeholder="https://..."
              value={(selectedComponent.data as any).mediaUrl ?? ''}
              onChange={(e) => handlePropertyChange('mediaUrl', e.target.value)}
            />

            <Input
              label="Description (alt text)"
              value={(selectedComponent.data as any).alt ?? ''}
              onChange={(e) => handlePropertyChange('alt', e.target.value)}
            />

            <Select
              label="Fit"
              value={(selectedComponent.data as any).objectFit ?? 'cover'}
              onChange={(e) => handlePropertyChange('objectFit', e.target.value)}
              options={[
                { value: 'cover', label: 'Cover' },
                { value: 'contain', label: 'Contain' },
                { value: 'fill', label: 'Stretch' },
              ]}
            />

            <SlideControls data={selectedComponent.data as any} onChange={onPropertyChange} />
          </div>
        )}

        {selectedComponent.type === 'video' && (
          <div className="space-y-3">
            <h4 className="text-sm font-semibold text-gray-700">Video</h4>

            <UploadControl
              accept="video/mp4,video/webm"
              label="Upload a video"
              onUploadMedia={onUploadMedia}
              onChooseFromLibrary={onChooseFromLibrary}
              progress={uploadProgress}
              error={uploadError}
            />

            <Input
              label="Video address (URL)"
              type="url"
              placeholder="https://..."
              value={(selectedComponent.data as any).mediaUrl ?? ''}
              onChange={(e) => handlePropertyChange('mediaUrl', e.target.value)}
            />

            {(['muted', 'loop', 'autoPlay'] as const).map((flag) => (
              <label key={flag} className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={Boolean((selectedComponent.data as any)[flag])}
                  onChange={(e) => handlePropertyChange(flag, e.target.checked)}
                  className="w-4 h-4"
                />
                <span className="text-sm text-gray-700">
                  {flag === 'autoPlay' ? 'Play automatically' : flag === 'loop' ? 'Repeat' : 'Muted'}
                </span>
              </label>
            ))}

            <PlaylistControls data={selectedComponent.data as any} onChange={onPropertyChange} />
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

        <EffectControls data={selectedComponent.data as any} onChange={onPropertyChange} />

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
