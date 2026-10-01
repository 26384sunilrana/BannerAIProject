'use client'

import React from 'react'
import { Button } from '@/components/Common'

export interface HeaderProps {
  bannerId: string
  bannerName?: string
  /** Where the back link goes; omitted when there is nowhere to go back to. */
  backHref?: string
  onSave: () => Promise<void>
  onUndo: () => void
  onRedo: () => void
  onTogglePreview: () => void
  isSaving?: boolean
  isDirty?: boolean
  canUndo?: boolean
  canRedo?: boolean
  isPreviewMode?: boolean
}

export function Header({
  bannerId,
  bannerName,
  backHref,
  onSave,
  onUndo,
  onRedo,
  onTogglePreview,
  isSaving = false,
  isDirty = false,
  canUndo = false,
  canRedo = false,
  isPreviewMode = false,
}: HeaderProps) {
  const [isSavingLocal, setIsSavingLocal] = React.useState(false)

  const handleSave = async () => {
    setIsSavingLocal(true)
    try {
      await onSave()
    } finally {
      setIsSavingLocal(false)
    }
  }

  return (
    <header className="flex items-center justify-between gap-4 px-6 py-4 bg-white border-b border-gray-200 shadow-sm">
      <div className="flex items-center gap-4 flex-1">
        {backHref && (
          <a href={backHref} className="text-sm text-blue-600 hover:text-blue-800">
            ← Banners
          </a>
        )}
        <h1 className="text-2xl font-bold text-gray-900">{bannerName || 'Banner Editor'}</h1>
        <span className="text-sm text-gray-500 px-2 py-1 bg-gray-100 rounded" title="Banner id">
          {bannerId}
        </span>
      </div>

      <div className="flex items-center gap-2">
        {/* Save Status */}
        {isDirty && (
          <span className="text-xs text-yellow-600 px-2 py-1 bg-yellow-50 rounded">
            Unsaved changes
          </span>
        )}

        {isSavingLocal && (
          <span className="text-xs text-blue-600 px-2 py-1 bg-blue-50 rounded">
            Saving...
          </span>
        )}

        {/* Undo/Redo */}
        <Button
          variant="ghost"
          size="sm"
          onClick={onUndo}
          disabled={!canUndo}
          title="Undo (Ctrl+Z)"
          aria-label="Undo"
        >
          ↶
        </Button>

        <Button
          variant="ghost"
          size="sm"
          onClick={onRedo}
          disabled={!canRedo}
          title="Redo (Ctrl+Shift+Z)"
          aria-label="Redo"
        >
          ↷
        </Button>

        <div className="w-px h-6 bg-gray-200" />

        {/* Save Button */}
        <Button
          variant="primary"
          size="md"
          onClick={handleSave}
          isLoading={isSavingLocal || isSaving}
          disabled={!isDirty || isSavingLocal || isSaving}
        >
          {isSavingLocal || isSaving ? 'Saving...' : 'Save'}
        </Button>

        {/* Preview Toggle */}
        <Button
          variant={isPreviewMode ? 'primary' : 'secondary'}
          size="md"
          onClick={onTogglePreview}
        >
          {isPreviewMode ? 'Exit Preview' : 'Preview'}
        </Button>
      </div>
    </header>
  )
}
