'use client'

import React, { useRef, useState } from 'react'
import { Button, Input, Select } from '@/components/Common'
import { MediaPickerDialog } from '@/components/media/MediaPickerDialog'
import { useMediaUpload } from '@/hooks/useMediaUpload'
import { MediaLibraryItem } from '@/api/mediaService'
import { formatDuration } from '@/lib/format'
import {
  CONTINUOUS_EFFECTS,
  EFFECT_KINDS,
  EFFECT_LABELS,
  LIMITS,
  PlaylistItem,
  SLIDE_TRANSITIONS,
  Slide,
  TRANSITION_LABELS,
  buildPlaySteps,
  cycleSeconds,
  effectChanged,
  readEffect,
} from '@/lib/componentSettings'

type Data = Record<string, any>
type Change = (property: string, value: unknown) => void

const arrayMove = <T,>(list: T[], from: number, to: number): T[] => {
  if (to < 0 || to >= list.length) return list
  const copy = [...list]
  const [moved] = copy.splice(from, 1)
  copy.splice(to, 0, moved)
  return copy
}

const seconds = (ms: number) => Math.round(ms / 100) / 10

// ----- effect (any component)

export function EffectControls({ data, onChange }: { data: Data; onChange: Change }) {
  const effect = readEffect(data)

  return (
    <div className="space-y-3 border-t border-gray-200 pt-4">
      <h4 className="text-sm font-semibold text-gray-700">Visual effect</h4>
      <Select
        id="effect-kind"
        label="Effect"
        placeholder={EFFECT_LABELS.none}
        options={EFFECT_KINDS.filter((k) => k !== 'none').map((k) => ({ value: k, label: EFFECT_LABELS[k] }))}
        value={effect.kind === 'none' ? '' : effect.kind}
        onChange={(e) => onChange('effect', effectChanged(effect, { kind: (e.target.value || 'none') as typeof effect.kind }))}
      />
      {effect.kind !== 'none' && (
        <div className="grid grid-cols-2 gap-3">
          <Input
            id="effect-duration"
            label="Takes (seconds)"
            type="number"
            step="0.1"
            min={LIMITS.effectMs.min / 1000}
            max={LIMITS.effectMs.max / 1000}
            value={seconds(effect.durationMs)}
            onChange={(e) => onChange('effect', effectChanged(effect, { durationMs: Number(e.target.value) * 1000 }))}
          />
          <Input
            id="effect-delay"
            label="Starts after (seconds)"
            type="number"
            step="0.1"
            min={0}
            max={LIMITS.effectDelayMs.max / 1000}
            value={seconds(effect.delayMs)}
            onChange={(e) => onChange('effect', effectChanged(effect, { delayMs: Number(e.target.value) * 1000 }))}
          />
        </div>
      )}
      {effect.kind !== 'none' && (
        <p className="text-xs text-gray-500">
          {CONTINUOUS_EFFECTS.includes(effect.kind) ? 'This effect keeps repeating while the banner is shown.' : 'This effect plays once when the banner starts.'}{' '}
          Use Preview to see it.
        </p>
      )}
    </div>
  )
}

// ----- adding a picture or video to a list: from the shop's files, or by uploading

function AddMedia({ kind, onAdded, disabled }: { kind: 'image' | 'video'; onAdded: (item: { id: string; url: string; name: string; durationSeconds: number | null }) => void; disabled?: boolean }) {
  const [picking, setPicking] = useState(false)
  const upload = useMediaUpload()
  const input = useRef<HTMLInputElement>(null)
  const uploading = upload.isUploading

  const fromLibrary = (item: MediaLibraryItem) => {
    setPicking(false)
    onAdded({ id: item.id, url: item.url, name: item.fileName, durationSeconds: item.durationSeconds })
  }

  const onFile = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    const done = await upload.upload(file, kind)
    if (done) onAdded({ id: done.mediaFileId, url: done.url, name: file.name, durationSeconds: done.durationSeconds ?? null })
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-2">
        <Button type="button" size="sm" variant="secondary" disabled={disabled || uploading} onClick={() => setPicking(true)}>
          Add from my files
        </Button>
        <Button type="button" size="sm" variant="secondary" isLoading={uploading} disabled={disabled} onClick={() => input.current?.click()}>
          Upload and add
        </Button>
        <input
          ref={input}
          type="file"
          className="sr-only"
          aria-label={kind === 'image' ? 'Upload a picture to add' : 'Upload a video to add'}
          accept={kind === 'image' ? 'image/png,image/jpeg,image/gif,image/webp' : 'video/mp4,video/webm'}
          onChange={onFile}
        />
      </div>
      {upload.error && (
        <p role="alert" className="text-sm text-red-600">
          {upload.error}
        </p>
      )}
      {picking && <MediaPickerDialog kind={kind} onSelect={fromLibrary} onClose={() => setPicking(false)} />}
    </div>
  )
}

