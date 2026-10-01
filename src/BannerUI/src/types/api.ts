export interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
  errors?: Record<string, string[]>
}

export interface ListResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface MediaUploadRequest {
  fileName: string
  fileType: string
  fileSize: number
  totalChunks: number
}

export interface MediaChunkRequest {
  chunkNumber: number
  chunkSize: number
  checksum: string
}

export interface MediaCompleteRequest {
  mediaFileId: string
}

export interface EffectRequest {
  type: string
  parameters: Record<string, unknown>
}

export interface VersionRestoreRequest {
  versionNumber: number
}

export interface ErrorResponse {
  statusCode: number
  message: string
  details?: Record<string, unknown>
}
