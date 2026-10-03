import React from 'react'
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('@/hooks/useMediaUpload', () => ({
  useMediaUpload: () => ({ upload: mockUpload, isUploading: false, progress: 0, error: null }),
}))
const mockUpload = jest.fn()

jest.mock('@/components/media/MediaPickerDialog', () => ({
  MediaPickerDialog: ({ kind, onSelect }: { kind: string; onSelect: (item: unknown) => void }) => (
    <button
      onClick={() =>
        onSelect(
          kind === 'image'
            ? { id: 'new-pic', fileName: 'new.png', url: 'http://x/new.png', durationSeconds: null }
            : { id: 'new-vid', fileName: 'new.mp4', url: 'http://x/new.mp4', durationSeconds: 20 }
        )
      }
    >
      pick from library
    </button>
  ),
}))

import { ComponentContent } from '@/components/Display/ComponentContent'
import { EffectControls, PlaylistControls, SlideControls } from '@/components/PropertyPanel/RotationControls'
import { BannerComponent } from '@/types/banner'
import { EditorProvider, UNDO_MERGE_MS } from '@/context/EditorContext'
import { useEditor } from '@/hooks/useEditor'

const component = (overrides: Partial<BannerComponent> & { data?: Record<string, unknown> }): BannerComponent =>
  ({
    id: 'c1', bannerId: 'b', type: 'image', x: 0, y: 0, width: 400, height: 200, zIndex: 0, rotation: 0, opacity: 1, isVisible: true,
    effects: [], createdAt: '', updatedAt: '', data: {}, ...overrides,
  }) as BannerComponent

const pics = (n: number) => Array.from({ length: n }, (_, i) => ({ mediaFileId: `p${i}`, mediaUrl: `http://x/p${i}.png`, alt: `Pic ${i}` }))
const vids = (durations: (number | null)[]) =>
  durations.map((d, i) => ({ mediaFileId: `v${i}`, mediaUrl: `http://x/v${i}.mp4`, durationSeconds: d, fileName: `Video ${i}` }))

beforeAll(() => {
  // jsdom does not play media
  window.HTMLMediaElement.prototype.play = jest.fn().mockResolvedValue(undefined)
  window.HTMLMediaElement.prototype.pause = jest.fn()
})

afterEach(() => jest.useRealTimers())

describe('effects', () => {
  it('plays the effect on a screen and in a preview', () => {
    render(<ComponentContent mode="play" component={component({ type: 'text', data: { content: 'Sale', effect: { kind: 'slideUp', durationMs: 600, delayMs: 100 } } })} />)

    expect(screen.getByTestId('content-c1').style.animation).toBe('banner-fx-slideUp 600ms ease-out 100ms 1 both')
  })

  it('stands still in the editor, but marks the component so it can be told apart', () => {
    render(<ComponentContent mode="edit" component={component({ type: 'text', data: { content: 'Sale', effect: { kind: 'pulse', durationMs: 800, delayMs: 0 } } })} />)

    expect(screen.getByTestId('content-c1').style.animation).toBe('')
    expect(screen.getByTestId('component-badges')).toHaveTextContent('✦ effect')
  })

  it('has no animation and no badge for a plain component', () => {
    render(<ComponentContent mode="play" component={component({ type: 'text', data: { content: 'Plain' } })} />)

    expect(screen.getByTestId('content-c1').style.animation).toBe('')
    expect(screen.queryByTestId('component-badges')).toBeNull()
  })
})

