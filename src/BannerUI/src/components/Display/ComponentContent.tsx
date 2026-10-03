'use client'

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { BannerComponent } from '@/types/banner'
import {
  PlaylistSettings,
  SlideSettings,
  buildPlaySteps,
  effectAnimation,
  readEffect,
  readPlaylist,
  readSlides,
  slidePosition,
  slideStyle,
} from '@/lib/componentSettings'

/** How a component is drawn: in the editor it stands still so it can be dragged; on a screen or in a preview it plays. */
export type ContentMode = 'edit' | 'play'

type Data = Record<string, any>

/**
 * The inside of one banner component (its text, picture, video or shape) with its effect, rotating pictures and
 * rotating videos. The editor, the preview and the shop screen all draw components through this, so what you
 * design is what the screen shows.
 */
export function ComponentContent({ component, mode }: { component: BannerComponent; mode: ContentMode }) {
  const data = component.data as unknown as Data
  const effect = readEffect(data)
  const playing = mode === 'play'
  const animation = playing ? effectAnimation(effect) : undefined

  return (
    <div data-testid={`content-${component.id}`} style={{ position: 'relative', width: '100%', height: '100%', ...animation }}>
      <Inner component={component} data={data} mode={mode} />
      {!playing && <EditBadges data={data} />}
    </div>
  )
}

function Inner({ component, data, mode }: { component: BannerComponent; data: Data; mode: ContentMode }) {
  const playing = mode === 'play'

  switch (component.type) {
    case 'text':
      return (
        <div
          style={{
            width: '100%',
            height: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: data.textAlign === 'center' ? 'center' : data.textAlign === 'right' ? 'flex-end' : 'flex-start',
            color: data.color,
            fontSize: `${data.fontSize}px`,
            fontFamily: data.fontFamily,
            fontWeight: data.fontWeight,
            lineHeight: data.lineHeight,
            whiteSpace: 'pre-wrap',
            wordBreak: 'break-word',
          }}
        >
          {data.content}
        </div>
      )

    case 'image': {
      const slides = readSlides(data)
      const usable = slides ? { ...slides, slides: slides.slides.filter((s) => s.mediaUrl) } : null
      if (usable && usable.slides.length >= 2) {
        return <SlideShow settings={usable} objectFit={data.objectFit ?? 'cover'} animate={playing} />
      }
      const url = data.mediaUrl || slides?.slides.find((s) => s.mediaUrl)?.mediaUrl
      return url ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={url} alt={data.alt ?? ''} style={{ width: '100%', height: '100%', objectFit: data.objectFit ?? 'cover', pointerEvents: 'none' }} />
      ) : mode === 'edit' ? (
        <EmptyMedia label="No picture yet" />
      ) : null
    }

    case 'video': {
      const playlist = readPlaylist(data)
      const usable = playlist ? { ...playlist, items: playlist.items.filter((i) => i.mediaUrl) } : null
      if (playing && usable && usable.items.length >= 2) return <VideoPlaylistPlayer settings={usable} />

      const url = data.mediaUrl || playlist?.items.find((i) => i.mediaUrl)?.mediaUrl
      if (!url) return mode === 'edit' ? <EmptyMedia label="No video yet" /> : null
      return (
        <video
          src={url}
          muted={data.muted !== false}
          loop={playing ? data.loop !== false : Boolean(data.loop)}
          autoPlay={playing ? true : Boolean(data.autoPlay)}
          preload="metadata"
          playsInline
          style={{ width: '100%', height: '100%', objectFit: 'cover', pointerEvents: 'none' }}
        />
      )
    }

    default:
      return (
        <div
          style={{
            width: '100%',
            height: '100%',
            backgroundColor: data.fillColor,
            borderStyle: 'solid',
            borderColor: data.strokeColor,
            borderWidth: `${data.strokeWidth ?? 0}px`,
            borderRadius: data.shapeType === 'circle' ? '50%' : undefined,
          }}
        />
      )
  }
}

/** What an empty picture or video component shows in the editor, so it can still be found and selected. */
function EmptyMedia({ label }: { label: string }) {
  return (
    <div
      data-testid="empty-media"
      style={{ width: '100%', height: '100%', border: '2px dashed #9ca3af', color: '#6b7280', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 13, background: 'rgba(243,244,246,0.6)', boxSizing: 'border-box' }}
    >
      {label}
    </div>
  )
}

