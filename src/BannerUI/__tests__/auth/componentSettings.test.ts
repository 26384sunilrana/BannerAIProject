import {
  DEFAULT_EFFECT,
  buildPlaySteps,
  cycleSeconds,
  effectAnimation,
  effectChanged,
  readEffect,
  readPlaylist,
  readSlides,
  slidePosition,
  slideStyle,
} from '@/lib/componentSettings'
import { toApiComponent } from '@/api/bannerMapper'

describe('readEffect', () => {
  it('has no effect when nothing is stored', () => {
    expect(readEffect(undefined)).toEqual(DEFAULT_EFFECT)
    expect(readEffect({})).toEqual(DEFAULT_EFFECT)
    expect(readEffect({ effect: 'fadeIn' })).toEqual(DEFAULT_EFFECT)
  })

  it('reads a stored effect and keeps it inside the allowed ranges', () => {
    expect(readEffect({ effect: { kind: 'zoomIn', durationMs: 1500, delayMs: 200 } })).toEqual({ kind: 'zoomIn', durationMs: 1500, delayMs: 200 })
    expect(readEffect({ effect: { kind: 'zoomIn', durationMs: 5, delayMs: -4 } })).toEqual({ kind: 'zoomIn', durationMs: 100, delayMs: 0 })
    expect(readEffect({ effect: { kind: 'zoomIn', durationMs: 99999, delayMs: 999999 } })).toEqual({ kind: 'zoomIn', durationMs: 10000, delayMs: 30000 })
  })

  it('falls back to none for a kind it does not know', () => {
    expect(readEffect({ effect: { kind: 'explode', durationMs: 800, delayMs: 0 } }).kind).toBe('none')
  })

  it('effectChanged changes only what is given and clamps', () => {
    const effect = { kind: 'fadeIn' as const, durationMs: 800, delayMs: 100 }

    expect(effectChanged(effect, { kind: 'pulse' })).toEqual({ kind: 'pulse', durationMs: 800, delayMs: 100 })
    expect(effectChanged(effect, { durationMs: 0 }).durationMs).toBe(100)
    expect(effectChanged(effect, { delayMs: 1e9 }).delayMs).toBe(30000)
  })
})

describe('effectAnimation', () => {
  it('has no animation for none', () => {
    expect(effectAnimation({ kind: 'none', durationMs: 800, delayMs: 0 })).toBeUndefined()
  })

  it('plays entrance effects once', () => {
    expect(effectAnimation({ kind: 'slideUp', durationMs: 600, delayMs: 250 })).toEqual({ animation: 'banner-fx-slideUp 600ms ease-out 250ms 1 both' })
  })

  it('repeats pulse and float', () => {
    expect(effectAnimation({ kind: 'pulse', durationMs: 1000, delayMs: 0 })?.animation).toContain('infinite')
    expect(effectAnimation({ kind: 'float', durationMs: 2000, delayMs: 0 })?.animation).toContain('infinite')
  })
})

describe('readSlides', () => {
  const slide = (n: number) => ({ mediaFileId: `f${n}`, mediaUrl: `http://x/${n}` })

  it('is a single picture until there are two or more', () => {
    expect(readSlides({})).toBeNull()
    expect(readSlides({ slides: [] })).toBeNull()
    expect(readSlides({ slides: [slide(1)] })).toBeNull()
  })

  it('reads the rotation with its defaults', () => {
    const settings = readSlides({ slides: [slide(1), slide(2)] })!

    expect(settings.slides).toHaveLength(2)
    expect(settings.intervalMs).toBe(5000)
    expect(settings.transition).toBe('fade')
    expect(settings.transitionMs).toBe(800)
  })

  it('reads and bounds the stored settings', () => {
    const settings = readSlides({ slides: [slide(1), slide(2)], slideIntervalSeconds: 3, slideTransition: 'zoom', slideTransitionMs: 100 })!

    expect(settings.intervalMs).toBe(3000)
    expect(settings.transition).toBe('zoom')
    expect(settings.transitionMs).toBe(200)
    expect(readSlides({ slides: [slide(1), slide(2)], slideIntervalSeconds: 500 })!.intervalMs).toBe(60000)
    expect(readSlides({ slides: [slide(1), slide(2)], slideTransition: 'spin' })!.transition).toBe('fade')
  })

  it('ignores entries without a file and keeps at most twenty', () => {
    const many = Array.from({ length: 30 }, (_, i) => slide(i))

    expect(readSlides({ slides: [slide(1), {}, null, 'x', slide(2)] })!.slides).toHaveLength(2)
    expect(readSlides({ slides: many })!.slides).toHaveLength(20)
  })
})

