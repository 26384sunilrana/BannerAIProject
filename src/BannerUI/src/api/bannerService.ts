import { apiClient } from './client'
import { Banner, BannerComponent, ComponentData, ComponentType } from '@/types/banner'
import { mediaService } from './mediaService'
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
    const banner = toBanner(summary, preview.components ?? [])
    await attachMediaLinks(banner)
    return banner
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

/** Images and videos that were uploaded are stored by id; give each one a link the browser can load now. */
async function attachMediaLinks(banner: Banner): Promise<void> {
  // one request per file, even when several components (or slides) use the same one
  const links = new Map<string, Promise<string>>()
  const linkFor = (mediaFileId: string): Promise<string> => {
    if (!links.has(mediaFileId)) {
      links.set(
        mediaFileId,
        mediaService
          .getMediaUrl(mediaFileId)
          .then((link) => link.url)
          // the file may still be uploading or was removed: it simply shows nothing
          .catch(() => '')
      )
    }
    return links.get(mediaFileId)!
  }

  const attach = async (entry: unknown) => {
    if (!entry || typeof entry !== 'object') return
    const record = entry as Record<string, unknown>
    if (typeof record.mediaFileId === 'string' && record.mediaFileId) record.mediaUrl = await linkFor(record.mediaFileId)
  }

  await Promise.all(
    banner.components.map(async (component) => {
      const data = component.data as unknown as Record<string, unknown>
      await attach(data)
      for (const key of ['slides', 'playlist']) {
        const list = data[key]
        if (Array.isArray(list)) await Promise.all(list.map(attach))
      }
    })
  )
}
