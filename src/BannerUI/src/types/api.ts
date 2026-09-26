import { Banner, BannerComponent, BannerVersion } from './banner'

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

export interface BannerRequest {
  title: string
  description: string
  width: number
  height: number
  backgroundColor: string
}

export interface BannerUpdateRequest {
  title?: string
  description?: string
  width?: number
  height?: number
  backgroundColor?: string
}

export interface ComponentRequest {
  type: string
  x: number
  y: number
  width: number
  height: number
  zIndex: number
  data: Record<string, unknown>
}

export interface ComponentUpdateRequest {
  x?: number
  y?: number
  width?: number
  height?: number
  zIndex?: number
  rotation?: number
  opacity?: number
  isVisible?: boolean
  data?: Record<string, unknown>
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

export interface LayerReorderRequest {
  componentId: string
  newZIndex: number
}

export interface VersionRestoreRequest {
  versionNumber: number
}

export interface ErrorResponse {
  statusCode: number
  message: string
  details?: Record<string, unknown>
}
