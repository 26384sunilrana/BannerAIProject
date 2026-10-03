import { apiClient } from './client'

export type TakeoverStatus = 'Requested' | 'Declined' | 'Cancelled' | 'Completed'
export type TakeoverAssociates = 'Keep' | 'Replace'

export interface Takeover {
  id: string
  /** Incoming: someone asks for the caller's shop. Outgoing: the caller asked. Admin: seen by an administrator. */
  direction: 'Incoming' | 'Outgoing' | 'Admin'
  existingShopName: string
  newShopName: string
  requesterName: string
  /** Masked, for example s***@example.com. */
  requesterEmail: string
  existingOwnerName: string
  status: TakeoverStatus
  associates: TakeoverAssociates | null
  decidedByName: string | null
  decidedByAdmin: boolean
  note: string | null
  createdAt: string
  decidedAt: string | null
  can: ('confirm' | 'confirm-as-admin' | 'decline' | 'cancel')[]
}

export const takeoverService = {
  list: () => apiClient.get<Takeover[]>('/shop-takeovers'),

  ask: (name: string, address: string, postalCode: string) => apiClient.post<Takeover>('/shop-takeovers', { name, address, postalCode: postalCode || null }),

  confirm: (id: string, associates: TakeoverAssociates, password: string) => apiClient.post<Takeover>(`/shop-takeovers/${id}/confirm`, { associates, password }),

  confirmAsAdmin: (id: string, associates: TakeoverAssociates, note: string) => apiClient.post<Takeover>(`/shop-takeovers/${id}/confirm-as-admin`, { associates, note }),

  decline: (id: string, note: string) => apiClient.post<Takeover>(`/shop-takeovers/${id}/decline`, { note }),

  cancel: (id: string) => apiClient.post<Takeover>(`/shop-takeovers/${id}/cancel`),
}
