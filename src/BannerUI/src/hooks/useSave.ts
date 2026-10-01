'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import { Banner, BannerComponent } from '@/types/banner'
import { bannerService } from '@/api/bannerService'
import { getErrorMessage } from '@/api/client'

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

  /**
   * Saves the banner and every component. Throws when anything could not be saved, and the banner stays
   * marked as having unsaved changes, so a half-saved banner is never reported as saved.
   */
  const save = useCallback(async () => {
    if (!banner) return

    setState((prev) => ({ ...prev, isSaving: true, error: null }))

    const failures: string[] = []

    try {
      await bannerService.updateBanner(bannerId, {
        title: banner.title,
        description: banner.description,
        width: banner.width,
        height: banner.height,
      })
    } catch (error) {
      failures.push(`Banner details: ${getErrorMessage(error, 'not saved')}`)
    }

    for (const component of components) {
      try {
        await bannerService.updateComponent(bannerId, component.id, component)
      } catch (error) {
        failures.push(`${component.type} component: ${getErrorMessage(error, 'not saved')}`)
      }
    }

    if (failures.length > 0) {
      const message = failures.join('; ')
      setState((prev) => ({ ...prev, isSaving: false, error: message }))
      throw new Error(message)
    }

    setState((prev) => ({
      ...prev,
      isSaving: false,
      isDirty: false,
      lastSavedAt: Date.now(),
    }))
  }, [banner, bannerId, components])

  const markDirty = useCallback(() => {
    setState((prev) => ({ ...prev, isDirty: true }))
  }, [])

  // Auto-save effect
  useEffect(() => {
    if (!state.isDirty) return

    autoSaveTimeoutRef.current = setTimeout(() => {
      save().catch(() => {
        // the error is kept in state and shown by the editor
      })
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
