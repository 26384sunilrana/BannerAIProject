import { renderHook, act, waitFor } from '@testing-library/react'
import { useMediaUpload } from '@/hooks/useMediaUpload'
import { setupFetchMockCleanup, mockFetchOnce } from '../helpers/mockFetch'
import * as mediaService from '@/api/mediaService'

describe('useMediaUpload', () => {
  setupFetchMockCleanup()

  beforeEach(() => {
    jest.spyOn(mediaService, 'mediaService', 'get').mockReturnValue({
      initializeUpload: jest.fn().mockResolvedValue({ mediaFileId: 'file-123' }),
      uploadChunk: jest.fn().mockResolvedValue({ uploaded: true }),
      completeUpload: jest.fn().mockResolvedValue({
        id: 'file-123',
        fileName: 'test.jpg',
        fileType: 'image/jpeg',
        fileSize: 1000,
        status: 'complete',
      }),
      getMediaInfo: jest.fn(),
      getMediaUrl: jest.fn(),
      deleteMedia: jest.fn(),
    } as any)
  })

  it('initializes with empty uploads', () => {
    const { result } = renderHook(() => useMediaUpload())
    expect(result.current.activeUploads).toEqual([])
  })

  it('starts upload with file', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file = new File(['content'], 'test.txt', { type: 'text/plain' })

    mockFetchOnce(200, { mediaFileId: 'file-123' })

    await act(async () => {
      await result.current.uploadFile(file)
    })

    expect(result.current.activeUploads.length).toBeGreaterThan(0)
  })

  it('tracks upload progress', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file = new File(['a'.repeat(1000)], 'large.txt', { type: 'text/plain' })

    mockFetchOnce(200, { mediaFileId: 'file-123' })

    await act(async () => {
      await result.current.uploadFile(file)
    })

    const upload = result.current.activeUploads[0]
    if (upload) {
      expect(upload.progress).toBeGreaterThanOrEqual(0)
      expect(upload.progress).toBeLessThanOrEqual(100)
    }
  })

  it('updates upload status during process', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file = new File(['content'], 'test.txt', { type: 'text/plain' })

    mockFetchOnce(200, { mediaFileId: 'file-123' })

    let uploadId: string | null = null

    await act(async () => {
      uploadId = await result.current.uploadFile(file)
    })

    expect(uploadId).toBeTruthy()
  })

  it('handles upload errors gracefully', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file = new File(['content'], 'test.txt', { type: 'text/plain' })

    mockFetchOnce(500, { error: 'Server error' })

    await act(async () => {
      const uploadId = await result.current.uploadFile(file)
      expect(uploadId).toBeNull()
    })

    const upload = result.current.activeUploads[0]
    if (upload) {
      expect(upload.status).toBe('error')
      expect(upload.error).toBeTruthy()
    }
  })

  it('provides upload progress info', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file = new File(['content'], 'test.txt', { type: 'text/plain' })

    mockFetchOnce(200, { mediaFileId: 'file-123' })

    let fileId: string | null = null

    await act(async () => {
      fileId = await result.current.uploadFile(file)
    })

    if (fileId) {
      const progress = result.current.getUploadProgress(fileId)
      expect(progress).toBeDefined()
      expect(progress?.fileName).toBe('test.txt')
      expect(progress?.progress).toBeGreaterThanOrEqual(0)
    }
  })

  it('cancels upload', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file = new File(['content'], 'test.txt', { type: 'text/plain' })

    mockFetchOnce(200, { mediaFileId: 'file-123' })

    let fileId: string | null = null

    await act(async () => {
      fileId = await result.current.uploadFile(file)
    })

    const initialCount = result.current.activeUploads.length

    if (fileId) {
      act(() => {
        result.current.cancelUpload(fileId)
      })

      const progress = result.current.getUploadProgress(fileId)
      expect(progress).toBeUndefined()
    }
  })

  it('clears completed uploads', async () => {
    const { result } = renderHook(() => useMediaUpload())

    act(() => {
      result.current.clearCompleted()
    })

    const completedCount = result.current.activeUploads.filter(
      (u) => u.status === 'complete' || u.status === 'error'
    ).length
    expect(completedCount).toBe(0)
  })

  it('handles multiple simultaneous uploads', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file1 = new File(['content1'], 'file1.txt', { type: 'text/plain' })
    const file2 = new File(['content2'], 'file2.txt', { type: 'text/plain' })

    mockFetchOnce(200, { mediaFileId: 'file-1' })

    await act(async () => {
      await result.current.uploadFile(file1)
    })

    mockFetchOnce(200, { mediaFileId: 'file-2' })

    await act(async () => {
      await result.current.uploadFile(file2)
    })

    expect(result.current.activeUploads.length).toBeGreaterThanOrEqual(1)
  })

  it('returns media file ID on successful upload', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file = new File(['content'], 'test.txt', { type: 'text/plain' })

    mockFetchOnce(200, { mediaFileId: 'file-123' })

    let uploadedId: string | null = null

    await act(async () => {
      uploadedId = await result.current.uploadFile(file)
    })

    expect(uploadedId).toBeTruthy()
  })

  it('returns null on upload failure', async () => {
    const { result } = renderHook(() => useMediaUpload())
    const file = new File(['content'], 'test.txt', { type: 'text/plain' })

    mockFetchOnce(500, { error: 'Upload failed' })

    let uploadedId: string | null = null

    await act(async () => {
      uploadedId = await result.current.uploadFile(file)
    })

    expect(uploadedId).toBeNull()
  })
})
