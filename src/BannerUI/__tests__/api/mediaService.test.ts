import { mediaService, MediaFile } from '@/api/mediaService'
import { setupFetchMockCleanup } from '../helpers/mockFetch'
import * as apiClient from '@/api/client'

describe('mediaService', () => {
  setupFetchMockCleanup()

  const mockMediaFile: MediaFile = {
    id: 'media1',
    fileName: 'test.jpg',
    fileType: 'image/jpeg',
    fileSize: 10240,
    status: 'complete',
    url: 'https://example.com/test.jpg',
  }

  beforeEach(() => {
    jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue({ mediaFileId: 'media1' })
    jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockMediaFile)
    jest.spyOn(apiClient.apiClient, 'delete').mockResolvedValue(undefined)
    jest.spyOn(apiClient.apiClient, 'upload').mockResolvedValue({ uploaded: true })
  })

  it('initializes upload', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue({ mediaFileId: 'media1' })

    const result = await mediaService.initializeUpload({
      fileName: 'test.jpg',
      fileType: 'image/jpeg',
      fileSize: 10240,
      totalChunks: 1,
    })

    expect(result.mediaFileId).toBe('media1')
    expect(postSpy).toHaveBeenCalledWith(
      '/media/upload/initialize',
      expect.any(Object)
    )
  })

  it('uploads chunk', async () => {
    const uploadSpy = jest.spyOn(apiClient.apiClient, 'upload').mockResolvedValue({ uploaded: true })

    const chunk = new Blob(['chunk data'])
    const result = await mediaService.uploadChunk('media1', 0, chunk as File)

    expect(result.uploaded).toBe(true)
    expect(uploadSpy).toHaveBeenCalledWith(
      '/media/media1/chunks/0',
      expect.any(Object),
      undefined
    )
  })

  it('uploads chunk with progress callback', async () => {
    const uploadSpy = jest.spyOn(apiClient.apiClient, 'upload').mockResolvedValue({ uploaded: true })
    const onProgress = jest.fn()

    const chunk = new Blob(['chunk data'])
    await mediaService.uploadChunk('media1', 0, chunk as File, onProgress)

    expect(uploadSpy).toHaveBeenCalledWith(
      '/media/media1/chunks/0',
      expect.any(Object),
      onProgress
    )
  })

  it('completes upload', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue(mockMediaFile)

    const result = await mediaService.completeUpload('media1')

    expect(result).toEqual(mockMediaFile)
    expect(postSpy).toHaveBeenCalledWith(
      '/media/media1/complete',
      {}
    )
  })

  it('gets media info', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockMediaFile)

    const result = await mediaService.getMediaInfo('media1')

    expect(result).toEqual(mockMediaFile)
    expect(getSpy).toHaveBeenCalledWith('/media/media1')
  })

  it('gets media URL with default expiration', async () => {
    const urlResponse = {
      url: 'https://example.com/media/media1?token=xyz',
      expiresAt: '2024-01-01T00:00:00Z',
    }
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(urlResponse)

    const result = await mediaService.getMediaUrl('media1')

    expect(result.url).toBeTruthy()
    expect(getSpy).toHaveBeenCalledWith('/media/media1/url?expiresIn=1440')
  })

  it('gets media URL with custom expiration', async () => {
    const urlResponse = {
      url: 'https://example.com/media/media1?token=xyz',
      expiresAt: '2024-01-01T00:00:00Z',
    }
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(urlResponse)

    const result = await mediaService.getMediaUrl('media1', 3600)

    expect(result.url).toBeTruthy()
    expect(getSpy).toHaveBeenCalledWith('/media/media1/url?expiresIn=3600')
  })

  it('deletes media', async () => {
    const deleteSpy = jest.spyOn(apiClient.apiClient, 'delete').mockResolvedValue(undefined)

    await expect(
      mediaService.deleteMedia('media1')
    ).resolves.toBeUndefined()

    expect(deleteSpy).toHaveBeenCalledWith('/media/media1')
  })

  it('handles image file type', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue({ mediaFileId: 'media1' })

    await mediaService.initializeUpload({
      fileName: 'image.png',
      fileType: 'image/png',
      fileSize: 5120,
      totalChunks: 1,
    })

    expect(postSpy).toHaveBeenCalledWith(
      '/media/upload/initialize',
      expect.objectContaining({
        fileType: 'image/png',
      })
    )
  })

  it('handles video file type', async () => {
    const postSpy = jest.spyOn(apiClient.apiClient, 'post').mockResolvedValue({ mediaFileId: 'media1' })

    await mediaService.initializeUpload({
      fileName: 'video.mp4',
      fileType: 'video/mp4',
      fileSize: 1024000,
      totalChunks: 10,
    })

    expect(postSpy).toHaveBeenCalledWith(
      '/media/upload/initialize',
      expect.objectContaining({
        fileType: 'video/mp4',
        totalChunks: 10,
      })
    )
  })

  it('handles multiple chunks upload', async () => {
    const uploadSpy = jest.spyOn(apiClient.apiClient, 'upload').mockResolvedValue({ uploaded: true })

    const chunk = new Blob(['chunk data'])

    await mediaService.uploadChunk('media1', 0, chunk as File)
    await mediaService.uploadChunk('media1', 1, chunk as File)
    await mediaService.uploadChunk('media1', 2, chunk as File)

    expect(uploadSpy).toHaveBeenCalledTimes(3)
    expect(uploadSpy).toHaveBeenNthCalledWith(1, '/media/media1/chunks/0', expect.any(Object), undefined)
    expect(uploadSpy).toHaveBeenNthCalledWith(2, '/media/media1/chunks/1', expect.any(Object), undefined)
    expect(uploadSpy).toHaveBeenNthCalledWith(3, '/media/media1/chunks/2', expect.any(Object), undefined)
  })

  it('media file has required properties', async () => {
    const getSpy = jest.spyOn(apiClient.apiClient, 'get').mockResolvedValue(mockMediaFile)

    const result = await mediaService.getMediaInfo('media1')

    expect(result.id).toBeTruthy()
    expect(result.fileName).toBeTruthy()
    expect(result.fileType).toBeTruthy()
    expect(result.fileSize).toBeGreaterThan(0)
    expect(result.status).toBeTruthy()
  })
})