/** Small labels in the editor so a component with an effect or a rotating list can be told from a plain one. */
function EditBadges({ data }: { data: Data }) {
  const labels: string[] = []
  const slides = readSlides(data)
  const playlist = readPlaylist(data)
  if (slides) labels.push(`⟳ ${slides.slides.length} pictures`)
  if (playlist) labels.push(`⟳ ${playlist.items.length} videos`)
  if (readEffect(data).kind !== 'none') labels.push('✦ effect')
  if (labels.length === 0) return null

  return (
    <div
      data-testid="component-badges"
      style={{ position: 'absolute', left: 4, top: 4, display: 'flex', gap: 4, pointerEvents: 'none', zIndex: 1 }}
    >
      {labels.map((label) => (
        <span key={label} style={{ background: 'rgba(17,24,39,0.75)', color: '#fff', fontSize: 11, padding: '1px 6px', borderRadius: 9999 }}>
          {label}
        </span>
      ))}
    </div>
  )
}

// ----- rotating pictures

export function SlideShow({ settings, objectFit, animate }: { settings: SlideSettings; objectFit: string; animate: boolean }) {
  const [active, setActive] = useState(0)
  const count = settings.slides.length

  useEffect(() => {
    if (!animate || count < 2) return
    const timer = setInterval(() => setActive((a) => (a + 1) % count), settings.intervalMs)
    return () => clearInterval(timer)
  }, [animate, count, settings.intervalMs])

  // the list may get shorter while showing
  const current = active < count ? active : 0

  return (
    <div data-testid="slideshow" style={{ position: 'relative', width: '100%', height: '100%', overflow: 'hidden' }}>
      {settings.slides.map((slide, index) => {
        const position = slidePosition(index, current, count)
        return (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            key={`${slide.mediaFileId}-${index}`}
            data-slide-position={position}
            src={slide.mediaUrl}
            alt={slide.alt ?? ''}
            style={{ ...slideStyle(position, settings.transition, settings.transitionMs), objectFit: objectFit as React.CSSProperties['objectFit'] }}
          />
        )
      })}
    </div>
  )
}

// ----- rotating videos

export function VideoPlaylistPlayer({ settings }: { settings: PlaylistSettings }) {
  const steps = useMemo(
    () => buildPlaySteps(settings),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [settings.items, settings.rotationMode, settings.secondsPerVideo]
  )
  const [index, setIndex] = useState(0)
  const video = useRef<HTMLVideoElement>(null)
  const current = index < steps.length ? index : 0
  const step = steps[current]

  // Moves on from one video, once: the end of the video and the timer can both ask, and the second ask is ignored
  const advanceFrom = useCallback(
    (from: number) => {
      setIndex((now) => {
        if (now !== from) return now
        const next = from + 1
        if (next < steps.length) return next
        return settings.loop ? 0 : from
      })
    },
    [steps.length, settings.loop]
  )

  // "Seconds each": move on after that long (a video shorter than that moves on when it ends, which happens first)
  useEffect(() => {
    if (settings.rotationMode !== 'fixedSeconds' || !step || step.playSeconds === null) return
    const timer = setTimeout(() => advanceFrom(current), step.playSeconds * 1000)
    return () => clearTimeout(timer)
  }, [current, step, settings.rotationMode, advanceFrom])

  // Start each video as it appears. A browser may refuse sound without a click: play it muted rather than not at all.
  useEffect(() => {
    const element = video.current
    if (!element) return
    element.muted = settings.muted
    element.volume = settings.muted ? 0 : settings.volume
    element.play().catch(() => {
      element.muted = true
      element.play().catch(() => undefined)
    })
  }, [current, step?.mediaUrl, settings.muted, settings.volume])

  if (!step) return null

  return (
    <video
      key={current}
      ref={video}
      data-testid="playlist-video"
      data-step={current}
      src={step.mediaUrl}
      muted={settings.muted}
      playsInline
      autoPlay
      onEnded={() => advanceFrom(current)}
      // a link that no longer works must not freeze the screen on one video
      onError={() => setTimeout(() => advanceFrom(current), 1000)}
      style={{ width: '100%', height: '100%', objectFit: 'cover', pointerEvents: 'none' }}
    />
  )
}
