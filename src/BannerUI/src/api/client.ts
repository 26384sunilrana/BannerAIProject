import axios, { AxiosInstance, AxiosError, AxiosResponse } from 'axios'
import { ErrorResponse } from '@/types/api'
import { clearSession, getAccessToken, saveTokens } from '@/lib/session'

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

const onShopScreen = () => typeof window !== 'undefined' && window.location.pathname.startsWith('/display')

class ApiClient {
  private client: AxiosInstance
  private refreshing: Promise<boolean> | null = null

  constructor() {
    this.client = axios.create({
      baseURL: API_URL,
      timeout: 30000,
      // the refresh token is an HttpOnly cookie: it is sent only when the browser is told to
      withCredentials: true,
      headers: {
        'Content-Type': 'application/json',
        // proves the call comes from this web app and not from a form on another site (see the API's forgery protection)
        'X-Requested-With': 'XMLHttpRequest',
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
        const original = error.config as (typeof error.config & { _retried?: boolean; skipAuthRedirect?: boolean }) | undefined

        if (response?.status === 401 && original && !original._retried && !original.url?.includes(AUTH_PATH)) {
          original._retried = true
          if (await this.tryRefresh()) {
            return this.client.request(original)
          }
          // A shop screen keeps showing its own default board when the session ends, so it is not sent to sign in
          if (!original.skipAuthRedirect && !onShopScreen()) this.signOut()
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
    if (!this.refreshing) {
      this.refreshing = axios
        .post(
          `${API_URL}${AUTH_PATH}refresh-token`,
          {},
          { withCredentials: true, headers: { 'X-Requested-With': 'XMLHttpRequest' } }
        )
        .then((res) => {
          const accessToken = res.data?.tokens?.accessToken
          if (!accessToken) return false
          saveTokens({ accessToken })
          return true
        })
        .catch(() => false)
        .finally(() => {
          this.refreshing = null
        })
    }
    return this.refreshing
  }

  /** Gets a new access token with the refresh cookie; false when there is no valid session. */
  refreshSession(): Promise<boolean> {
    return this.tryRefresh()
  }

  /** Ends the session and sends the person to sign in (not on a shop screen). */
  endSession(): void {
    if (!onShopScreen()) this.signOut()
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

/**
 * fetch against the API with the signed-in person's token. A 401 triggers one refresh and one retry,
 * and a second 401 ends the session. Paths start with "/", like the ones given to apiClient.
 */
export async function authFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const send = () => {
    const token = getAccessToken()
    return fetch(`${API_URL}${path}`, {
      ...init,
      headers: {
        'Content-Type': 'application/json',
        ...((init.headers as Record<string, string> | undefined) ?? {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    })
  }

  let response = await send()
  if (response.status === 401) {
    if (await apiClient.refreshSession()) response = await send()
    else apiClient.endSession()
  }
  return response
}
