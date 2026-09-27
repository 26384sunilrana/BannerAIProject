import { versionService } from '@/api/versionService'
import { setupFetchMockCleanup } from '../helpers/mockFetch'
import * as apiClient from '@/api/client'
import { BannerVersion } from '@/types/banner'

describe('versionService', () => {
  setupFetchMockCleanup()

  const mockVersion: BannerVersion = {
    id: 'v1',
    bannerId: 'banner1',
    versionNumber: 1,
    title: 'Version 1',
    description: 'First version',
    createdAt: new Date().toISOString(),
    createdBy: 'user1',
  }

  beforeEach(() => {
    jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue([mockVersion])
    jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockVersion)
  })

  it('lists all versions of a banner', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue([mockVersion])

    const result = await versionService.listVersions('banner1')

    expect(Array.isArray(result)).toBe(true)
    expect(result.length).toBeGreaterThan(0)
    expect(getSpy).toHaveBeenCalledWith('/banners/banner1/versions')
  })

  it('gets specific version by number', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockVersion)

    const result = await versionService.getVersion('banner1', 1)

    expect(result).toEqual(mockVersion)
    expect(getSpy).toHaveBeenCalledWith('/banners/banner1/versions/1')
  })

  it('creates snapshot of current banner', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockVersion)

    const result = await versionService.createSnapshot('banner1')

    expect(result).toEqual(mockVersion)
    expect(postSpy).toHaveBeenCalledWith('/banners/banner1/versions', {})
  })

  it('restores banner to specific version', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockVersion)

    const result = await versionService.restoreVersion('banner1', 1)

    expect(result).toEqual(mockVersion)
    expect(postSpy).toHaveBeenCalledWith('/banners/banner1/versions/1/restore', {})
  })

  it('returns multiple versions', async () => {
    const versions = [
      mockVersion,
      { ...mockVersion, versionNumber: 2, id: 'v2' },
      { ...mockVersion, versionNumber: 3, id: 'v3' },
    ]
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(versions)

    const result = await versionService.listVersions('banner1')

    expect(result).toHaveLength(3)
    expect(result[0].versionNumber).toBe(1)
    expect(result[1].versionNumber).toBe(2)
    expect(result[2].versionNumber).toBe(3)
  })

  it('version has required properties', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockVersion)

    const result = await versionService.getVersion('banner1', 1)

    expect(result.id).toBeTruthy()
    expect(result.bannerId).toBeTruthy()
    expect(result.versionNumber).toBeGreaterThan(0)
    expect(result.title).toBeTruthy()
    expect(result.createdAt).toBeTruthy()
  })

  it('handles version with large version number', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockVersion)

    await versionService.getVersion('banner1', 9999)

    expect(getSpy).toHaveBeenCalledWith('/banners/banner1/versions/9999')
  })

  it('construct correct API paths', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue([mockVersion])

    await versionService.listVersions('banner-abc')

    expect(getSpy).toHaveBeenCalledWith('/banners/banner-abc/versions')
  })

  it('restore operation with correct payload', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockVersion)

    await versionService.restoreVersion('banner1', 5)

    expect(postSpy).toHaveBeenCalledWith('/banners/banner1/versions/5/restore', {})
  })

  it('snapshot preserves banner state', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockVersion)

    const result = await versionService.createSnapshot('banner1')

    expect(result.bannerId).toBe('banner1')
    expect(postSpy).toHaveBeenCalledWith('/banners/banner1/versions', {})
  })

  it('handles empty version list', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue([])

    const result = await versionService.listVersions('empty-banner')

    expect(result).toEqual([])
  })
})
