'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import { Banner, BannerComponent } from '@/types/banner'
import { bannerService } from '@/api/bannerService'

const AUTO_SAVE_INTERVAL = parseInt(process.env.NEXT_PUBLIC_AUTO_SAVE_INTERVAL || '30000')

export interface SaveState {
  isSaving: boolean
  isDirty: boolean
  lastSavedAt: number | null
  error: string | null
}

export function useSave(bannerId: string, components: BannerComponent[], banner: Banner | null) {
  const [state, setState] = useState<SaveState>({
    isSaving: false,
    isDirty: false,
    lastSavedAt: null,
    error: null,
  })

  const autoSaveTimeoutRef = useRef<NodeJS.Timeout>()

  const save = useCallback(async () => {
    if (!banner) return

    try {
      setState((prev) => ({ ...prev, isSaving: true, error: null }))

      const updates = {
        title: banner.title,
        description: banner.description,
      }

      await bannerService.updateBanner(bannerId, updates)

      // Save component changes individually
      for (const component of components) {
        try {
          await bannerService.updateComponent(bannerId, component.id, {
            x: component.x,
            y: component.y,
            width: component.width,
            height: component.height,
            zIndex: component.zIndex,
            rotation: component.rotation,
            opacity: component.opacity,
            isVisible: component.isVisible,
            data: component.data as unknown as Record<string, unknown>,
          })
        } catch (error) {
          console.error(`Failed to save component ${component.id}:`, error)
        }
      }

      setState((prev) => ({
        ...prev,
        isSaving: false,
        isDirty: false,
        lastSavedAt: Date.now(),
      }))
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to save'
      setState((prev) => ({
        ...prev,
        isSaving: false,
        error: errorMessage,
      }))
    }
  }, [banner, bannerId, components])

  const markDirty = useCallback(() => {
    setState((prev) => ({ ...prev, isDirty: true }))
  }, [])

  // Auto-save effect
  useEffect(() => {
    if (!state.isDirty) return

    autoSaveTimeoutRef.current = setTimeout(() => {
      if (state.isDirty) {
        save()
      }
    }, AUTO_SAVE_INTERVAL)

    return () => {
      if (autoSaveTimeoutRef.current) {
        clearTimeout(autoSaveTimeoutRef.current)
      }
    }
  }, [state.isDirty, save])

  return {
    ...state,
    save,
    markDirty,
  }
}
