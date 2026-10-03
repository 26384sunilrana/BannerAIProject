'use client'

import { useCallback, useState } from 'react'
import SparkMD5 from 'spark-md5'
import { mediaService } from '@/api/mediaService'
import { getErrorMessage } from '@/api/client'

export const MAX_UPLOAD_BYTES = 500 * 1024 * 1024

const ALLOWED_TYPES: Record<'image' | 'video', string[]> = {
  image: ['image/png', 'image/jpeg', 'image/gif', 'image/webp'],
  video: ['video/mp4', 'video/webm'],
}

export type MediaKind = 'image' | 'video'

export interface UploadResult {
  mediaFileId: string
  /** A link the browser can load right now (it expires; the media file id is what gets saved). */
  url: string
  /** Video length in seconds when it could be read from the file. */
  durationSeconds?: number | null
}

/** Why a file cannot be uploaded, or null when it can. */
export function checkUploadFile(file: { type: string; size: number }, kind: MediaKind): string | null {
  if (!ALLOWED_TYPES[kind].includes(file.type)) {
    return kind === 'image'
      ? 'Choose a PNG, JPEG, GIF or WebP image.'
      : 'Choose an MP4 or WebM video.'
  }
  if (file.size <= 0) return 'The file is empty.'
  if (file.size > MAX_UPLOAD_BYTES) return 'The file is larger than 500 MB.'
  return null
}

// Blob.arrayBuffer is missing from some older browsers and from the test environment, so fall back to FileReader
const readBytes = (blob: Blob): Promise<ArrayBuffer> =>
  typeof blob.arrayBuffer === 'function'
    ? blob.arrayBuffer()
    : new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.onload = () => resolve(reader.result as ArrayBuffer)
        reader.onerror = () => reject(reader.error)
        reader.readAsArrayBuffer(blob)
      })

const md5 = async (chunk: Blob) => SparkMD5.ArrayBuffer.hash(await readBytes(chunk))

export function useMediaUpload() {
  const [isUploading, setIsUploading] = useState(false)
  const [progress, setProgress] = useState(0)
  const [error, setError] = useState<string | null>(null)

  const upload = useCallback(async (file: File, kind: MediaKind): Promise<UploadResult | null> => {
    const problem = checkUploadFile(file, kind)
    if (problem) {
      setError(problem)
      return null
    }

    setError(null)
    setProgress(0)
    setIsUploading(true)

    try {
      const session = await mediaService.initializeUpload({
        fileName: file.name.slice(0, 255),
        contentType: file.type,
        totalSizeBytes: file.size,
        fileType: kind === 'video' ? 2 : 1,
      })

      for (let index = 0; index < session.totalChunks; index++) {
        const start = index * session.chunkSizeBytes
        const chunk = file.slice(start, Math.min(start + session.chunkSizeBytes, file.size))
        const checksum = await md5(chunk)

        // a dropped connection is retried before the upload is given up
        for (let attempt = 1; ; attempt++) {
          try {
            await mediaService.uploadChunk(session.mediaFileId, index, chunk, checksum, (loaded) =>
              setProgress(Math.round(((start + loaded) / file.size) * 100))
            )
            break
          } catch (chunkError) {
            const status = (chunkError as { response?: { status: number } }).response?.status
            if (attempt >= 3 || (status !== undefined && status < 500)) throw chunkError
          }
        }
      }

      const stored = await mediaService.completeUpload(session.mediaFileId)
      const link = await mediaService.getMediaUrl(session.mediaFileId)
      setProgress(100)
      return { mediaFileId: session.mediaFileId, url: link.url, durationSeconds: stored?.durationSeconds ?? null }
    } catch (err) {
      setError(getErrorMessage(err, 'The upload failed.'))
      return null
    } finally {
      setIsUploading(false)
    }
  }, [])

  return { upload, isUploading, progress, error, clearError: () => setError(null) }
}
