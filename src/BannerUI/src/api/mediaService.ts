import { apiClient, API_ORIGIN } from './client'

/** What the API reports about a stored file. FileType: 1 image, 2 video, 3 graphics. Status: 1 pending, 2 active, 3 failed. */
export interface MediaFile {
  id: string
  fileName: string
  fileType: number
  contentType: string
  sizeBytes: number
  status: number
}

export interface UploadSession {
  mediaFileId: string
  fileName: string
  totalSizeBytes: number
  /** The server decides the piece size; the browser cuts the file to match. */
  chunkSizeBytes: number
  totalChunks: number
}

export interface MediaLink {
  mediaFileId: string
  url: string
  expiresAt: string | null
}

export const mediaService = {
  initializeUpload(request: {
    fileName: string
    contentType: string
    totalSizeBytes: number
    fileType: number
  }): Promise<UploadSession> {
    return apiClient.post<UploadSession>('/media/upload/initialize', request)
  },

  /** Sends one piece as the raw request body, with its MD5 so the server can check it arrived intact. */
  async uploadChunk(
    mediaFileId: string,
    chunkNumber: number,
    chunk: Blob,
    checksumMD5: string,
    onProgress?: (loaded: number) => void
  ): Promise<void> {
    await apiClient.putBinary(`/media/${mediaFileId}/chunks/${chunkNumber}`, chunk, {
      headers: { 'X-Checksum-MD5': checksumMD5 },
      onProgress,
    })
  },

  completeUpload(mediaFileId: string): Promise<MediaFile> {
    return apiClient.post<MediaFile>(`/media/${mediaFileId}/complete`, {})
  },

  getMediaInfo(mediaFileId: string): Promise<MediaFile> {
    return apiClient.get<MediaFile>(`/media/${mediaFileId}`)
  },

  /** A link a browser can load directly (image and video tags cannot send a sign-in header). */
  async getMediaUrl(mediaFileId: string, expirationMinutes = 240): Promise<MediaLink> {
    const link = await apiClient.get<MediaLink>(`/media/${mediaFileId}/url?expirationMinutes=${expirationMinutes}`)
    return { ...link, url: absoluteMediaUrl(link.url) }
  },
}

/** The API returns links starting at /api; the page and the API may be on different addresses. */
export function absoluteMediaUrl(url: string): string {
  return /^https?:\/\//i.test(url) ? url : `${API_ORIGIN}${url.startsWith('/') ? '' : '/'}${url}`
}
