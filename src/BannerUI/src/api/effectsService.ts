import { apiClient } from './client'
import { Effect } from '@/types/banner'
import { EffectRequest } from '@/types/api'

export const effectsService = {
  async applyEffect(
    bannerId: string,
    componentId: string,
    request: EffectRequest
  ): Promise<Effect> {
    return apiClient.post<Effect>(
      `/banners/${bannerId}/components/${componentId}/effects`,
      request
    )
  },

  async updateEffect(
    bannerId: string,
    componentId: string,
    effectId: string,
    request: Partial<EffectRequest>
  ): Promise<Effect> {
    return apiClient.put<Effect>(
      `/banners/${bannerId}/components/${componentId}/effects/${effectId}`,
      request
    )
  },

  async removeEffect(
    bannerId: string,
    componentId: string,
    effectId: string
  ): Promise<void> {
    await apiClient.delete(
      `/banners/${bannerId}/components/${componentId}/effects/${effectId}`
    )
  },

  async getComponentEffects(
    bannerId: string,
    componentId: string
  ): Promise<Effect[]> {
    return apiClient.get<Effect[]>(
      `/banners/${bannerId}/components/${componentId}/effects`
    )
  },
}