describe('rotating pictures', () => {
  const slideshow = (extra: Record<string, unknown> = {}) =>
    component({ data: { mediaUrl: 'http://x/main.png', slides: pics(3), slideIntervalSeconds: 4, slideTransition: 'slideLeft', slideTransitionMs: 500, objectFit: 'cover', ...extra } })
  const positions = () => Array.from(document.querySelectorAll('[data-slide-position]')).map((e) => e.getAttribute('data-slide-position'))

  it('shows the first picture and moves on every interval, wrapping round', () => {
    jest.useFakeTimers()
    render(<ComponentContent mode="play" component={slideshow()} />)

    expect(positions()).toEqual(['active', 'after', 'before'])
    act(() => { jest.advanceTimersByTime(4000) })
    expect(positions()).toEqual(['before', 'active', 'after'])
    act(() => { jest.advanceTimersByTime(4000) })
    expect(positions()).toEqual(['after', 'before', 'active'])
    act(() => { jest.advanceTimersByTime(4000) })
    expect(positions()).toEqual(['active', 'after', 'before'])
  })

  it('uses the chosen transition and time', () => {
    render(<ComponentContent mode="play" component={slideshow()} />)

    const images = Array.from(document.querySelectorAll('img')) as HTMLImageElement[]
    expect(images[1].style.transform).toBe('translateX(100%)')
    expect(images[1].style.transition).toContain('500ms')
  })

  it('stands still in the editor and says how many pictures there are', () => {
    jest.useFakeTimers()
    render(<ComponentContent mode="edit" component={slideshow()} />)

    act(() => { jest.advanceTimersByTime(20000) })
    expect(positions()).toEqual(['active', 'after', 'before'])
    expect(screen.getByTestId('component-badges')).toHaveTextContent('⟳ 3 pictures')
  })

  it('skips a picture whose file is gone, and shows one picture when fewer than two remain', () => {
    const { rerender } = render(<ComponentContent mode="play" component={slideshow({ slides: [pics(1)[0], { mediaFileId: 'gone', mediaUrl: '' }, ...pics(3).slice(1)] })} />)
    expect(document.querySelectorAll('[data-slide-position]')).toHaveLength(3)

    rerender(<ComponentContent mode="play" component={slideshow({ slides: [pics(1)[0], { mediaFileId: 'gone', mediaUrl: '' }] })} />)
    expect(document.querySelectorAll('[data-slide-position]')).toHaveLength(0)
    expect(document.querySelectorAll('img')).toHaveLength(1)
  })

  it('is a plain picture when there is only one', () => {
    render(<ComponentContent mode="play" component={component({ data: { mediaUrl: 'http://x/main.png', alt: 'Logo' } })} />)

    expect(screen.queryByTestId('slideshow')).toBeNull()
    expect(screen.getByAltText('Logo')).toHaveAttribute('src', 'http://x/main.png')
  })

  it('stops its timer when removed', () => {
    jest.useFakeTimers()
    const { unmount } = render(<ComponentContent mode="play" component={slideshow()} />)

    unmount()

    expect(jest.getTimerCount()).toBe(0)
  })
})