describe('slidePosition and slideStyle', () => {
  it('shows one slide, calls the one before "before", and keeps the rest waiting "after"', () => {
    expect([0, 1, 2, 3].map((i) => slidePosition(i, 0, 4))).toEqual(['active', 'after', 'after', 'before'])
    expect([0, 1, 2, 3].map((i) => slidePosition(i, 2, 4))).toEqual(['after', 'before', 'active', 'after'])
  })

  it('works for two slides: the other one is both before and after, and counts as before', () => {
    expect([0, 1].map((i) => slidePosition(i, 0, 2))).toEqual(['active', 'before'])
  })

  it('fades by opacity', () => {
    expect(slideStyle('active', 'fade', 600).opacity).toBe(1)
    expect(slideStyle('after', 'fade', 600).opacity).toBe(0)
    expect(slideStyle('active', 'fade', 600).transition).toContain('600ms')
  })

  it('slides left: the next comes in from the right, the last one leaves to the left', () => {
    expect(slideStyle('active', 'slideLeft', 500).transform).toBe('translateX(0)')
    expect(slideStyle('after', 'slideLeft', 500).transform).toBe('translateX(100%)')
    expect(slideStyle('before', 'slideLeft', 500).transform).toBe('translateX(-100%)')
  })

  it('slides right the other way round, and zooms with a fade', () => {
    expect(slideStyle('after', 'slideRight', 500).transform).toBe('translateX(-100%)')
    expect(slideStyle('before', 'slideRight', 500).transform).toBe('translateX(100%)')
    expect(slideStyle('after', 'zoom', 500)).toMatchObject({ opacity: 0, transform: 'scale(1.2)' })
    expect(slideStyle('active', 'zoom', 500)).toMatchObject({ opacity: 1, transform: 'scale(1)' })
  })
})

describe('readPlaylist and buildPlaySteps', () => {
  const item = (n: number, durationSeconds?: number | null) => ({ mediaFileId: `v${n}`, mediaUrl: `http://x/${n}.mp4`, durationSeconds })

  it('is a single video until there are two or more', () => {
    expect(readPlaylist({})).toBeNull()
    expect(readPlaylist({ playlist: [item(1)] })).toBeNull()
  })

  it('reads the rotation, the sound and the repeat setting', () => {
    const settings = readPlaylist({ playlist: [item(1), item(2)], rotationMode: 'fixedSeconds', secondsPerVideo: 8, muted: false, volume: 0.4, loop: false })!

    expect(settings).toMatchObject({ rotationMode: 'fixedSeconds', secondsPerVideo: 8, muted: false, volume: 0.4, loop: false })
  })

  it('defaults to playing each to its end, muted, repeating', () => {
    expect(readPlaylist({ playlist: [item(1), item(2)] })).toMatchObject({ rotationMode: 'playFull', muted: true, loop: true, volume: 1 })
  })

  it('whole video: each runs for its own length, unknown lengths wait for the end', () => {
    const steps = buildPlaySteps({ items: [item(1, 12), item(2, 30), item(3)], rotationMode: 'playFull', secondsPerVideo: 10 })

    expect(steps.map((s) => s.playSeconds)).toEqual([12, 30, null])
    expect(cycleSeconds(steps)).toBeNull()
  })

  it('seconds each: the shorter of the setting and the video wins (a short video moves on when it ends)', () => {
    const steps = buildPlaySteps({ items: [item(1, 4), item(2, 60), item(3)], rotationMode: 'fixedSeconds', secondsPerVideo: 10 })

    expect(steps.map((s) => s.playSeconds)).toEqual([4, 10, 10])
    expect(cycleSeconds(steps)).toBe(24)
  })

  it('treats a zero or negative length as unknown', () => {
    expect(buildPlaySteps({ items: [item(1, 0), item(2, -3)], rotationMode: 'fixedSeconds', secondsPerVideo: 9 }).map((s) => s.playSeconds)).toEqual([9, 9])
  })
})

describe('what is sent to the API', () => {
  const component = (data: Record<string, unknown>) => ({ type: 'image' as const, x: 0, y: 0, width: 100, height: 50, zIndex: 0, data })

  it('never stores the temporary links of pictures or videos in a list, nor the playback plan', () => {
    const request = toApiComponent(
      component({
        mediaFileId: 'main',
        mediaUrl: 'http://x/main',
        slides: [{ mediaFileId: 'a', mediaUrl: 'http://x/a', alt: 'A' }, { mediaFileId: 'b', mediaUrl: 'http://x/b' }],
        playlist: [{ mediaFileId: 'v1', mediaUrl: 'http://x/v1', durationSeconds: 5 }],
        playbackPlan: { steps: [] },
        effect: { kind: 'fadeIn', durationMs: 800, delayMs: 0 },
      })
    )

    const props = request.properties as Record<string, any>
    expect(props.mediaUrl).toBeUndefined()
    expect(props.slides).toEqual([{ mediaFileId: 'a', alt: 'A' }, { mediaFileId: 'b' }])
    expect(props.playlist).toEqual([{ mediaFileId: 'v1', durationSeconds: 5 }])
    expect(props.playbackPlan).toBeUndefined()
    expect(props.effect).toEqual({ kind: 'fadeIn', durationMs: 800, delayMs: 0 })
  })

  it('keeps a link when the entry has no file id (an address typed in)', () => {
    const props = toApiComponent(component({ slides: [{ mediaUrl: 'http://x/typed' }] })).properties as Record<string, any>

    expect(props.slides).toEqual([{ mediaUrl: 'http://x/typed' }])
  })

  it('adds nothing for a component without lists', () => {
    const props = toApiComponent(component({ mediaFileId: 'main' })).properties as Record<string, unknown>

    expect('slides' in props).toBe(false)
    expect('playlist' in props).toBe(false)
  })
})
