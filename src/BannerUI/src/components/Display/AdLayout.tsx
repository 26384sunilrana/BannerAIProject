'use client'

import React, { useEffect, useState } from 'react'
import type { ShopAd } from '@/api/adService'

/** One ad drawn to fill the box it is given. */
export function AdTile({ ad, testId }: { ad: ShopAd; testId?: string }) {
  return (
    <div
      data-testid={testId ?? `ad-${ad.id}`}
      className="flex h-full w-full flex-col items-center justify-center gap-[1.5vmin] overflow-hidden p-[2vmin] text-center"
      style={{ backgroundColor: ad.background, color: ad.textColor }}
    >
      {ad.mediaUrl && (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={ad.mediaUrl} alt="" style={{ maxHeight: '55%', maxWidth: '100%', objectFit: 'contain' }} />
      )}
      <p className="font-bold leading-tight" style={{ fontSize: 'clamp(0.9rem, 4vmin, 4rem)' }}>
        {ad.headline}
      </p>
      {ad.body && (
        <p className="leading-snug opacity-90" style={{ fontSize: 'clamp(0.7rem, 2.4vmin, 2.4rem)' }}>
          {ad.body}
        </p>
      )}
      <p className="opacity-70" style={{ fontSize: 'clamp(0.6rem, 1.6vmin, 1.4rem)' }}>
        {ad.advertiserName}
      </p>
    </div>
  )
}

/** True during the seconds a popup is up. It repeats from the clock, so it needs no state to stay in step. */
export function popupIsUp(ad: Pick<ShopAd, 'popupSeconds' | 'popupEveryMinutes'>, nowMs: number): boolean {
  const cycle = ad.popupEveryMinutes * 60_000
  return cycle > 0 && nowMs % cycle < ad.popupSeconds * 1000
}

/**
 * Lays the shop's live ads around whatever is on the screen (the banner or the default board). A side ad is a strip along
 * an edge; a mega ad takes the larger share and the banner moves to the rest; a popup shows now and then over everything;
 * a minor ad is a small tile in a corner. When an ad is no longer live it is simply not in the list and its space goes back.
 */
export function ScreenWithAds({ ads, children, now }: { ads: ShopAd[]; children: React.ReactNode; now?: number }) {
  const [clock, setClock] = useState(() => now ?? Date.now())
  const hasPopup = ads.some((ad) => ad.kind === 'Popup')

  useEffect(() => {
    if (now !== undefined || !hasPopup) return
    const timer = setInterval(() => setClock(Date.now()), 1000)
    return () => clearInterval(timer)
  }, [hasPopup, now])

  const edge = (placement: string) => ads.find((ad) => (ad.kind === 'Side' || ad.kind === 'Mega') && ad.placement === placement)
  const left = edge('Left')
  const right = edge('Right')
  const top = edge('Top')
  const bottom = edge('Bottom')
  const minors = ads.filter((ad) => ad.kind === 'Minor')
  const popups = ads.filter((ad) => ad.kind === 'Popup' && popupIsUp(ad, now ?? clock))

  if (!left && !right && !top && !bottom && minors.length === 0 && popups.length === 0) return <>{children}</>

  const share = (ad?: ShopAd) => (ad ? `${ad.spacePercent}%` : undefined)

  return (
    <div className="relative flex h-full w-full flex-col" data-testid="ad-layout">
      {top && <div style={{ height: share(top), flex: 'none' }}><AdTile ad={top} /></div>}
      <div className="flex min-h-0 flex-1">
        {left && <div style={{ width: share(left), flex: 'none' }}><AdTile ad={left} /></div>}
        <div className="relative min-w-0 flex-1 overflow-hidden">{children}</div>
        {right && <div style={{ width: share(right), flex: 'none' }}><AdTile ad={right} /></div>}
      </div>
      {bottom && <div style={{ height: share(bottom), flex: 'none' }}><AdTile ad={bottom} /></div>}

      {minors.map((ad) => (
        <div
          key={ad.id}
          className="absolute"
          style={{
            width: '16vmin',
            height: '16vmin',
            top: ad.placement.startsWith('Top') ? '1.5vmin' : undefined,
            bottom: ad.placement.startsWith('Bottom') ? '1.5vmin' : undefined,
            left: ad.placement.endsWith('Left') ? '1.5vmin' : undefined,
            right: ad.placement.endsWith('Right') ? '1.5vmin' : undefined,
          }}
        >
          <AdTile ad={ad} testId={`ad-${ad.id}`} />
        </div>
      ))}

      {popups.map((ad) => (
        <div key={ad.id} className="absolute inset-0 flex items-center justify-center bg-black/40" data-testid="ad-popup">
          <div className="shadow-2xl" style={{ width: '60%', height: '60%' }}>
            <AdTile ad={ad} />
          </div>
        </div>
      ))}
    </div>
  )
}
