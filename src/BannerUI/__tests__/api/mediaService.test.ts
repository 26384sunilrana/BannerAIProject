import { absoluteMediaUrl, mediaService } from '@/api/mediaService'
import * as client from '@/api/client'

describe('mediaService', () => {
  afterEach(() => jest.restoreAllMocks())

  it('starts an upload with the fields the API expects', async () => {
    const post = jest.spyOn(client.apiClient, 'post').mockResolvedValue({ mediaFileId: 'm1' })

    await mediaService.initializeUpload({ fileName: 'a.png', contentType: 'image/png', totalSizeBytes: 10, fileType: 1 })

    expect(post).toHaveBeenCalledWith('/media/upload/initialize', {
      fileName: 'a.png',
      contentType: 'image/png',
      totalSizeBytes: 10,
      fileType: 1,
    })
  })

  it('sends a chunk as the raw body with its checksum', async () => {
    const put = jest.spyOn(client.apiClient, 'putBinary').mockResolvedValue(undefined)
    const chunk = new Blob(['data'])
    const onProgress = jest.fn()

    await mediaService.uploadChunk('m1', 3, chunk, 'abc', onProgress)

    expect(put).toHaveBeenCalledWith('/media/m1/chunks/3', chunk, {
      headers: { 'X-Checksum-MD5': 'abc' },
      onProgress,
    })
  })

  it('completes an upload', async () => {
    const post = jest.spyOn(client.apiClient, 'post').mockResolvedValue({ id: 'm1', status: 2 })

    await mediaService.completeUpload('m1')

    expect(post).toHaveBeenCalledWith('/media/m1/complete', {})
  })

  it('asks for a link by the API parameter name and makes it absolute', async () => {
    const get = jest.spyOn(client.apiClient, 'get').mockResolvedValue({
      mediaFileId: 'm1',
      url: '/api/media/m1/download?expires=1&sig=x',
      expiresAt: null,
    })

    const link = await mediaService.getMediaUrl('m1', 60)

    expect(get).toHaveBeenCalledWith('/media/m1/url?expirationMinutes=60')
    expect(link.url).toBe(`${client.API_ORIGIN}/api/media/m1/download?expires=1&sig=x`)
  })

  it('leaves full addresses alone', () => {
    expect(absoluteMediaUrl('https://cdn.example.com/a.png')).toBe('https://cdn.example.com/a.png')
  })
})
