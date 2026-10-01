import { apiClient } from './client'
import { BannerSummary, PublishWorkflow } from '@/types/workflow'

export const workflowService = {
  listShopWorkflows(shopId: string): Promise<PublishWorkflow[]> {
    return apiClient.get<PublishWorkflow[]>(`/publish-workflow/shop/${shopId}`)
  },

  getByBanner(bannerId: string): Promise<PublishWorkflow | null> {
    return apiClient.get<PublishWorkflow>(`/publish-workflow/banner/${bannerId}`).catch((error) => {
      if (error?.response?.status === 404) return null
      throw error
    })
  },

  initiate(bannerId: string): Promise<PublishWorkflow> {
    return apiClient.post<PublishWorkflow>('/publish-workflow/initiate', { bannerId })
  },

  submit(workflowId: string): Promise<PublishWorkflow> {
    return apiClient.post<PublishWorkflow>(`/publish-workflow/${workflowId}/submit`)
  },

  approve(workflowId: string, comment?: string): Promise<PublishWorkflow> {
    return apiClient.post<PublishWorkflow>(`/publish-workflow/${workflowId}/approve`, { comment })
  },

  reject(workflowId: string, reason: string): Promise<PublishWorkflow> {
    return apiClient.post<PublishWorkflow>(`/publish-workflow/${workflowId}/reject`, { reason })
  },

  publish(workflowId: string): Promise<PublishWorkflow> {
    return apiClient.post<PublishWorkflow>(`/publish-workflow/${workflowId}/publish`)
  },

  unpublish(workflowId: string): Promise<PublishWorkflow> {
    return apiClient.post<PublishWorkflow>(`/publish-workflow/${workflowId}/unpublish`)
  },
}

export interface ScheduleResult {
  bannerId: string
  startAt: string
  endAt: string
  /** True when the banner was live or approved: the change has to be approved again. */
  requiresReapproval: boolean
}

export interface ActiveBanner {
  banner: BannerSummary | null
  useDefaultBanner: boolean
}

export const bannerListService = {
  list(): Promise<BannerSummary[]> {
    return apiClient.get<BannerSummary[]>('/banners')
  },

  create(request: { name: string; description: string; width: number; height: number }): Promise<BannerSummary> {
    return apiClient.post<BannerSummary>('/banners', request)
  },

  /** Sets when the banner is shown. Overlapping another banner of the shop is refused (409). */
  setSchedule(bannerId: string, startAtIso: string, endAtIso: string): Promise<ScheduleResult> {
    return apiClient.put<ScheduleResult>(`/banners/${bannerId}/schedule`, { startAt: startAtIso, endAt: endAtIso })
  },

  /** The banner live right now, or useDefaultBanner when the shop falls back to the one on its own machine. */
  getActive(): Promise<ActiveBanner> {
    return apiClient.get<ActiveBanner>('/banners/active')
  },
}