function Row({ index, count, label, children, onMove, onRemove }: { index: number; count: number; label: string; children?: React.ReactNode; onMove: (to: number) => void; onRemove: () => void }) {
  return (
    <li className="flex items-center gap-2 rounded-lg border border-gray-200 p-2" data-testid={`rotation-row-${index}`}>
      {children}
      <span className="min-w-0 flex-1 truncate text-sm text-gray-800" title={label}>
        {index + 1}. {label}
      </span>
      <Button type="button" size="sm" variant="ghost" aria-label={`Move ${label} up`} disabled={index === 0} onClick={() => onMove(index - 1)}>
        ↑
      </Button>
      <Button type="button" size="sm" variant="ghost" aria-label={`Move ${label} down`} disabled={index === count - 1} onClick={() => onMove(index + 1)}>
        ↓
      </Button>
      <Button type="button" size="sm" variant="ghost" aria-label={`Remove ${label}`} onClick={onRemove}>
        ✕
      </Button>
    </li>
  )
}

// ----- rotating pictures (image components)

export function SlideControls({ data, onChange }: { data: Data; onChange: Change }) {
  const slides: Slide[] = Array.isArray(data.slides) ? data.slides : []
  const interval = typeof data.slideIntervalSeconds === 'number' ? data.slideIntervalSeconds : 5
  const transition = SLIDE_TRANSITIONS.includes(data.slideTransition) ? data.slideTransition : 'fade'
  const transitionMs = typeof data.slideTransitionMs === 'number' ? data.slideTransitionMs : 800

  const add = (item: { id: string; url: string; name: string }) => {
    // the picture already on the component becomes the first one of the list
    const start: Slide[] =
      slides.length === 0 && data.mediaFileId ? [{ mediaFileId: data.mediaFileId, mediaUrl: data.mediaUrl, alt: data.alt || undefined }] : slides
    onChange('slides', [...start, { mediaFileId: item.id, mediaUrl: item.url, alt: item.name }].slice(0, LIMITS.slides))
  }

  return (
    <div className="space-y-3 border-t border-gray-200 pt-4">
      <h4 className="text-sm font-semibold text-gray-700">Rotating pictures</h4>
      <p className="text-xs text-gray-500">
        Add more pictures and this one rotates through them, like a hero banner. Put it on the lowest layer, full size, with text and other
        pictures on top.
      </p>

      {slides.length > 0 && (
        <ul className="space-y-2">
          {slides.map((slide, index) => (
            <Row
              key={`${slide.mediaFileId}-${index}`}
              index={index}
              count={slides.length}
              label={slide.alt || 'Picture'}
              onMove={(to) => onChange('slides', arrayMove(slides, index, to))}
              onRemove={() => onChange('slides', slides.filter((_, i) => i !== index))}
            >
              {slide.mediaUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={slide.mediaUrl} alt="" className="h-10 w-14 rounded object-cover" />
              )}
            </Row>
          ))}
        </ul>
      )}
      {slides.length === 1 && <p className="text-xs text-amber-700">Add at least one more picture to start rotating.</p>}

      <AddMedia kind="image" onAdded={add} disabled={slides.length >= LIMITS.slides} />

      {slides.length >= 2 && (
        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <Input
              id="slide-interval"
              label="Each picture (seconds)"
              type="number"
              min={LIMITS.slideSeconds.min}
              max={LIMITS.slideSeconds.max}
              value={interval}
              onChange={(e) => onChange('slideIntervalSeconds', Math.min(LIMITS.slideSeconds.max, Math.max(LIMITS.slideSeconds.min, Number(e.target.value) || LIMITS.slideSeconds.min)))}
            />
            <Input
              id="slide-transition-seconds"
              label="Change takes (seconds)"
              type="number"
              step="0.1"
              min={LIMITS.transitionMs.min / 1000}
              max={LIMITS.transitionMs.max / 1000}
              value={seconds(transitionMs)}
              onChange={(e) =>
                onChange('slideTransitionMs', Math.min(LIMITS.transitionMs.max, Math.max(LIMITS.transitionMs.min, Math.round(Number(e.target.value) * 1000) || LIMITS.transitionMs.min)))
              }
            />
          </div>
          <Select
            id="slide-transition"
            label="How it changes"
            placeholder={TRANSITION_LABELS.fade}
            options={SLIDE_TRANSITIONS.filter((t) => t !== 'fade').map((t) => ({ value: t, label: TRANSITION_LABELS[t] }))}
            value={transition === 'fade' ? '' : transition}
            onChange={(e) => onChange('slideTransition', e.target.value || 'fade')}
          />
        </div>
      )}
    </div>
  )
}

