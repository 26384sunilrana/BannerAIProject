import { apiClient, API_ORIGIN } from './client'

/** What the API reports about a stored file. FileType: 1 image, 2 video, 3 graphics. Status: 1 pending, 2 active, 3 failed. */
export interface MediaFile {
  id: string
  fileName: string
  fileType: number
  contentType: string
  sizeBytes: number
  status: number
  width?: number | null
  height?: number | null
  /** Video length in seconds, read from the file when the upload completed. */
  durationSeconds?: number | null
}

/** One file in the library. fileType: 1 image, 2 video. */
export interface MediaLibraryItem {
  id: string
  fileName: string
  fileType: number
  contentType: string
  sizeBytes: number
  width: number | null
  height: number | null
  durationSeconds: number | null
  createdAt: string
  /** A link the browser can load right now (it expires). */
  url: string
  /** A banner (or an earlier version of one) still uses the file, so it cannot be deleted. */
  inUse: boolean
}

export interface MediaLibraryPage {
  items: MediaLibraryItem[]
  total: number
  page: number
  pageSize: number
}

export interface MediaUsage {
  usedBytes: number
  fileCount: number
  imageCount: number
  videoCount: number
  /** What the plan allows, or null when the shop has no plan. */
  limitBytes: number | null
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

  async list(options: { type?: 'image' | 'video'; search?: string; page?: number; pageSize?: number } = {}): Promise<MediaLibraryPage> {
    const params = new URLSearchParams()
    if (options.type) params.set('type', options.type)
    if (options.search?.trim()) params.set('search', options.search.trim())
    if (options.page) params.set('page', String(options.page))
    if (options.pageSize) params.set('pageSize', String(options.pageSize))
    const text = params.toString()
    const page = await apiClient.get<MediaLibraryPage>(`/media${text ? `?${text}` : ''}`)
    return { ...page, items: page.items.map((item) => ({ ...item, url: absoluteMediaUrl(item.url) })) }
  },

  usage(): Promise<MediaUsage> {
    return apiClient.get<MediaUsage>('/media/usage')
  },

  remove(mediaFileId: string): Promise<{ message: string }> {
    return apiClient.delete<{ message: string }>(`/media/${mediaFileId}`)
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
