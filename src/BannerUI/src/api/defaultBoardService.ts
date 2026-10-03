import { apiClient, API_ORIGIN } from './client'

/** How the shop's default board looks. Null fields mean the standard look. */
export interface DefaultBoardSettings {
  message: string | null
  /** #rrggbb */
  background: string | null
  textColor: string | null
  logoMediaFileId: string | null
  /** A link the screen can load for the logo (expires after four hours). */
  logoUrl: string | null
  shopName: string
}

export interface DefaultBoardChanges {
  message: string | null
  background: string | null
  textColor: string | null
  logoMediaFileId: string | null
}

const absolute = (url: string | null): string | null => (url ? (/^https?:\/\//i.test(url) ? url : `${API_ORIGIN}${url}`) : null)

export const defaultBoardService = {
  async get(shopId: string): Promise<DefaultBoardSettings> {
    const board = await apiClient.get<DefaultBoardSettings>(`/shops/${shopId}/default-board`)
    return { ...board, logoUrl: absolute(board.logoUrl) }
  },

  async save(shopId: string, changes: DefaultBoardChanges): Promise<DefaultBoardSettings> {
    const board = await apiClient.put<DefaultBoardSettings>(`/shops/${shopId}/default-board`, changes)
    return { ...board, logoUrl: absolute(board.logoUrl) }
  },
}
