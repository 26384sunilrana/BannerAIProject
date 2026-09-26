import axios, { AxiosInstance, AxiosError, AxiosResponse } from 'axios'
import { ApiResponse, ErrorResponse } from '@/types/api'

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

class ApiClient {
  private client: AxiosInstance

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
      (error: AxiosError) => {
        const response = error.response as AxiosResponse<ErrorResponse>
        if (response?.status === 401) {
          this.clearAuthToken()
          window.location.href = '/login'
        }
        return Promise.reject(error)
      }
    )
  }

  private getAuthToken(): string | null {
    if (typeof window !== 'undefined') {
      return localStorage.getItem('auth_token')
    }
    return null
  }

  private clearAuthToken(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('auth_token')
    }
  }

  async get<T>(path: string, config?: any): Promise<T> {
    const response = await this.client.get<ApiResponse<T>>(path, config)
    return response.data.data
  }

  async post<T>(path: string, data?: any, config?: any): Promise<T> {
    const response = await this.client.post<ApiResponse<T>>(path, data, config)
    return response.data.data
  }

  async put<T>(path: string, data?: any, config?: any): Promise<T> {
    const response = await this.client.put<ApiResponse<T>>(path, data, config)
    return response.data.data
  }

  async delete<T>(path: string, config?: any): Promise<T> {
    const response = await this.client.delete<ApiResponse<T>>(path, config)
    return response.data.data
  }

  async upload<T>(
    path: string,
    file: File,
    onProgress?: (progress: number) => void,
    config?: any
  ): Promise<T> {
    const formData = new FormData()
    formData.append('file', file)

    const response = await this.client.post<ApiResponse<T>>(path, formData, {
      ...config,
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (onProgress && progressEvent.total) {
          const progress = Math.round((progressEvent.loaded / progressEvent.total) * 100)
          onProgress(progress)
        }
      },
    })

    return response.data.data
  }
}

export const apiClient = new ApiClient()
