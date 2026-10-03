'use client'

import React, { useCallback, useEffect, useRef, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { BannerView } from '@/components/Display/BannerView'
import { DefaultBoard } from '@/components/Display/DefaultBoard'
import { bannerService } from '@/api/bannerService'
import { bannerListService } from '@/api/workflowService'
import { useAuth } from '@/context/AuthContext'
import { defaultBoardService } from '@/api/defaultBoardService'
import { BoardLook } from '@/components/Display/DefaultBoard'
import { MAX_CACHED_LOGO_CHARS, loadDisplayCache, saveDisplayCache } from '@/lib/displayCache'
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
  const { user: authUser, ready: authReady, refresh } = useAuth()
  const [banner, setBanner] = useState<Banner | null>(null)
  const [reason, setReason] = useState<string | null>(null)
  const [shopName, setShopName] = useState('')
  const [look, setLook] = useState<BoardLook | null>(null)
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
    // wait until the session cookie has been looked at, or a reload would send the screen to the sign-in page
    if (!authReady) return
    const user = authUser
    const cache = loadDisplayCache()

    if (!user && !cache) {
      router.replace('/login?returnUrl=%2Fdisplay')
      return
    }

    if (cache) {
      setShopName(cache.shopName)
      if (cache.look) setLook({ message: cache.look.message, background: cache.look.background, textColor: cache.look.textColor, logoUrl: cache.look.logoData })
    }
    setSignedIn(!!user)
    setReady(true)

    if (!user) {
      loaded.current = null
      setBanner(null)
      setReason('SessionEnded')
      return
    }

    if (user.shopId) {
      // The shop's own board design (and name). Kept on this machine, with the logo as data, so it shows with no connection.
      defaultBoardService
        .get(user.shopId)
        .then(async (board) => {
          setShopName(board.shopName)
          const fresh: BoardLook = { message: board.message, background: board.background, textColor: board.textColor, logoUrl: board.logoUrl }
          setLook(fresh)

          const logoData = board.logoUrl ? await toDataAddress(board.logoUrl) : null
          if (logoData) setLook({ ...fresh, logoUrl: logoData })
          saveDisplayCache({
            shopId: user.shopId!,
            shopName: board.shopName,
            look: { message: board.message, background: board.background, textColor: board.textColor, logoData },
          })
        })
        .catch(() => undefined)
    }

    poll()
    const timer = setInterval(poll, POLL_MS)
    return () => clearInterval(timer)
  }, [poll, router, authReady, authUser])

  // A screen that started without a connection (or whose session ended) keeps showing the default board and
  // looks for the session again every minute, so it picks up by itself when the connection is back.
  useEffect(() => {
    if (!authReady || authUser) return
    const timer = setInterval(() => {
      refresh()
    }, 60_000)
    return () => clearInterval(timer)
  }, [authReady, authUser, refresh])

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
        <DefaultBoard shopName={shopName} look={look} note={reason ? NOTES[reason] : undefined} />
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

/** Fetches a picture and returns it as a data address, or null when it cannot be fetched or is too big to keep. */
async function toDataAddress(url: string): Promise<string | null> {
  try {
    const response = await fetch(url)
    if (!response.ok) return null
    const blob = await response.blob()
    const data = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader()
      reader.onload = () => resolve(String(reader.result))
      reader.onerror = () => reject(reader.error)
      reader.readAsDataURL(blob)
    })
    return data.length <= MAX_CACHED_LOGO_CHARS ? data : null
  } catch {
    return null
  }
}
