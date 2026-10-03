import { apiClient } from './client'

export interface ScreenInfo {
  id: string
  name: string
  online: boolean
  createdAt: string
  lastSeenAt: string | null
  appVersion: string | null
  status: string
}

export interface PlayRow {
  kind: 'Banner' | 'Ad' | 'DefaultBoard'
  refId: string | null
  label: string
  hours: number
}

export interface PlayReport {
  from: string
  to: string
  rows: PlayRow[]
  days: { day: string; hours: number }[]
}

/** What the owner does with the shop's screen. The screen's own calls are in the player. */
export const screenService = {
  list: (shopId: string) => apiClient.get<ScreenInfo[]>(`/shops/${shopId}/screens`),

  /** Types the six characters the screen shows. */
  pair: (shopId: string, code: string, name: string) => apiClient.post<ScreenInfo>(`/shops/${shopId}/screens/pair`, { code, name }),

  rename: (shopId: string, screenId: string, name: string) => apiClient.put<ScreenInfo>(`/shops/${shopId}/screens/${screenId}`, { name }),

  async remove(shopId: string, screenId: string): Promise<void> {
    await apiClient.delete(`/shops/${shopId}/screens/${screenId}`)
  },

  report: (shopId: string, days = 7) => apiClient.get<PlayReport>(`/shops/${shopId}/screens/report?days=${days}`),
}
