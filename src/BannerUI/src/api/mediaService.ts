import { apiClient } from './client'
import { MediaUploadRequest, MediaChunkRequest } from '@/types/api'

export interface MediaFile {
  id: string
  fileName: string
  fileType: string
  fileSize: number
  status: string
  url?: string
}

export const mediaService = {
  async initializeUpload(request: MediaUploadRequest): Promise<{ mediaFileId: string }> {
    return apiClient.post<{ mediaFileId: string }>('/media/upload/initialize', request)
  },

  async uploadChunk(
    mediaFileId: string,
    chunkNumber: number,
    chunkData: Blob,
    onProgress?: (progress: number) => void
  ): Promise<{ uploaded: boolean }> {
    return apiClient.upload<{ uploaded: boolean }>(
      `/media/${mediaFileId}/chunks/${chunkNumber}`,
      chunkData as unknown as File,
      onProgress
    )
  },

  async completeUpload(mediaFileId: string): Promise<MediaFile> {
    return apiClient.post<MediaFile>(`/media/${mediaFileId}/complete`, {})
  },

  async getMediaInfo(mediaFileId: string): Promise<MediaFile> {
    return apiClient.get<MediaFile>(`/media/${mediaFileId}`)
  },

  async getMediaUrl(
    mediaFileId: string,
    expiresInMinutes: number = 1440
  ): Promise<{ url: string; expiresAt: string }> {
    return apiClient.get<{ url: string; expiresAt: string }>(
      `/media/${mediaFileId}/url?expiresIn=${expiresInMinutes}`
    )
  },

  async deleteMedia(mediaFileId: string): Promise<void> {
    await apiClient.delete(`/media/${mediaFileId}`)
  },
}
