'use client'

import { useCallback, useState } from 'react'
import { mediaService } from '@/api/mediaService'
import crypto from 'crypto'

const CHUNK_SIZE = 10 * 1024 * 1024 // 10MB chunks

export interface UploadProgress {
  fileName: string
  fileSize: number
  uploadedBytes: number
  totalChunks: number
  uploadedChunks: number
  progress: number
  status: 'idle' | 'uploading' | 'completing' | 'complete' | 'error'
  error: string | null
}

export function useMediaUpload() {
  const [uploads, setUploads] = useState<Map<string, UploadProgress>>(new Map())

  const calculateMD5 = useCallback(async (chunk: Blob): Promise<string> => {
    const buffer = await chunk.arrayBuffer()
    const hashBuffer = await crypto.subtle.digest('SHA-256', buffer)
    return Array.from(new Uint8Array(hashBuffer))
      .map((b) => b.toString(16).padStart(2, '0'))
      .join('')
  }, [])

  const uploadFile = useCallback(
    async (file: File): Promise<string | null> => {
      const fileId = `${file.name}-${Date.now()}`
      const totalChunks = Math.ceil(file.size / CHUNK_SIZE)

      try {
        // Initialize upload
        setUploads((prev) => {
          const newMap = new Map(prev)
          newMap.set(fileId, {
            fileName: file.name,
            fileSize: file.size,
            uploadedBytes: 0,
            totalChunks,
            uploadedChunks: 0,
            progress: 0,
            status: 'uploading',
            error: null,
          })
          return newMap
        })

        const initResponse = await mediaService.initializeUpload({
          fileName: file.name,
          fileType: file.type,
          fileSize: file.size,
          totalChunks,
        })

        const mediaFileId = initResponse.mediaFileId

        // Upload chunks
        for (let i = 0; i < totalChunks; i++) {
          const start = i * CHUNK_SIZE
          const end = Math.min(start + CHUNK_SIZE, file.size)
          const chunk = file.slice(start, end)

          const checksum = await calculateMD5(chunk)

          await mediaService.uploadChunk(mediaFileId, i, chunk, (progress) => {
            const uploadedBytes = start + (progress / 100) * (end - start)
            const overallProgress = (uploadedBytes / file.size) * 100

            setUploads((prev) => {
              const newMap = new Map(prev)
              const current = newMap.get(fileId)
              if (current) {
                newMap.set(fileId, {
                  ...current,
                  uploadedBytes,
                  uploadedChunks: i,
                  progress: overallProgress,
                })
              }
              return newMap
            })
          })
        }

        // Complete upload
        setUploads((prev) => {
          const newMap = new Map(prev)
          const current = newMap.get(fileId)
          if (current) {
            newMap.set(fileId, {
              ...current,
              status: 'completing',
            })
          }
          return newMap
        })

        await mediaService.completeUpload(mediaFileId)

        setUploads((prev) => {
          const newMap = new Map(prev)
          const current = newMap.get(fileId)
          if (current) {
            newMap.set(fileId, {
              ...current,
              status: 'complete',
              uploadedChunks: totalChunks,
              progress: 100,
              uploadedBytes: file.size,
            })
          }
          return newMap
        })

        return mediaFileId
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Upload failed'
        setUploads((prev) => {
          const newMap = new Map(prev)
          const current = newMap.get(fileId)
          if (current) {
            newMap.set(fileId, {
              ...current,
              status: 'error',
              error: errorMessage,
            })
          }
          return newMap
        })
        return null
      }
    },
    [calculateMD5]
  )

  const getUploadProgress = useCallback((fileId: string): UploadProgress | undefined => {
    return uploads.get(fileId)
  }, [uploads])

  const cancelUpload = useCallback((fileId: string) => {
    setUploads((prev) => {
      const newMap = new Map(prev)
      newMap.delete(fileId)
      return newMap
    })
  }, [])

  const clearCompleted = useCallback(() => {
    setUploads((prev) => {
      const newMap = new Map(prev)
      Array.from(newMap.entries()).forEach(([key, value]) => {
        if (value.status === 'complete' || value.status === 'error') {
          newMap.delete(key)
        }
      })
      return newMap
    })
  }, [])

  return {
    uploadFile,
    getUploadProgress,
    cancelUpload,
    clearCompleted,
    activeUploads: Array.from(uploads.values()),
  }
}
