'use client'

import { useEffect, useState } from 'react'
import { useAuth } from '@/context/AuthContext'
import { apiClient } from '@/api/client'
import { BROWSER_ZONE } from '@/lib/timeZones'

export interface ShopZone {
  /** The IANA name in use; the browser's own zone until the shop's is known. */
  timeZoneId: string
  /** shop, city, country or default. */
  source: string
  /** The shop's own setting, null when it follows its city or country. */
  ownTimeZoneId: string | null
  loaded: boolean
}

const cache = new Map<string, Omit<ShopZone, 'loaded'>>()

/** Forget what was learned about a shop's zone (after it was changed). */
export function forgetShopTimeZone(shopId?: string) {
  if (shopId) cache.delete(shopId)
  else cache.clear()
}

/** The time zone of the signed-in person's shop: schedules and the calendar are in this zone, not in the browser's. */
export function useShopTimeZone(): ShopZone {
  const { user } = useAuth()
  const shopId = user?.shopId ?? null
  const [zone, setZone] = useState<ShopZone>(() => {
    const known = shopId ? cache.get(shopId) : undefined
    return known ? { ...known, loaded: true } : { timeZoneId: BROWSER_ZONE, source: 'browser', ownTimeZoneId: null, loaded: !shopId }
  })

  useEffect(() => {
    if (!shopId) {
      setZone({ timeZoneId: BROWSER_ZONE, source: 'browser', ownTimeZoneId: null, loaded: true })
      return
    }
    const known = cache.get(shopId)
    if (known) {
      setZone({ ...known, loaded: true })
      return
    }

    let alive = true
    apiClient
      .get<{ timeZoneId: string; source: string; ownTimeZoneId: string | null }>(`/shops/${shopId}/time-zone`)
      .then((loaded) => {
        const value = { timeZoneId: loaded.timeZoneId, source: loaded.source, ownTimeZoneId: loaded.ownTimeZoneId ?? null }
        cache.set(shopId, value)
        if (alive) setZone({ ...value, loaded: true })
      })
      .catch(() => {
        // the browser's zone is the best guess when the shop's cannot be read
        if (alive) setZone({ timeZoneId: BROWSER_ZONE, source: 'browser', ownTimeZoneId: null, loaded: true })
      })
    return () => {
      alive = false
    }
  }, [shopId])

  return zone
}
