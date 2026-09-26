import { apiClient } from './client'
import { BannerComponent } from '@/types/banner'

export const layerService = {
  async reorderComponent(
    bannerId: string,
    componentId: string,
    newZIndex: number
  ): Promise<BannerComponent> {
    return apiClient.post<BannerComponent>(
      `/banners/${bannerId}/layers/reorder`,
      { componentId, newZIndex }
    )
  },

  async moveForward(bannerId: string, componentId: string): Promise<BannerComponent> {
    return apiClient.post<BannerComponent>(
      `/banners/${bannerId}/layers/${componentId}/move-forward`,
      {}
    )
  },

  async moveBackward(bannerId: string, componentId: string): Promise<BannerComponent> {
    return apiClient.post<BannerComponent>(
      `/banners/${bannerId}/layers/${componentId}/move-backward`,
      {}
    )
  },

  async sendToFront(bannerId: string, componentId: string): Promise<BannerComponent> {
    return apiClient.post<BannerComponent>(
      `/banners/${bannerId}/layers/${componentId}/send-to-front`,
      {}
    )
  },

  async sendToBack(bannerId: string, componentId: string): Promise<BannerComponent> {
    return apiClient.post<BannerComponent>(
      `/banners/${bannerId}/layers/${componentId}/send-to-back`,
      {}
    )
  },
}
