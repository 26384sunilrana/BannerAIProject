import axios, { AxiosInstance, AxiosError, AxiosResponse } from 'axios'
import { ErrorResponse } from '@/types/api'
import { clearSession, getAccessToken, getRefreshToken, saveTokens } from '@/lib/session'

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

/** Scheme and host of the API, for links the API returns as paths. */
export const API_ORIGIN = (() => {
  try {
    return new URL(API_URL).origin
  } catch {
    return ''
  }
})()

const ENVELOPE_KEYS = new Set(['success', 'data', 'message', 'errors', 'error', 'timestamp'])

/**
 * Some endpoints answer { success, data }, others return the value itself.
 * Only an object that looks like the envelope is unwrapped.
 */
function unwrap<T>(body: unknown): T {
  if (body && typeof body === 'object' && !Array.isArray(body)) {
    const keys = Object.keys(body)
    if (keys.includes('data') && keys.every((k) => ENVELOPE_KEYS.has(k))) {
      return (body as { data: T }).data
    }
  }
  return body as T
}

/** Best message to show a person for a failed request. */
export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  const response = (error as AxiosError<any>)?.response
  const body = response?.data

  if (body) {
    if (typeof body === 'string') return body
    if (typeof body.message === 'string') return body.message
    if (typeof body.error?.message === 'string') return body.error.message
    if (typeof body.error === 'string') return body.error
    if (body.errors && typeof body.errors === 'object') {
      const first = Object.values(body.errors).flat()[0]
      if (typeof first === 'string') return first
    }
    if (typeof body.title === 'string') return body.title
  }

  if (response?.status === 403) return 'You do not have permission to do that.'
  if (response?.status === 401) return 'Your session has expired. Please sign in again.'
  if ((error as AxiosError)?.code === 'ERR_NETWORK') return 'Cannot reach the server. Check your connection.'
  return fallback
}

const AUTH_PATH = '/authentication/'

class ApiClient {
  private client: AxiosInstance
  private refreshing: Promise<boolean> | null = null

  constructor() {
    this.client = axios.create({
      baseURL: API_URL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    })

    this.client.interceptors.request.use(
      (config) => {
        const token = this.getAuthToken()
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        return config
      },
      (error) => Promise.reject(error)
    )

    this.client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const response = error.response as AxiosResponse<ErrorResponse> | undefined
        const original = error.config as (typeof error.config & { _retried?: boolean }) | undefined

        if (response?.status === 401 && original && !original._retried && !original.url?.includes(AUTH_PATH)) {
          original._retried = true
          if (await this.tryRefresh()) {
            return this.client.request(original)
          }
          this.signOut()
        }
        return Promise.reject(error)
      }
    )
  }

  private getAuthToken(): string | null {
    return getAccessToken()
  }

  /** One refresh at a time, shared by every request that failed with 401 meanwhile. */
  private tryRefresh(): Promise<boolean> {
    const refreshToken = getRefreshToken()
    if (!refreshToken) return Promise.resolve(false)

    if (!this.refreshing) {
      this.refreshing = axios
        .post(`${API_URL}${AUTH_PATH}refresh-token`, { refreshToken })
        .then((res) => {
          const tokens = res.data?.tokens
          if (!tokens?.accessToken || !tokens?.refreshToken) return false
          saveTokens({ accessToken: tokens.accessToken, refreshToken: tokens.refreshToken })
          return true
        })
        .catch(() => false)
        .finally(() => {
          this.refreshing = null
        })
    }
    return this.refreshing
  }

  private signOut(): void {
    clearSession()
    if (typeof window !== 'undefined' && !window.location.pathname.startsWith('/login')) {
      const returnUrl = encodeURIComponent(window.location.pathname + window.location.search)
      window.location.href = `/login?returnUrl=${returnUrl}`
    }
  }

  async get<T>(path: string, config?: any): Promise<T> {
    const response = await this.client.get(path, config)
    return unwrap<T>(response.data)
  }

  async post<T>(path: string, data?: any, config?: any): Promise<T> {
    const response = await this.client.post(path, data, config)
    return unwrap<T>(response.data)
  }

  async put<T>(path: string, data?: any, config?: any): Promise<T> {
    const response = await this.client.put(path, data, config)
    return unwrap<T>(response.data)
  }

  async delete<T>(path: string, config?: any): Promise<T> {
    const response = await this.client.delete(path, config)
    return unwrap<T>(response.data)
  }

  /** Sends a file or piece of one as the raw request body (not a form). */
  async putBinary(
    path: string,
    body: Blob,
    options: { headers?: Record<string, string>; onProgress?: (loaded: number) => void } = {}
  ): Promise<void> {
    await this.client.put(path, body, {
      headers: { ...(options.headers ?? {}), 'Content-Type': 'application/octet-stream' },
      timeout: 5 * 60 * 1000,
      onUploadProgress: (event: { loaded: number }) => options.onProgress?.(event.loaded),
    })
  }

  async upload<T>(
    path: string,
    file: File,
    onProgress?: (progress: number) => void,
    config?: any
  ): Promise<T> {
    const formData = new FormData()
    formData.append('file', file)

    const response = await this.client.post(path, formData, {
      ...config,
      headers: {
        ...(config?.headers ?? {}),
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent: { loaded: number; total?: number }) => {
        if (onProgress && progressEvent.total) {
          const progress = Math.round((progressEvent.loaded / progressEvent.total) * 100)
          onProgress(progress)
        }
      },
    })

    return unwrap<T>(response.data)
  }
}

export const apiClient = new ApiClient()