describe('rotating videos', () => {
  const player = (data: Record<string, unknown>) =>
    component({ type: 'video', data: { playlist: vids([5, 8, 3]), rotationMode: 'playFull', muted: true, loop: true, ...data } })
  const step = () => screen.getByTestId('playlist-video').getAttribute('data-step')
  const src = () => (screen.getByTestId('playlist-video') as HTMLVideoElement).getAttribute('src')

  it('plays the videos one after another as each ends, then starts again', () => {
    render(<ComponentContent mode="play" component={player({})} />)

    expect(step()).toBe('0')
    expect(src()).toBe('http://x/v0.mp4')
    fireEvent.ended(screen.getByTestId('playlist-video'))
    expect(step()).toBe('1')
    fireEvent.ended(screen.getByTestId('playlist-video'))
    expect(step()).toBe('2')
    fireEvent.ended(screen.getByTestId('playlist-video'))
    expect(step()).toBe('0')
  })

  it('stops on the last video when it should not repeat', () => {
    render(<ComponentContent mode="play" component={player({ loop: false })} />)

    fireEvent.ended(screen.getByTestId('playlist-video'))
    fireEvent.ended(screen.getByTestId('playlist-video'))
    fireEvent.ended(screen.getByTestId('playlist-video'))

    expect(step()).toBe('2')
  })

  it('"seconds each": moves on after the set time even if the video is longer', () => {
    jest.useFakeTimers()
    render(<ComponentContent mode="play" component={player({ playlist: vids([60, 60, 60]), rotationMode: 'fixedSeconds', secondsPerVideo: 10 })} />)

    act(() => { jest.advanceTimersByTime(9_900) })
    expect(step()).toBe('0')
    act(() => { jest.advanceTimersByTime(200) })
    expect(step()).toBe('1')
  })

  it('"seconds each": a video shorter than that moves on when it ends, and the timer does not skip the next one too', () => {
    jest.useFakeTimers()
    render(<ComponentContent mode="play" component={player({ playlist: vids([4, 60, 60]), rotationMode: 'fixedSeconds', secondsPerVideo: 10 })} />)

    act(() => { jest.advanceTimersByTime(3_900) })
    fireEvent.ended(screen.getByTestId('playlist-video')) // the 4 second video ends first
    expect(step()).toBe('1')
    act(() => { jest.advanceTimersByTime(4_000) })
    expect(step()).toBe('1') // the old timer was cancelled: the second video still has time to play
    act(() => { jest.advanceTimersByTime(6_100) })
    expect(step()).toBe('2')
  })

  it('asks the video to play with the chosen sound', () => {
    render(<ComponentContent mode="play" component={player({ muted: false, volume: 0.4 })} />)

    const video = screen.getByTestId('playlist-video') as HTMLVideoElement
    expect(video.muted).toBe(false)
    expect(video.volume).toBeCloseTo(0.4)
    expect(window.HTMLMediaElement.prototype.play).toHaveBeenCalled()
  })

  it('plays muted when the browser refuses sound without a click', async () => {
    const play = window.HTMLMediaElement.prototype.play as jest.Mock
    play.mockRejectedValueOnce(new Error('NotAllowedError')).mockResolvedValue(undefined)
    render(<ComponentContent mode="play" component={player({ muted: false, volume: 1 })} />)

    await waitFor(() => expect((screen.getByTestId('playlist-video') as HTMLVideoElement).muted).toBe(true))
  })

  it('moves past a video that cannot be loaded', () => {
    jest.useFakeTimers()
    render(<ComponentContent mode="play" component={player({})} />)

    fireEvent.error(screen.getByTestId('playlist-video'))
    act(() => { jest.advanceTimersByTime(1_100) })

    expect(step()).toBe('1')
  })

  it('shows the first video, not the rotation, in the editor, with a badge', () => {
    const { container } = render(<ComponentContent mode="edit" component={player({})} />)

    expect(screen.queryByTestId('playlist-video')).toBeNull()
    expect(container.querySelector('video')?.getAttribute('src')).toBe('http://x/v0.mp4')
    expect(screen.getByTestId('component-badges')).toHaveTextContent('⟳ 3 videos')
  })

  it('is an ordinary video when there is no list', () => {
    const { container } = render(<ComponentContent mode="play" component={component({ type: 'video', data: { mediaUrl: 'http://x/one.mp4', muted: true, loop: true } })} />)

    expect(screen.queryByTestId('playlist-video')).toBeNull()
    expect((container.querySelector('video') as HTMLVideoElement).loop).toBe(true)
  })
})

describe('Effect controls', () => {
  it('picks an effect and its timing in seconds', async () => {
    const onChange = jest.fn()
    const { rerender } = render(<EffectControls data={{}} onChange={onChange} />)

    expect(screen.queryByLabelText('Takes (seconds)')).toBeNull()
    await userEvent.selectOptions(screen.getByLabelText('Effect'), 'zoomIn')
    expect(onChange).toHaveBeenLastCalledWith('effect', { kind: 'zoomIn', durationMs: 800, delayMs: 0 })

    rerender(<EffectControls data={{ effect: { kind: 'zoomIn', durationMs: 800, delayMs: 0 } }} onChange={onChange} />)
    fireEvent.change(screen.getByLabelText('Takes (seconds)'), { target: { value: '1.5' } })
    expect(onChange).toHaveBeenLastCalledWith('effect', { kind: 'zoomIn', durationMs: 1500, delayMs: 0 })
    fireEvent.change(screen.getByLabelText('Starts after (seconds)'), { target: { value: '2' } })
    expect(onChange).toHaveBeenLastCalledWith('effect', { kind: 'zoomIn', durationMs: 800, delayMs: 2000 })
  })

  it('goes back to no effect, and says whether it repeats', async () => {
    const onChange = jest.fn()
    const { rerender } = render(<EffectControls data={{ effect: { kind: 'pulse', durationMs: 800, delayMs: 0 } }} onChange={onChange} />)
    expect(screen.getByText(/keeps repeating/)).toBeInTheDocument()

    rerender(<EffectControls data={{ effect: { kind: 'fadeIn', durationMs: 800, delayMs: 0 } }} onChange={onChange} />)
    expect(screen.getByText(/plays once/)).toBeInTheDocument()

    await userEvent.selectOptions(screen.getByLabelText('Effect'), '')
    expect(onChange).toHaveBeenLastCalledWith('effect', { kind: 'none', durationMs: 800, delayMs: 0 })
  })
})

