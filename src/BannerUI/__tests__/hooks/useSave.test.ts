import { renderHook, act } from '@testing-library/react'
import { useSave } from '@/hooks/useSave'
import { setupFetchMockCleanup, mockFetchOnce } from '../helpers/mockFetch'
import { Banner, BannerComponent } from '@/types/banner'
import * as bannerService from '@/api/bannerService'

describe('useSave', () => {
  setupFetchMockCleanup()

  const mockBanner: Banner = {
    id: '1',
    title: 'Test Banner',
    description: 'Test Description',
    width: 1200,
    height: 600,
    backgroundColor: '#fff',
  }

  const mockComponents: BannerComponent[] = [
    {
      id: 'comp1',
      type: 'text',
      x: 50,
      y: 50,
      width: 200,
      height: 100,
      zIndex: 0,
      rotation: 0,
      opacity: 1,
      isVisible: true,
      data: { content: 'Test' },
    },
  ]

  beforeEach(() => {
    jest.spyOn(bannerService, 'bannerService', 'get').mockReturnValue({
      getBanner: jest.fn(),
      updateBanner: jest.fn().mockResolvedValue(mockBanner),
      addComponent: jest.fn(),
      updateComponent: jest.fn().mockResolvedValue(mockComponents[0]),
      deleteComponent: jest.fn(),
      swapComponent: jest.fn(),
    } as any)
  })

  it('initializes with correct save state', () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, mockBanner)
    )

    expect(result.current.isSaving).toBe(false)
    expect(result.current.isDirty).toBe(false)
    expect(result.current.lastSavedAt).toBeNull()
    expect(result.current.error).toBeNull()
  })

  it('marks banner as dirty', () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, mockBanner)
    )

    act(() => {
      result.current.markDirty()
    })

    expect(result.current.isDirty).toBe(true)
  })

  it('saves banner successfully', async () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, mockBanner)
    )

    mockFetchOnce(200, { data: mockBanner })

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.isSaving).toBe(false)
    expect(result.current.lastSavedAt).not.toBeNull()
  })

  it('sets isDirty to false after save', async () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, mockBanner)
    )

    act(() => {
      result.current.markDirty()
    })

    expect(result.current.isDirty).toBe(true)

    mockFetchOnce(200, { data: mockBanner })

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.isDirty).toBe(false)
  })

  it('handles save errors', async () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, mockBanner)
    )

    mockFetchOnce(500, { error: 'Server error' })

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.error).toBeTruthy()
    expect(result.current.isSaving).toBe(false)
  })

  it('does nothing when banner is null', async () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, null)
    )

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.isSaving).toBe(false)
  })

  it('tracks save state during save operation', async () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, mockBanner)
    )

    mockFetchOnce(200, { data: mockBanner })

    act(() => {
      result.current.markDirty()
    })

    let savingStateChanged = false

    act(async () => {
      const savePromise = result.current.save()
      if (result.current.isSaving) {
        savingStateChanged = true
      }
      await savePromise
    })

    expect(savingStateChanged).toBe(true)
    expect(result.current.isSaving).toBe(false)
  })

  it('saves all components', async () => {
    const multipleComponents: BannerComponent[] = [
      mockComponents[0],
      {
        ...mockComponents[0],
        id: 'comp2',
        x: 300,
      },
    ]

    const { result } = renderHook(() =>
      useSave('1', multipleComponents, mockBanner)
    )

    mockFetchOnce(200, { data: mockBanner })

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.error).toBeNull()
  })

  it('records timestamp on successful save', async () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, mockBanner)
    )

    mockFetchOnce(200, { data: mockBanner })

    const beforeSave = Date.now()

    await act(async () => {
      await result.current.save()
    })

    const afterSave = Date.now()

    expect(result.current.lastSavedAt).toBeTruthy()
    if (result.current.lastSavedAt) {
      expect(result.current.lastSavedAt).toBeGreaterThanOrEqual(beforeSave)
      expect(result.current.lastSavedAt).toBeLessThanOrEqual(afterSave)
    }
  })

  it('clears error on successful save', async () => {
    const { result } = renderHook(() =>
      useSave('1', mockComponents, mockBanner)
    )

    // First fail
    mockFetchOnce(500, { error: 'Error' })

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.error).toBeTruthy()

    // Then succeed
    mockFetchOnce(200, { data: mockBanner })

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.error).toBeNull()
  })

  it('handles banner with no components', async () => {
    const { result } = renderHook(() =>
      useSave('1', [], mockBanner)
    )

    mockFetchOnce(200, { data: mockBanner })

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.error).toBeNull()
  })

  it('updates components individually', async () => {
    const components = [
      { ...mockComponents[0], id: 'comp1' },
      { ...mockComponents[0], id: 'comp2' },
    ]

    const { result } = renderHook(() =>
      useSave('1', components, mockBanner)
    )

    mockFetchOnce(200, { data: mockBanner })

    await act(async () => {
      await result.current.save()
    })

    expect(result.current.error).toBeNull()
  })
})
