import { act, renderHook } from '@testing-library/react'
import { checkUploadFile, MAX_UPLOAD_BYTES, useMediaUpload } from '@/hooks/useMediaUpload'
import { mediaService } from '@/api/mediaService'

jest.mock('@/api/mediaService', () => ({
  mediaService: {
    initializeUpload: jest.fn(),
    uploadChunk: jest.fn(),
    completeUpload: jest.fn(),
    getMediaUrl: jest.fn(),
  },
}))

const mocked = mediaService as jest.Mocked<typeof mediaService>

// 20 bytes cut into pieces of 8
const file = (type = 'image/png') => new File([new Uint8Array(20).fill(1)], 'logo.png', { type })

beforeEach(() => {
  jest.clearAllMocks()
  mocked.initializeUpload.mockResolvedValue({
    mediaFileId: 'm1',
    fileName: 'logo.png',
    totalSizeBytes: 20,
    chunkSizeBytes: 8,
    totalChunks: 3,
  })
  mocked.uploadChunk.mockResolvedValue(undefined)
  mocked.completeUpload.mockResolvedValue({} as never)
  mocked.getMediaUrl.mockResolvedValue({ mediaFileId: 'm1', url: 'http://localhost:5000/api/media/m1/download', expiresAt: null })
})

describe('checkUploadFile', () => {
  it('accepts supported images and videos', () => {
    expect(checkUploadFile({ type: 'image/webp', size: 10 }, 'image')).toBeNull()
    expect(checkUploadFile({ type: 'video/mp4', size: 10 }, 'video')).toBeNull()
  })

  it('refuses other types, wrong kinds, empty and oversize files', () => {
    expect(checkUploadFile({ type: 'image/svg+xml', size: 10 }, 'image')).toMatch(/PNG, JPEG/)
    expect(checkUploadFile({ type: 'video/mp4', size: 10 }, 'image')).toMatch(/image/)
    expect(checkUploadFile({ type: 'image/png', size: 10 }, 'video')).toMatch(/video/)
    expect(checkUploadFile({ type: 'image/png', size: 0 }, 'image')).toMatch(/empty/)
    expect(checkUploadFile({ type: 'image/png', size: MAX_UPLOAD_BYTES + 1 }, 'image')).toMatch(/500 MB/)
  })
})

describe('useMediaUpload', () => {
  it('cuts the file to the size the server asked for and sends each piece with its checksum', async () => {
    const { result } = renderHook(() => useMediaUpload())

    let uploaded: Awaited<ReturnType<typeof result.current.upload>> = null
    await act(async () => {
      uploaded = await result.current.upload(file(), 'image')
    })

    expect(mocked.initializeUpload).toHaveBeenCalledWith({ fileName: 'logo.png', contentType: 'image/png', totalSizeBytes: 20, fileType: 1 })
    expect(mocked.uploadChunk.mock.calls.map((c) => [c[1], (c[2] as Blob).size])).toEqual([[0, 8], [1, 8], [2, 4]])
    expect(mocked.uploadChunk.mock.calls.every((c) => /^[0-9a-f]{32}$/.test(c[3] as string))).toBe(true)
    expect(mocked.completeUpload).toHaveBeenCalledWith('m1')
    expect(uploaded).toEqual({ mediaFileId: 'm1', url: 'http://localhost:5000/api/media/m1/download' })
    expect(result.current.progress).toBe(100)
    expect(result.current.isUploading).toBe(false)
  })

  it('uses the video file type for videos', async () => {
    const { result } = renderHook(() => useMediaUpload())

    await act(async () => {
      await result.current.upload(file('video/mp4'), 'video')
    })

    expect(mocked.initializeUpload).toHaveBeenCalledWith(expect.objectContaining({ contentType: 'video/mp4', fileType: 2 }))
  })

  it('retries a piece after a server error, then continues', async () => {
    mocked.uploadChunk.mockRejectedValueOnce({ response: { status: 503 } }).mockResolvedValue(undefined)
    const { result } = renderHook(() => useMediaUpload())

    await act(async () => {
      await result.current.upload(file(), 'image')
    })

    expect(mocked.uploadChunk).toHaveBeenCalledTimes(4) // 3 pieces + 1 retry
    expect(result.current.error).toBeNull()
  })

  it('stops at once on a client error and shows the server message', async () => {
    mocked.uploadChunk.mockRejectedValue({ response: { status: 400, data: { error: { message: 'Checksum mismatch' } } } })
    const { result } = renderHook(() => useMediaUpload())

    let uploaded: unknown = 'unset'
    await act(async () => {
      uploaded = await result.current.upload(file(), 'image')
    })

    expect(uploaded).toBeNull()
    expect(mocked.uploadChunk).toHaveBeenCalledTimes(1)
    expect(mocked.completeUpload).not.toHaveBeenCalled()
    expect(result.current.error).toBe('Checksum mismatch')
  })

  it('does not contact the server for an unsupported file', async () => {
    const { result } = renderHook(() => useMediaUpload())

    await act(async () => {
      await result.current.upload(file('image/svg+xml'), 'image')
    })

    expect(mocked.initializeUpload).not.toHaveBeenCalled()
    expect(result.current.error).toMatch(/PNG, JPEG/)
  })
})