describe('Rotating picture controls', () => {
  it('makes the picture already on the component the first one when adding another', async () => {
    const onChange = jest.fn()
    render(<SlideControls data={{ mediaFileId: 'main', mediaUrl: 'http://x/main.png', alt: 'Hero' }} onChange={onChange} />)

    await userEvent.click(screen.getByRole('button', { name: 'Add from my files' }))
    await userEvent.click(screen.getByText('pick from library'))

    expect(onChange).toHaveBeenCalledWith('slides', [
      { mediaFileId: 'main', mediaUrl: 'http://x/main.png', alt: 'Hero' },
      { mediaFileId: 'new-pic', mediaUrl: 'http://x/new.png', alt: 'new.png' },
    ])
  })

  it('adds an uploaded picture', async () => {
    mockUpload.mockResolvedValue({ mediaFileId: 'up-1', url: 'http://x/up.png' })
    const onChange = jest.fn()
    render(<SlideControls data={{ slides: pics(2) }} onChange={onChange} />)

    await userEvent.upload(screen.getByLabelText('Upload a picture to add'), new File(['x'], 'up.png', { type: 'image/png' }))

    await waitFor(() => expect(onChange).toHaveBeenCalledWith('slides', [...pics(2), { mediaFileId: 'up-1', mediaUrl: 'http://x/up.png', alt: 'up.png' }]))
  })

  it('reorders and removes pictures', async () => {
    const onChange = jest.fn()
    render(<SlideControls data={{ slides: pics(3) }} onChange={onChange} />)

    await userEvent.click(screen.getByRole('button', { name: 'Move Pic 0 down' }))
    expect(onChange).toHaveBeenLastCalledWith('slides', [pics(3)[1], pics(3)[0], pics(3)[2]])

    await userEvent.click(screen.getByRole('button', { name: 'Remove Pic 1' }))
    expect(onChange).toHaveBeenLastCalledWith('slides', [pics(3)[0], pics(3)[2]])

    expect(screen.getByRole('button', { name: 'Move Pic 0 up' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Move Pic 2 down' })).toBeDisabled()
  })

  it('shows the timing only once there are two pictures, and keeps values in range', () => {
    const onChange = jest.fn()
    const { rerender } = render(<SlideControls data={{ slides: pics(1) }} onChange={onChange} />)
    expect(screen.getByText(/Add at least one more picture/)).toBeInTheDocument()
    expect(screen.queryByLabelText('Each picture (seconds)')).toBeNull()

    rerender(<SlideControls data={{ slides: pics(2), slideIntervalSeconds: 5 }} onChange={onChange} />)
    fireEvent.change(screen.getByLabelText('Each picture (seconds)'), { target: { value: '200' } })
    expect(onChange).toHaveBeenLastCalledWith('slideIntervalSeconds', 60)
    fireEvent.change(screen.getByLabelText('Change takes (seconds)'), { target: { value: '0.1' } })
    expect(onChange).toHaveBeenLastCalledWith('slideTransitionMs', 200)
    fireEvent.change(screen.getByLabelText('How it changes'), { target: { value: 'zoom' } })
    expect(onChange).toHaveBeenLastCalledWith('slideTransition', 'zoom')
  })
})

describe('Rotating video controls', () => {
  it('makes the video already on the component the first one, with its length', async () => {
    const onChange = jest.fn()
    render(<PlaylistControls data={{ mediaFileId: 'main', mediaUrl: 'http://x/main.mp4', duration: 42 }} onChange={onChange} />)

    await userEvent.click(screen.getByRole('button', { name: 'Add from my files' }))
    await userEvent.click(screen.getByText('pick from library'))

    expect(onChange).toHaveBeenCalledWith('playlist', [
      { mediaFileId: 'main', mediaUrl: 'http://x/main.mp4', durationSeconds: 42, fileName: 'First video' },
      { mediaFileId: 'new-vid', mediaUrl: 'http://x/new.mp4', durationSeconds: 20, fileName: 'new.mp4' },
    ])
  })

  it('lists the videos with their lengths and totals one round', () => {
    render(<PlaylistControls data={{ playlist: vids([60, 30]), rotationMode: 'playFull' }} onChange={jest.fn()} />)

    expect(screen.getByTestId('rotation-row-0')).toHaveTextContent('Video 0 (1:00)')
    expect(screen.getByTestId('playlist-cycle')).toHaveTextContent('One round takes 1:30.')
  })

  it('switches between the whole video and a number of seconds', async () => {
    const onChange = jest.fn()
    const { rerender } = render(<PlaylistControls data={{ playlist: vids([60, 30]), rotationMode: 'playFull' }} onChange={onChange} />)

    expect(screen.queryByLabelText('Seconds for each video')).toBeNull()
    await userEvent.click(screen.getByLabelText('A number of seconds each'))
    expect(onChange).toHaveBeenLastCalledWith('rotationMode', 'fixedSeconds')

    rerender(<PlaylistControls data={{ playlist: vids([60, 4]), rotationMode: 'fixedSeconds', secondsPerVideo: 10 }} onChange={onChange} />)
    expect(screen.getByTestId('playlist-cycle')).toHaveTextContent('One round takes 0:14.')
    expect(screen.getByText(/shorter than this moves on when it ends/)).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText('Seconds for each video'), { target: { value: '99999' } })
    expect(onChange).toHaveBeenLastCalledWith('secondsPerVideo', 3600)
  })

  it('offers a volume only when the sound is on', () => {
    const onChange = jest.fn()
    const { rerender } = render(<PlaylistControls data={{ playlist: vids([5, 5]), muted: true }} onChange={onChange} />)
    expect(screen.queryByLabelText('Volume')).toBeNull()
    expect(screen.getByTestId('playlist-cycle')).toHaveTextContent('Sound is off')

    rerender(<PlaylistControls data={{ playlist: vids([5, 5]), muted: false, volume: 1 }} onChange={onChange} />)
    fireEvent.change(screen.getByLabelText('Volume'), { target: { value: '0.3' } })
    expect(onChange).toHaveBeenLastCalledWith('volume', 0.3)
  })

  it('says when a length is not known for every video', () => {
    render(<PlaylistControls data={{ playlist: vids([5, null]), rotationMode: 'playFull' }} onChange={jest.fn()} />)

    expect(screen.getByTestId('playlist-cycle')).toHaveTextContent('a length is not known for every video')
  })

  it('reorders and removes videos', async () => {
    const onChange = jest.fn()
    render(<PlaylistControls data={{ playlist: vids([5, 6, 7]) }} onChange={onChange} />)

    await userEvent.click(screen.getByRole('button', { name: /Move Video 2 .* up/ }))
    expect(onChange).toHaveBeenLastCalledWith('playlist', [vids([5, 6, 7])[0], vids([5, 6, 7])[2], vids([5, 6, 7])[1]])
    await userEvent.click(screen.getByRole('button', { name: /Remove Video 0/ }))
    expect(onChange).toHaveBeenLastCalledWith('playlist', vids([5, 6, 7]).slice(1))
  })
})

describe('undo and redo', () => {
  function Probe() {
    const { state, addComponent, updateComponent, deleteComponent, undo, redo, setBanner } = useEditor()
    const first = state.components[0]
    return (
      <div>
        <p data-testid="x">{first ? first.x : 'none'}</p>
        <p data-testid="past">{state.past.length}</p>
        <p data-testid="future">{state.future.length}</p>
        <p data-testid="dirty">{String(state.isDirty)}</p>
        <button onClick={() => setBanner({ id: 'b', components: [component({ x: 0 })] } as never)}>load</button>
        <button onClick={() => updateComponent('c1', { x: state.components[0].x + 10 })}>move</button>
        <button onClick={() => addComponent(component({ id: 'c2' }))}>add</button>
        <button onClick={() => deleteComponent('c2')}>delete</button>
        <button onClick={undo}>undo</button>
        <button onClick={redo}>redo</button>
      </div>
    )
  }
  const mount = async () => {
    render(<EditorProvider><Probe /></EditorProvider>)
    await userEvent.click(screen.getByText('load'))
  }

  it('undoes and redoes changes one step at a time', async () => {
    jest.useFakeTimers({ advanceTimers: true })
    await mount()
    await userEvent.click(screen.getByText('move'))
    act(() => { jest.advanceTimersByTime(UNDO_MERGE_MS + 100) })
    await userEvent.click(screen.getByText('move'))
    expect(screen.getByTestId('x')).toHaveTextContent('20')

    await userEvent.click(screen.getByText('undo'))
    expect(screen.getByTestId('x')).toHaveTextContent('10')
    await userEvent.click(screen.getByText('undo'))
    expect(screen.getByTestId('x')).toHaveTextContent('0')
    await userEvent.click(screen.getByText('undo'))
    expect(screen.getByTestId('x')).toHaveTextContent('0') // nothing more to undo

    await userEvent.click(screen.getByText('redo'))
    await userEvent.click(screen.getByText('redo'))
    expect(screen.getByTestId('x')).toHaveTextContent('20')
  })

  it('turns a quick run of changes (a drag) into one step', async () => {
    await mount()

    for (let i = 0; i < 5; i++) await userEvent.click(screen.getByText('move'))

    expect(screen.getByTestId('x')).toHaveTextContent('50')
    expect(screen.getByTestId('past')).toHaveTextContent('1')
    await userEvent.click(screen.getByText('undo'))
    expect(screen.getByTestId('x')).toHaveTextContent('0')
  })

  it('marks the banner as changed again after an undo, and drops redo when something new is done', async () => {
    await mount()
    await userEvent.click(screen.getByText('move'))
    await userEvent.click(screen.getByText('undo'))
    expect(screen.getByTestId('future')).toHaveTextContent('1')
    expect(screen.getByTestId('dirty')).toHaveTextContent('true')

    await userEvent.click(screen.getByText('move'))
    expect(screen.getByTestId('future')).toHaveTextContent('0')
  })

  it('starts afresh after a component is added or deleted, since those are stored straight away', async () => {
    await mount()
    await userEvent.click(screen.getByText('move'))
    expect(screen.getByTestId('past')).toHaveTextContent('1')

    await userEvent.click(screen.getByText('add'))
    expect(screen.getByTestId('past')).toHaveTextContent('0')

    await userEvent.click(screen.getByText('move'))
    await userEvent.click(screen.getByText('delete'))
    expect(screen.getByTestId('past')).toHaveTextContent('0')
    expect(screen.getByTestId('future')).toHaveTextContent('0')
  })

  it('keeps at most fifty steps', async () => {
    await mount()
    let clock = Date.now()
    const now = jest.spyOn(Date, 'now').mockImplementation(() => clock)

    for (let i = 0; i < 60; i++) {
      clock += UNDO_MERGE_MS + 100 // far enough apart to be separate steps
      fireEvent.click(screen.getByText('move'))
    }

    now.mockRestore()
    expect(screen.getByTestId('past')).toHaveTextContent('50')
  })
})
