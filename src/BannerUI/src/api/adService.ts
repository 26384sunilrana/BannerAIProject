import { apiClient, API_ORIGIN } from './client'

export type AdKind = 'Side' | 'Mega' | 'Popup' | 'Minor'
export type AdPlacement = 'Left' | 'Right' | 'Top' | 'Bottom' | 'TopLeft' | 'TopRight' | 'BottomLeft' | 'BottomRight' | 'Center'
export type AdStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'Rejected' | 'Cancelled'
export type AdAction = 'edit' | 'submit' | 'cancel' | 'approve' | 'reject'

export interface ShopAd {
  id: string
  shopId: string
  shopName: string
  /** Who booked it: Admin, ShopOwner or SalesExecutive. */
  source: string
  advertiserName: string
  headline: string
  body: string | null
  mediaFileId: string | null
  mediaUrl: string | null
  background: string
  textColor: string
  kind: AdKind
  placement: AdPlacement
  spacePercent: number
  popupSeconds: number
  popupEveryMinutes: number
  startAt: string
  endAt: string
  dailyStartMinutes: number | null
  dailyEndMinutes: number | null
  activeDays: number
  status: AdStatus
  decidedByName: string | null
  decidedAt: string | null
  decisionNote: string | null
  createdByUserId: string
  can: AdAction[]
}

export interface AdHistoryEntry {
  action: string
  userName: string
  note: string | null
  at: string
}

export interface AdInput {
  /** Only the administrator names the shop. */
  shopId?: string
  advertiserName: string
  headline: string
  body: string | null
  mediaFileId: string | null
  background: string
  textColor: string
  kind: AdKind
  placement: AdPlacement
  spacePercent: number
  popupSeconds: number
  popupEveryMinutes: number
  startAt: string
  endAt: string
  daily: { startMinutes: number; endMinutes: number; days: number } | null
}

const absolute = (ad: ShopAd): ShopAd => ({
  ...ad,
  mediaUrl: ad.mediaUrl ? (/^https?:\/\//i.test(ad.mediaUrl) ? ad.mediaUrl : `${API_ORIGIN}${ad.mediaUrl}`) : null,
})

export const adService = {
  async list(shopId?: string): Promise<ShopAd[]> {
    const query = shopId ? `?shopId=${shopId}` : ''
    return (await apiClient.get<ShopAd[]>(`/shop-ads${query}`)).map(absolute)
  },

  async create(input: AdInput): Promise<ShopAd> {
    return absolute(await apiClient.post<ShopAd>('/shop-ads', input))
  },

  async update(id: string, input: AdInput): Promise<ShopAd> {
    return absolute(await apiClient.put<ShopAd>(`/shop-ads/${id}`, input))
  },

  async act(id: string, action: 'submit' | 'approve' | 'reject' | 'cancel', body: { note?: string; reason?: string } = {}): Promise<ShopAd> {
    return absolute(await apiClient.post<ShopAd>(`/shop-ads/${id}/${action}`, body))
  },

  history(id: string): Promise<AdHistoryEntry[]> {
    return apiClient.get<AdHistoryEntry[]>(`/shop-ads/${id}/history`)
  },

  /** What is on the shop screen at this moment. */
  async live(shopId: string): Promise<ShopAd[]> {
    return (await apiClient.get<ShopAd[]>(`/shops/${shopId}/ads/live`)).map(absolute)
  },
}
