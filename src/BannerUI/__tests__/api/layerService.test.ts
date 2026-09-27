import { layerService } from '@/api/layerService'
import { setupFetchMockCleanup } from '../helpers/mockFetch'
import * as apiClient from '@/api/client'
import { BannerComponent } from '@/types/banner'

describe('layerService', () => {
  setupFetchMockCleanup()

  const mockComponent: BannerComponent = {
    id: 'comp1',
    type: 'text',
    x: 50,
    y: 50,
    width: 200,
    height: 100,
    zIndex: 2,
    rotation: 0,
    opacity: 1,
    isVisible: true,
    data: {},
  }

  beforeEach(() => {
    jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)
  })

  it('reorders component by z-index', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    const result = await layerService.reorderComponent('banner1', 'comp1', 5)

    expect(result).toEqual(mockComponent)
    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/layers/reorder',
      { componentId: 'comp1', newZIndex: 5 }
    )
  })

  it('moves component forward', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    const result = await layerService.moveForward('banner1', 'comp1')

    expect(result).toEqual(mockComponent)
    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/layers/comp1/move-forward',
      {}
    )
  })

  it('moves component backward', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    const result = await layerService.moveBackward('banner1', 'comp1')

    expect(result).toEqual(mockComponent)
    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/layers/comp1/move-backward',
      {}
    )
  })

  it('sends component to front', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    const result = await layerService.sendToFront('banner1', 'comp1')

    expect(result).toEqual(mockComponent)
    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/layers/comp1/send-to-front',
      {}
    )
  })

  it('sends component to back', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    const result = await layerService.sendToBack('banner1', 'comp1')

    expect(result).toEqual(mockComponent)
    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/layers/comp1/send-to-back',
      {}
    )
  })

  it('returns updated component after layer operation', async () => {
    const updatedComponent = { ...mockComponent, zIndex: 0 }
    jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(updatedComponent)

    const result = await layerService.sendToBack('banner1', 'comp1')

    expect(result.zIndex).toBe(0)
  })

  it('constructs correct API paths for layer operations', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    await layerService.moveForward('banner-abc', 'comp-xyz')

    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner-abc/layers/comp-xyz/move-forward',
      {}
    )
  })

  it('handles reorder with high z-index value', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    await layerService.reorderComponent('banner1', 'comp1', 9999)

    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/layers/reorder',
      { componentId: 'comp1', newZIndex: 9999 }
    )
  })

  it('handles reorder with zero z-index', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    await layerService.reorderComponent('banner1', 'comp1', 0)

    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/layers/reorder',
      { componentId: 'comp1', newZIndex: 0 }
    )
  })

  it('multiple layer operations can be chained', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockComponent)

    await layerService.moveForward('banner1', 'comp1')
    await layerService.moveForward('banner1', 'comp1')
    await layerService.sendToFront('banner1', 'comp1')

    expect(postSpy).toHaveBeenCalledTimes(3)
  })
})
