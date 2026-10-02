'use client'

import React, { useCallback, useEffect, useRef, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { BannerView } from '@/components/Display/BannerView'
import { DefaultBoard } from '@/components/Display/DefaultBoard'
import { bannerService } from '@/api/bannerService'
import { bannerListService } from '@/api/workflowService'
import { apiClient } from '@/api/client'
import { getSessionUser } from '@/lib/session'
import { loadDisplayCache, saveDisplayCache } from '@/lib/displayCache'
import { Banner } from '@/types/banner'

const POLL_MS = 30_000
// Media links are made for 4 hours; get fresh ones well before they run out
const LINK_LIFETIME_MS = 3 * 60 * 60 * 1000

const NOTES: Record<string, string> = {
  SubscriptionEnded: 'This shop’s plan has ended. Renew it to show your banners again.',
  SessionEnded: 'Signed out. Sign in again to show your banners.',
}

/**
 * The screen in the shop. It shows the banner that is live right now and swaps it when the schedule says so.
 * When nothing is scheduled, the plan has ended, or the server cannot be reached, it shows the shop's default
 * board, which it keeps on this machine, so the screen is never blank.
 */
export default function DisplayPage() {
  const router = useRouter()
  const [banner, setBanner] = useState<Banner | null>(null)
  const [reason, setReason] = useState<string | null>(null)
  const [shopName, setShopName] = useState('')
  const [ready, setReady] = useState(false)
  const [signedIn, setSignedIn] = useState(false)
  const [controlsVisible, setControlsVisible] = useState(false)

  const loaded = useRef<{ id: string; updatedAt: string; at: number } | null>(null)
  const hideTimer = useRef<ReturnType<typeof setTimeout>>()

  const poll = useCallback(async () => {
    try {
      const active = await bannerListService.getActive()
      setReason(active.useDefaultBanner ? active.reason ?? 'NothingScheduled' : null)

      if (!active.banner) {
        loaded.current = null
        setBanner(null)
        return
      }

      const current = loaded.current
      const unchanged = current && current.id === active.banner.id && current.updatedAt === active.banner.updatedAt
      if (unchanged && Date.now() - current.at < LINK_LIFETIME_MS) return

      const full = await bannerService.getBanner(active.banner.id)
      loaded.current = { id: active.banner.id, updatedAt: active.banner.updatedAt, at: Date.now() }
      setBanner(full)
    } catch (error) {
      const status = (error as { response?: { status?: number } })?.response?.status
      // Signed out or switched off: fall back to the default board. Any other failure (offline): keep what is showing.
      if (status === 401 || status === 403) {
        loaded.current = null
        setBanner(null)
        setReason('SessionEnded')
      }
    }
  }, [])

  useEffect(() => {
    const user = getSessionUser()
    const cache = loadDisplayCache()

    if (!user && !cache) {
      router.replace('/login?returnUrl=%2Fdisplay')
      return
    }

    if (cache) setShopName(cache.shopName)
    setSignedIn(!!user)
    setReady(true)

    if (!user) {
      setReason('SessionEnded')
      return
    }

    if (user.shopId) {
      apiClient
        .get<{ name: string }>(`/shops/${user.shopId}`)
        .then((shop) => {
          if (shop?.name) {
            setShopName(shop.name)
            saveDisplayCache({ shopId: user.shopId!, shopName: shop.name })
          }
        })
        .catch(() => undefined)
    }

    poll()
    const timer = setInterval(poll, POLL_MS)
    return () => clearInterval(timer)
  }, [poll, router])

  const showControls = () => {
    setControlsVisible(true)
    clearTimeout(hideTimer.current)
    hideTimer.current = setTimeout(() => setControlsVisible(false), 3000)
  }

  const toggleFullscreen = () => {
    if (document.fullscreenElement) document.exitFullscreen?.()
    else document.documentElement.requestFullscreen?.()
  }

  if (!ready) return <div className="h-screen w-screen bg-black" />

  return (
    <div
      className="relative h-screen w-screen overflow-hidden bg-black"
      onMouseMove={showControls}
      style={{ cursor: controlsVisible ? 'default' : 'none' }}
    >
      {banner ? (
        <BannerView banner={banner} />
      ) : (
        <DefaultBoard shopName={shopName} note={reason ? NOTES[reason] : undefined} />
      )}

      <div
        className={`absolute right-4 top-4 flex gap-2 transition-opacity ${controlsVisible ? 'opacity-100' : 'pointer-events-none opacity-0'}`}
      >
        <button type="button" onClick={toggleFullscreen} className="rounded-lg bg-black/60 px-3 py-1.5 text-sm text-white hover:bg-black/80">
          Full screen
        </button>
        <Link href={signedIn ? '/dashboard' : '/login?returnUrl=%2Fdisplay'} className="rounded-lg bg-black/60 px-3 py-1.5 text-sm text-white hover:bg-black/80">
          {signedIn ? 'Back to app' : 'Sign in'}
        </Link>
      </div>
    </div>
  )
}
