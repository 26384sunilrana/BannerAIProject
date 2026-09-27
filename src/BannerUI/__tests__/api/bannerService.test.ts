import { bannerService } from '@/api/bannerService'
import { setupFetchMockCleanup, mockFetchOnce } from '../helpers/mockFetch'
import { Banner, BannerComponent } from '@/types/banner'
import * as apiClient from '@/api/client'

describe('bannerService', () => {
  setupFetchMockCleanup()

  const mockBanner: Banner = {
    id: '1',
    title: 'Test Banner',
    description: 'Test Description',
    width: 1200,
    height: 600,
    backgroundColor: '#fff',
  }

  const mockComponent: BannerComponent = {
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
  }

  beforeEach(() => {
    jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockBanner)
    jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)
    jest.spyOn(apiClient.apiClient, 'put').mockResolvedValue(mockBanner)
    jest.spyOn(apiClient.apiClient, 'delete').mockResolvedValue(undefined)
  })

  it('gets banner by id', async () => {
    jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockBanner)

    const result = await bannerService.getBanner('1')
    expect(result).toEqual(mockBanner)
  })

  it('updates banner', async () => {
    jest.spyOn(apiClient.apiClient, 'put').mockResolvedValue(mockBanner)

    const result = await bannerService.updateBanner('1', {
      title: 'Updated',
      description: 'Updated Description',
    })
    expect(result).toEqual(mockBanner)
  })

  it('adds component to banner', async () => {
    jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    const result = await bannerService.addComponent('1', {
      type: 'text',
      x: 50,
      y: 50,
      width: 200,
      height: 100,
      zIndex: 0,
      data: { content: 'Test' },
    })
    expect(result).toEqual(mockComponent)
  })

  it('updates component', async () => {
    jest.spyOn(apiClient.apiClient, 'put').mockResolvedValue(mockComponent)

    const result = await bannerService.updateComponent('1', 'comp1', {
      x: 100,
      y: 100,
      width: 250,
      height: 120,
    })
    expect(result).toEqual(mockComponent)
  })

  it('deletes component', async () => {
    jest.spyOn(apiClient.apiClient, 'delete').mockResolvedValue(undefined)

    await expect(
      bannerService.deleteComponent('1', 'comp1')
    ).resolves.toBeUndefined()
  })

  it('swaps component', async () => {
    jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    const result = await bannerService.swapComponent('1', 'comp1', 'comp2')
    expect(result).toEqual(mockComponent)
  })

  it('calls correct api endpoints', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockBanner)

    await bannerService.getBanner('1')
    expect(getSpy).toHaveBeenCalledWith('/banners/1')
  })

  it('passes update request to api', async () => {
    const putSpy = jest.spyOn(apiClient.apiClient, 'put').mockResolvedValue(mockBanner)

    await bannerService.updateBanner('1', { title: 'New' })
    expect(putSpy).toHaveBeenCalledWith('/banners/1', { title: 'New' })
  })
})
