import { act, renderHook } from '@testing-library/react'
import { useSave } from '@/hooks/useSave'
import { bannerService } from '@/api/bannerService'
import { Banner, BannerComponent } from '@/types/banner'

jest.mock('@/api/bannerService', () => ({
  bannerService: { updateBanner: jest.fn(), updateComponent: jest.fn() },
}))

const banner = { id: 'b1', title: 'Sale', description: 'd', width: 1200, height: 600 } as Banner
const component = (id: string) => ({ id, type: 'text', x: 1, y: 2, width: 3, height: 4, zIndex: 0, data: {} }) as unknown as BannerComponent

const mocked = bannerService as jest.Mocked<typeof bannerService>

beforeEach(() => {
  jest.clearAllMocks()
  mocked.updateBanner.mockResolvedValue(undefined)
  mocked.updateComponent.mockResolvedValue({} as BannerComponent)
})

describe('useSave', () => {
  it('saves the banner details and every full component', async () => {
    const { result } = renderHook(() => useSave('b1', [component('c1'), component('c2')], banner))

    act(() => result.current.markDirty())
    await act(async () => {
      await result.current.save()
    })

    expect(mocked.updateBanner).toHaveBeenCalledWith('b1', { title: 'Sale', description: 'd', width: 1200, height: 600 })
    expect(mocked.updateComponent).toHaveBeenCalledTimes(2)
    expect(mocked.updateComponent).toHaveBeenCalledWith('b1', 'c1', expect.objectContaining({ type: 'text', x: 1 }))
    expect(result.current.isDirty).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('does not report success when a component could not be saved', async () => {
    mocked.updateComponent.mockImplementation(async (_b, id) => {
      if (id === 'c2') throw { response: { status: 400, data: { message: 'Component with ZIndex 1 already exists' } } }
      return {} as BannerComponent
    })
    const { result } = renderHook(() => useSave('b1', [component('c1'), component('c2')], banner))

    act(() => result.current.markDirty())
    await act(async () => {
      await expect(result.current.save()).rejects.toThrow(/ZIndex 1 already exists/)
    })

    expect(mocked.updateComponent).toHaveBeenCalledTimes(2) // the other component was still saved
    expect(result.current.isDirty).toBe(true) // and the banner stays marked as unsaved
    expect(result.current.error).toMatch(/ZIndex 1 already exists/)
  })

  it('reports a failure to save the banner details too', async () => {
    mocked.updateBanner.mockRejectedValue({ response: { status: 403 } })
    const { result } = renderHook(() => useSave('b1', [], banner))

    await act(async () => {
      await expect(result.current.save()).rejects.toThrow(/Banner details/)
    })
  })
})
