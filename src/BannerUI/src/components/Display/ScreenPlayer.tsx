'use client'

import React, { useCallback, useEffect, useRef, useState } from 'react'
import { BannerView } from '@/components/Display/BannerView'
import { BoardLook, DefaultBoard } from '@/components/Display/DefaultBoard'
import { ScreenWithAds } from '@/components/Display/AdLayout'
import { bannerService } from '@/api/bannerService'
import { bannerListService } from '@/api/workflowService'
import { adService, ShopAd } from '@/api/adService'
import { defaultBoardService } from '@/api/defaultBoardService'
import { apiClient } from '@/api/client'
import { toDataAddress } from '@/lib/deviceScreen'
import { loadDisplayCache, saveDisplayCache } from '@/lib/displayCache'
import { Banner } from '@/types/banner'

const POLL_MS = 30_000
const HEARTBEAT_MS = 60_000
// Media links are made for 4 hours; get fresh ones well before they run out
const LINK_LIFETIME_MS = 3 * 60 * 60 * 1000

const NOTES: Record<string, string> = {
  SubscriptionEnded: 'This shop’s plan has ended. Renew it to show your banners again.',
}

interface Props {
  shopId: string
  shopName: string
  /** The screen was removed (or lost its pairing): the player starts pairing again. */
  onLost: () => void
}

/**
 * What a paired television shows: the banner that is live now, the shop's ads around it, and the default board between banners.
 * Every 30 seconds it asks again; every minute it tells the server what is on the screen (the proof of play). It keeps working when
 * the connection drops: what is showing stays, and the default board is kept on the device.
 */
export function ScreenPlayer({ shopId, shopName, onLost }: Props) {
  const [banner, setBanner] = useState<Banner | null>(null)
  const [reason, setReason] = useState<string | null>(null)
  const [look, setLook] = useState<BoardLook | null>(null)
  const [ads, setAds] = useState<ShopAd[]>([])
  const [name, setName] = useState(shopName)

  const loaded = useRef<{ id: string; updatedAt: string; at: number } | null>(null)
  // what is on the screen, for the heartbeat (refs: the heartbeat timer must see the latest without restarting)
  const showing = useRef<{ bannerId: string | null; adIds: string[]; defaultBoard: boolean }>({ bannerId: null, adIds: [], defaultBoard: true })

  const lost = useCallback(
    (error: unknown) => {
      const status = (error as { response?: { status?: number } })?.response?.status
      if (status === 401) onLost()
    },
    [onLost]
  )

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
      // offline or a slow server: what is showing stays. A screen that is no longer paired starts pairing again.
      lost(error)
    }
  }, [lost])

  useEffect(() => {
    const cache = loadDisplayCache()
    if (cache && cache.shopId === shopId) {
      setName(cache.shopName)
      if (cache.look) setLook({ message: cache.look.message, background: cache.look.background, textColor: cache.look.textColor, logoUrl: cache.look.logoData })
    }

    // the shop's own board design, kept on this device with the logo as data so it shows without a connection
    defaultBoardService
      .get(shopId)
      .then(async (board) => {
        setName(board.shopName)
        const fresh: BoardLook = { message: board.message, background: board.background, textColor: board.textColor, logoUrl: board.logoUrl }
        setLook(fresh)
        const logoData = board.logoUrl ? await toDataAddress(board.logoUrl) : null
        if (logoData) setLook({ ...fresh, logoUrl: logoData })
        saveDisplayCache({ shopId, shopName: board.shopName, look: { message: board.message, background: board.background, textColor: board.textColor, logoData } })
      })
      .catch(lost)

    const pollAds = () => adService.live(shopId).then(setAds).catch(lost)
    poll()
    pollAds()
    const timer = setInterval(() => {
      poll()
      pollAds()
    }, POLL_MS)
    return () => clearInterval(timer)
  }, [shopId, poll, lost])

  useEffect(() => {
    showing.current = { bannerId: banner?.id ?? null, adIds: ads.map((a) => a.id), defaultBoard: !banner }
  }, [banner, ads])

  useEffect(() => {
    const beat = () =>
      apiClient
        .post('/screens/heartbeat', { bannerId: showing.current.bannerId, ads: showing.current.adIds.map((id) => ({ id })), defaultBoard: showing.current.defaultBoard, appVersion: 'web-player-1' })
        .catch(lost)
    beat()
    const timer = setInterval(beat, HEARTBEAT_MS)
    return () => clearInterval(timer)
  }, [lost])

  return (
    <div className="relative h-screen w-screen overflow-hidden bg-black" style={{ cursor: 'none' }} data-testid="screen-player">
      <ScreenWithAds ads={ads}>
        {banner ? <BannerView banner={banner} /> : <DefaultBoard shopName={name} look={look} note={reason ? NOTES[reason] : undefined} />}
      </ScreenWithAds>
    </div>
  )
}
