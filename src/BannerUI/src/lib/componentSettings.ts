import type React from 'react'

/**
 * The optional settings a component keeps next to its content: a visual effect, a list of rotating pictures and a list of
 * rotating videos. They live in the component's stored properties, so they are saved, versioned, previewed and approved with
 * the banner. The limits here are the API's limits: the editor keeps values inside them instead of having them refused.
 */

export const EFFECT_KINDS = ['none', 'fadeIn', 'slideLeft', 'slideRight', 'slideUp', 'zoomIn', 'pulse', 'float'] as const
export type EffectKind = (typeof EFFECT_KINDS)[number]

export const EFFECT_LABELS: Record<EffectKind, string> = {
  none: 'No effect',
  fadeIn: 'Fade in',
  slideLeft: 'Slide in from the right',
  slideRight: 'Slide in from the left',
  slideUp: 'Slide up',
  zoomIn: 'Zoom in',
  pulse: 'Pulse (keeps going)',
  float: 'Float up and down (keeps going)',
}

/** Effects that keep repeating while the banner is shown; the others play once when the banner (or slide) appears. */
export const CONTINUOUS_EFFECTS: EffectKind[] = ['pulse', 'float']

export const SLIDE_TRANSITIONS = ['fade', 'slideLeft', 'slideRight', 'zoom'] as const
export type SlideTransition = (typeof SLIDE_TRANSITIONS)[number]

export const TRANSITION_LABELS: Record<SlideTransition, string> = {
  fade: 'Fade',
  slideLeft: 'Slide left',
  slideRight: 'Slide right',
  zoom: 'Zoom',
}

export const LIMITS = {
  effectMs: { min: 100, max: 10_000 },
  effectDelayMs: { min: 0, max: 30_000 },
  slides: 20,
  slideSeconds: { min: 1, max: 60 },
  transitionMs: { min: 200, max: 2_000 },
  playlist: 20,
  secondsPerVideo: { min: 1, max: 3_600 },
} as const

export interface ComponentEffect {
  kind: EffectKind
  durationMs: number
  delayMs: number
}

export interface Slide {
  mediaFileId: string
  /** A link the browser can load now; made fresh whenever the banner opens, never stored. */
  mediaUrl?: string
  alt?: string
}

export interface SlideSettings {
  slides: Slide[]
  intervalMs: number
  transition: SlideTransition
  transitionMs: number
}

export type RotationMode = 'playFull' | 'fixedSeconds'

export interface PlaylistItem {
  mediaFileId: string
  mediaUrl?: string
  /** Length in seconds when known (read from the file when it was uploaded). */
  durationSeconds?: number | null
  fileName?: string
}

export interface PlaylistSettings {
  items: PlaylistItem[]
  rotationMode: RotationMode
  secondsPerVideo: number
  muted: boolean
  volume: number
  loop: boolean
}

export interface PlayStep {
  index: number
  mediaUrl: string
  /** Seconds to show this video; null means until it ends. */
  playSeconds: number | null
}

type Data = Record<string, unknown>

const clamp = (value: unknown, min: number, max: number, fallback: number) => {
  const n = typeof value === 'number' && Number.isFinite(value) ? value : fallback
  return Math.min(max, Math.max(min, n))
}

export const DEFAULT_EFFECT: ComponentEffect = { kind: 'none', durationMs: 800, delayMs: 0 }

/** The component's effect, with anything missing or out of range replaced by a sensible value. */
export function readEffect(data: Data | undefined): ComponentEffect {
  const raw = data?.effect as Partial<ComponentEffect> | undefined
  if (!raw || typeof raw !== 'object') return DEFAULT_EFFECT
  return {
    kind: EFFECT_KINDS.includes(raw.kind as EffectKind) ? (raw.kind as EffectKind) : 'none',
    durationMs: clamp(raw.durationMs, LIMITS.effectMs.min, LIMITS.effectMs.max, DEFAULT_EFFECT.durationMs),
    delayMs: clamp(raw.delayMs, LIMITS.effectDelayMs.min, LIMITS.effectDelayMs.max, 0),
  }
}

export function effectChanged(effect: ComponentEffect, changes: Partial<ComponentEffect>): ComponentEffect {
  return {
    kind: changes.kind ?? effect.kind,
    durationMs: clamp(changes.durationMs ?? effect.durationMs, LIMITS.effectMs.min, LIMITS.effectMs.max, DEFAULT_EFFECT.durationMs),
    delayMs: clamp(changes.delayMs ?? effect.delayMs, LIMITS.effectDelayMs.min, LIMITS.effectDelayMs.max, 0),
  }
}

