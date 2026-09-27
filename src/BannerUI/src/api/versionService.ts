import { apiClient } from './client'
import { BannerVersion } from '@/types/banner'

export const versionService = {
  async listVersions(bannerId: string): Promise<BannerVersion[]> {
    return apiClient.get<BannerVersion[]>(`/banners/${bannerId}/versions`)
  },

  async getVersion(bannerId: string, versionNumber: number): Promise<BannerVersion> {
    return apiClient.get<BannerVersion>(
      `/banners/${bannerId}/versions/${versionNumber}`
    )
  },

  async createSnapshot(bannerId: string): Promise<BannerVersion> {
    return apiClient.post<BannerVersion>(`/banners/${bannerId}/versions`, {})
  },

  async restoreVersion(bannerId: string, versionNumber: number): Promise<BannerVersion> {
    return apiClient.post<BannerVersion>(
      `/banners/${bannerId}/versions/${versionNumber}/restore`,
      {}
    )
  },
}
