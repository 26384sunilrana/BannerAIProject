import { apiClient } from './client'
import { Banner, BannerComponent } from '@/types/banner'
import { BannerRequest, BannerUpdateRequest, ComponentRequest, ComponentUpdateRequest } from '@/types/api'

export const bannerService = {
  async getBanner(bannerId: string): Promise<Banner> {
    return apiClient.get<Banner>(`/banners/${bannerId}`)
  },

  async updateBanner(bannerId: string, request: BannerUpdateRequest): Promise<Banner> {
    return apiClient.put<Banner>(`/banners/${bannerId}`, request)
  },

  async addComponent(bannerId: string, request: ComponentRequest): Promise<BannerComponent> {
    return apiClient.post<BannerComponent>(`/banners/${bannerId}/components`, request)
  },

  async updateComponent(
    bannerId: string,
    componentId: string,
    request: ComponentUpdateRequest
  ): Promise<BannerComponent> {
    return apiClient.put<BannerComponent>(
      `/banners/${bannerId}/components/${componentId}`,
      request
    )
  },

  async deleteComponent(bannerId: string, componentId: string): Promise<void> {
    await apiClient.delete(`/banners/${bannerId}/components/${componentId}`)
  },

  async swapComponent(
    bannerId: string,
    componentId: string,
    newComponentId: string
  ): Promise<BannerComponent> {
    return apiClient.post<BannerComponent>(
      `/banners/${bannerId}/components/${componentId}/swap`,
      { newComponentId }
    )
  },
}