// ----- rotating videos (video components)

export function PlaylistControls({ data, onChange }: { data: Data; onChange: Change }) {
  const items: PlaylistItem[] = Array.isArray(data.playlist) ? data.playlist : []
  const mode = data.rotationMode === 'fixedSeconds' ? 'fixedSeconds' : 'playFull'
  const perVideo = typeof data.secondsPerVideo === 'number' ? data.secondsPerVideo : 10
  const muted = data.muted !== false

  const add = (item: { id: string; url: string; name: string; durationSeconds: number | null }) => {
    const start: PlaylistItem[] =
      items.length === 0 && data.mediaFileId
        ? [{ mediaFileId: data.mediaFileId, mediaUrl: data.mediaUrl, durationSeconds: data.duration || null, fileName: 'First video' }]
        : items
    onChange('playlist', [...start, { mediaFileId: item.id, mediaUrl: item.url, durationSeconds: item.durationSeconds, fileName: item.name }].slice(0, LIMITS.playlist))
  }

  const steps = buildPlaySteps({ items, rotationMode: mode, secondsPerVideo: perVideo })
  const cycle = items.length >= 2 ? cycleSeconds(steps) : null

  return (
    <div className="space-y-3 border-t border-gray-200 pt-4">
      <h4 className="text-sm font-semibold text-gray-700">Videos in rotation</h4>
      <p className="text-xs text-gray-500">Add more videos and this one plays them one after another.</p>

      {items.length > 0 && (
        <ul className="space-y-2">
          {items.map((item, index) => (
            <Row
              key={`${item.mediaFileId}-${index}`}
              index={index}
              count={items.length}
              label={`${item.fileName || 'Video'}${item.durationSeconds ? ` (${formatDuration(item.durationSeconds)})` : ''}`}
              onMove={(to) => onChange('playlist', arrayMove(items, index, to))}
              onRemove={() => onChange('playlist', items.filter((_, i) => i !== index))}
            />
          ))}
        </ul>
      )}
      {items.length === 1 && <p className="text-xs text-amber-700">Add at least one more video to start rotating.</p>}

      <AddMedia kind="video" onAdded={add} disabled={items.length >= LIMITS.playlist} />

      {items.length >= 2 && (
        <fieldset className="space-y-2">
          <legend className="text-sm font-medium text-gray-700">How long each video plays</legend>
          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input type="radio" name="rotation-mode" checked={mode === 'playFull'} onChange={() => onChange('rotationMode', 'playFull')} />
            The whole video, then the next
          </label>
          <label className="flex items-center gap-2 text-sm text-gray-700">
            <input type="radio" name="rotation-mode" checked={mode === 'fixedSeconds'} onChange={() => onChange('rotationMode', 'fixedSeconds')} />
            A number of seconds each
          </label>
          {mode === 'fixedSeconds' && (
            <>
              <Input
                id="seconds-per-video"
                label="Seconds for each video"
                type="number"
                min={LIMITS.secondsPerVideo.min}
                max={LIMITS.secondsPerVideo.max}
                value={perVideo}
                onChange={(e) =>
                  onChange('secondsPerVideo', Math.min(LIMITS.secondsPerVideo.max, Math.max(LIMITS.secondsPerVideo.min, Math.round(Number(e.target.value)) || LIMITS.secondsPerVideo.min)))
                }
              />
              <p className="text-xs text-gray-500">A video shorter than this moves on when it ends.</p>
            </>
          )}
          {!muted && (
            <label className="block text-sm text-gray-700">
              Volume
              <input
                id="playlist-volume"
                type="range"
                min={0}
                max={1}
                step={0.05}
                value={typeof data.volume === 'number' ? data.volume : 1}
                onChange={(e) => onChange('volume', Number(e.target.value))}
                className="mt-1 block w-full"
              />
            </label>
          )}
          <p className="text-xs text-gray-500" data-testid="playlist-cycle">
            {cycle !== null ? `One round takes ${formatDuration(cycle)}.` : 'One round takes as long as the videos run (a length is not known for every video).'}
            {muted ? ' Sound is off: untick Muted above to hear them.' : ''}
          </p>
        </fieldset>
      )}
    </div>
  )
}
