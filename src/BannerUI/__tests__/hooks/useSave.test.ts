import { act, renderHook } from '@testing-library/react'
import { useSave } from '@/hooks/useSave'
import { bannerService } from '@/api/bannerService'
import { Banner, BannerComponent } from '@/types/banner'

jest.mock('@/api/bannerService', () => ({
  bannerService: { updateBanner: jest.fn(), updateComponent: jest.fn() },
}))

const banner = { id: '1', title: 'Test Banner', description: 'Test', width: 1200, height: 600 } as Banner
const components = [{ id: 'c1', type: 'text', x: 50, y: 50, width: 200, height: 100, zIndex: 0, data: {} }] as unknown as BannerComponent[]
const mocked = bannerService as jest.Mocked<typeof bannerService>

beforeEach(() => {
  jest.clearAllMocks()
  jest.useRealTimers()
  mocked.updateBanner.mockResolvedValue(undefined)
  mocked.updateComponent.mockResolvedValue({} as BannerComponent)
})

describe('useSave', () => {
  it('starts clean', () => {
    const { result } = renderHook(() => useSave('1', components, banner))

    expect(result.current).toMatchObject({ isSaving: false, isDirty: false, lastSavedAt: null, error: null })
  })

  it('marks the banner as having unsaved changes', () => {
    const { result } = renderHook(() => useSave('1', components, banner))

    act(() => result.current.markDirty())

    expect(result.current.isDirty).toBe(true)
  })

  it('records when it last saved and clears the unsaved flag', async () => {
    const { result } = renderHook(() => useSave('1', components, banner))
    act(() => result.current.markDirty())

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.isSaving).toBe(false)
    expect(result.current.isDirty).toBe(false)
    expect(result.current.lastSavedAt).not.toBeNull()
  })

  it('shows that it is saving while the request is open', async () => {
    let finish: () => void = () => undefined
    mocked.updateBanner.mockReturnValue(new Promise<void>((resolve) => (finish = resolve)))
    const { result } = renderHook(() => useSave('1', components, banner))

    let pending: Promise<void> = Promise.resolve()
    act(() => {
      pending = result.current.save() as Promise<void>
    })
    expect(result.current.isSaving).toBe(true)

    await act(async () => {
      finish()
      await pending
    })
    expect(result.current.isSaving).toBe(false)
  })

  it('saves a banner that has no components', async () => {
    const { result } = renderHook(() => useSave('1', [], banner))

    await act(async () => {
      await result.current.save()
    })

    expect(mocked.updateBanner).toHaveBeenCalledTimes(1)
    expect(mocked.updateComponent).not.toHaveBeenCalled()
  })

  it('does nothing before the banner has loaded', async () => {
    const { result } = renderHook(() => useSave('1', components, null))

    await act(async () => {
      await result.current.save()
    })

    expect(mocked.updateBanner).not.toHaveBeenCalled()
    expect(result.current.lastSavedAt).toBeNull()
  })

  it('clears an earlier error when the next save works', async () => {
    mocked.updateBanner.mockRejectedValueOnce({ response: { status: 500 } })
    const { result } = renderHook(() => useSave('1', components, banner))

    await act(async () => {
      await expect(result.current.save()).rejects.toThrow()
    })
    expect(result.current.error).not.toBeNull()

    await act(async () => {
      await result.current.save()
    })
    expect(result.current.error).toBeNull()
  })

  it('saves by itself some time after a change', async () => {
    jest.useFakeTimers()
    const { result } = renderHook(() => useSave('1', components, banner))

    act(() => result.current.markDirty())
    await act(async () => {
      jest.advanceTimersByTime(30000)
    })

    expect(mocked.updateBanner).toHaveBeenCalledTimes(1)
  })
})
