import { apiClient } from './client'
import { Banner, BannerComponent, ComponentData, ComponentType } from '@/types/banner'
import {
  ApiBannerSummary,
  ApiComponent,
  toApiBanner,
  toApiComponent,
  toBanner,
  toBannerComponent,
} from './bannerMapper'

export interface ComponentInput {
  type: ComponentType
  x: number
  y: number
  width: number
  height: number
  zIndex: number
  rotation?: number
  opacity?: number
  isVisible?: boolean
  data: ComponentData | Record<string, unknown>
}

export interface BannerChanges {
  title?: string
  description?: string
  width?: number
  height?: number
}

export const bannerService = {
  /** The banner and all of its components, in the editor's shape. */
  async getBanner(bannerId: string): Promise<Banner> {
    const [summary, preview] = await Promise.all([
      apiClient.get<ApiBannerSummary>(`/banners/${bannerId}`),
      apiClient.get<{ components: ApiComponent[] }>(`/banners/${bannerId}/preview`),
    ])
    return toBanner(summary, preview.components ?? [])
  },

  /** Changes the given fields; the rest keep their current values. */
  async updateBanner(bannerId: string, changes: BannerChanges): Promise<void> {
    const current = await apiClient.get<ApiBannerSummary>(`/banners/${bannerId}`)
    const request = toApiBanner({
      title: changes.title ?? current.name,
      description: changes.description ?? current.description,
      width: changes.width ?? current.width,
      height: changes.height ?? current.height,
    })
    await apiClient.put(`/banners/${bannerId}`, request)
  },

  async addComponent(bannerId: string, component: ComponentInput): Promise<BannerComponent> {
    const created = await apiClient.post<ApiComponent>(`/banners/${bannerId}/components`, toApiComponent(component))
    return toBannerComponent(created)
  },

  async updateComponent(bannerId: string, componentId: string, component: ComponentInput): Promise<BannerComponent> {
    const updated = await apiClient.put<ApiComponent>(
      `/banners/${bannerId}/components/${componentId}`,
      toApiComponent(component)
    )
    return toBannerComponent(updated)
  },

  async deleteComponent(bannerId: string, componentId: string): Promise<void> {
    await apiClient.delete(`/banners/${bannerId}/components/${componentId}`)
  },
}
