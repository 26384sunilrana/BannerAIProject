import { effectsService } from '@/api/effectsService'
import { setupFetchMockCleanup } from '../helpers/mockFetch'
import * as apiClient from '@/api/client'

describe('effectsService', () => {
  setupFetchMockCleanup()

  const mockEffect = {
    id: 'eff1',
    name: 'Fade',
    type: 'fade',
    duration: 1000,
    intensity: 0.5,
  }

  beforeEach(() => {
    jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockEffect)
    jest.spyOn(apiClient.apiClient, 'put').mockResolvedValue(mockEffect)
    jest.spyOn(apiClient.apiClient, 'delete').mockResolvedValue(undefined)
    jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue([mockEffect])
  })

  it('applies effect to component', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockEffect)

    const result = await effectsService.applyEffect('banner1', 'comp1', {
      name: 'Fade',
      type: 'fade',
      duration: 1000,
      intensity: 0.5,
    })

    expect(result).toEqual(mockEffect)
    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/components/comp1/effects',
      expect.any(Object)
    )
  })

  it('updates effect', async () => {
    const putSpy = jest.spyOn(apiClient.apiClient, 'put').mockResolvedValue(mockEffect)

    const result = await effectsService.updateEffect(
      'banner1',
      'comp1',
      'eff1',
      { intensity: 0.8 }
    )

    expect(result).toEqual(mockEffect)
    expect(putSpy).toHaveBeenCalledWith(
      '/banners/banner1/components/comp1/effects/eff1',
      { intensity: 0.8 }
    )
  })

  it('removes effect', async () => {
    const deleteSpy = jest.spyOn(apiClient.apiClient, 'delete').mockResolvedValue(undefined)

    await expect(
      effectsService.removeEffect('banner1', 'comp1', 'eff1')
    ).resolves.toBeUndefined()

    expect(deleteSpy).toHaveBeenCalledWith(
      '/banners/banner1/components/comp1/effects/eff1'
    )
  })

  it('gets all effects for component', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue([mockEffect])

    const result = await effectsService.getComponentEffects('banner1', 'comp1')

    expect(Array.isArray(result)).toBe(true)
    expect(getSpy).toHaveBeenCalledWith(
      '/banners/banner1/components/comp1/effects'
    )
  })

  it('handles multiple effects', async () => {
    const effects = [
      mockEffect,
      { ...mockEffect, id: 'eff2', name: 'Blur' },
      { ...mockEffect, id: 'eff3', name: 'Glow' },
    ]

    jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(effects)

    const result = await effectsService.getComponentEffects('banner1', 'comp1')

    expect(result).toHaveLength(3)
  })

  it('applies effect with all properties', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockEffect)

    const request = {
      name: 'CustomEffect',
      type: 'custom',
      duration: 2000,
      intensity: 0.7,
      properties: { custom: 'value' },
    }

    await effectsService.applyEffect('banner1', 'comp1', request as any)

    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner1/components/comp1/effects',
      request
    )
  })

  it('updates effect with partial data', async () => {
    const putSpy = jest.spyOn(apiClient.apiClient, 'put').mockResolvedValue(mockEffect)

    await effectsService.updateEffect('banner1', 'comp1', 'eff1', {
      intensity: 0.9,
    })

    expect(putSpy).toHaveBeenCalledWith(
      '/banners/banner1/components/comp1/effects/eff1',
      { intensity: 0.9 }
    )
  })

  it('constructs correct API paths', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockEffect)

    await effectsService.applyEffect('banner-xyz', 'component-abc', {
      name: 'Test',
      type: 'test',
      duration: 500,
    } as any)

    expect(postSpy).toHaveBeenCalledWith(
      '/banners/banner-xyz/components/component-abc/effects',
      expect.any(Object)
    )
  })
})
