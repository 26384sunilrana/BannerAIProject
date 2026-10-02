/**
 * What a shop screen remembers on its own machine so it can still show something when the server cannot be
 * reached or the account has been switched off: the shop's name for the default board.
 */
const KEY = 'display_cache'

export interface DisplayCache {
  shopId: string
  shopName: string
  savedAt: number
}

export function loadDisplayCache(): DisplayCache | null {
  try {
    const raw = typeof window !== 'undefined' ? window.localStorage.getItem(KEY) : null
    if (!raw) return null
    const parsed = JSON.parse(raw)
    return typeof parsed?.shopName === 'string' && typeof parsed?.shopId === 'string' ? parsed : null
  } catch {
    return null
  }
}

export function saveDisplayCache(cache: Omit<DisplayCache, 'savedAt'>): void {
  try {
    window.localStorage.setItem(KEY, JSON.stringify({ ...cache, savedAt: Date.now() }))
  } catch {
    // storage full or blocked: the screen simply has no local copy
  }
}