/** The rotating pictures of an image component, or null when it shows just one picture. */
export function readSlides(data: Data | undefined): SlideSettings | null {
  const raw = data?.slides
  if (!Array.isArray(raw)) return null

  const slides: Slide[] = raw
    .filter((s): s is Slide => !!s && typeof s === 'object' && typeof (s as Slide).mediaFileId === 'string' && (s as Slide).mediaFileId !== '')
    .slice(0, LIMITS.slides)
  if (slides.length < 2) return null

  return {
    slides,
    intervalMs: clamp(data?.slideIntervalSeconds, LIMITS.slideSeconds.min, LIMITS.slideSeconds.max, 5) * 1000,
    transition: SLIDE_TRANSITIONS.includes(data?.slideTransition as SlideTransition) ? (data?.slideTransition as SlideTransition) : 'fade',
    transitionMs: clamp(data?.slideTransitionMs, LIMITS.transitionMs.min, LIMITS.transitionMs.max, 800),
  }
}

/** The rotating videos of a video component, or null when it plays just one video. */
export function readPlaylist(data: Data | undefined): PlaylistSettings | null {
  const raw = data?.playlist
  if (!Array.isArray(raw)) return null

  const items: PlaylistItem[] = raw
    .filter((i): i is PlaylistItem => !!i && typeof i === 'object' && typeof (i as PlaylistItem).mediaFileId === 'string' && (i as PlaylistItem).mediaFileId !== '')
    .slice(0, LIMITS.playlist)
  if (items.length < 2) return null

  return {
    items,
    rotationMode: data?.rotationMode === 'fixedSeconds' ? 'fixedSeconds' : 'playFull',
    secondsPerVideo: Math.round(clamp(data?.secondsPerVideo, LIMITS.secondsPerVideo.min, LIMITS.secondsPerVideo.max, 10)),
    muted: data?.muted !== false,
    volume: clamp(data?.volume, 0, 1, 1),
    loop: data?.loop !== false,
  }
}

/**
 * What the player does with a list of videos. "Play the whole video" waits for each to end. "Seconds each" moves on after that many
 * seconds, but a video shorter than that moves on when it ends, so the shorter value wins. A video whose length is not known is
 * given the configured seconds (or played to its end).
 */
export function buildPlaySteps(settings: Pick<PlaylistSettings, 'items' | 'rotationMode' | 'secondsPerVideo'>): PlayStep[] {
  return settings.items.map((item, index) => {
    const length = typeof item.durationSeconds === 'number' && item.durationSeconds > 0 ? item.durationSeconds : null
    const playSeconds =
      settings.rotationMode === 'playFull'
        ? length
        : length !== null
          ? Math.min(settings.secondsPerVideo, length)
          : settings.secondsPerVideo
    return { index, mediaUrl: item.mediaUrl ?? '', playSeconds }
  })
}

/** Length of one pass through the list; null when any video's length is not known. */
export function cycleSeconds(steps: PlayStep[]): number | null {
  return steps.some((s) => s.playSeconds === null) ? null : steps.reduce((sum, s) => sum + (s.playSeconds ?? 0), 0)
}

/** CSS for an effect. Entrance effects play once; continuous ones repeat. Returns nothing for "none". */
export function effectAnimation(effect: ComponentEffect): { animation: string } | undefined {
  if (effect.kind === 'none') return undefined
  const repeat = CONTINUOUS_EFFECTS.includes(effect.kind) ? 'infinite' : '1'
  const timing = CONTINUOUS_EFFECTS.includes(effect.kind) ? 'ease-in-out' : 'ease-out'
  return { animation: `banner-fx-${effect.kind} ${effect.durationMs}ms ${timing} ${effect.delayMs}ms ${repeat} both` }
}

/** Rotation state of one slide relative to the one showing: 0 is showing, the one before is "before", the others wait "after". */
export function slidePosition(index: number, active: number, count: number): 'active' | 'before' | 'after' {
  if (index === active) return 'active'
  return (index - active + count) % count === count - 1 ? 'before' : 'after'
}

/** Style of a slide for its position and the chosen transition. */
export function slideStyle(position: 'active' | 'before' | 'after', transition: SlideTransition, transitionMs: number): React.CSSProperties {
  const base: React.CSSProperties = {
    position: 'absolute',
    inset: 0,
    width: '100%',
    height: '100%',
    transition: `opacity ${transitionMs}ms ease, transform ${transitionMs}ms ease`,
    pointerEvents: 'none',
  }
  const active = position === 'active'

  switch (transition) {
    case 'slideLeft':
      return { ...base, opacity: 1, transform: active ? 'translateX(0)' : position === 'before' ? 'translateX(-100%)' : 'translateX(100%)' }
    case 'slideRight':
      return { ...base, opacity: 1, transform: active ? 'translateX(0)' : position === 'before' ? 'translateX(100%)' : 'translateX(-100%)' }
    case 'zoom':
      return { ...base, opacity: active ? 1 : 0, transform: active ? 'scale(1)' : 'scale(1.2)' }
    default:
      return { ...base, opacity: active ? 1 : 0 }
  }
}

