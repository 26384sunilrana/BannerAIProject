import axios from 'axios'
import { API_URL } from '@/api/client'
import { MAX_CACHED_LOGO_CHARS } from '@/lib/displayCache'

/**
 * What the television itself keeps: the long secret it made when it started pairing. It never leaves the device except in the
 * X-Screen-Token header of the call that trades it for an hour-long access token.
 */
const SECRET_KEY = 'bannerai.screen.secret'

export function loadScreenSecret(): string | null {
  try {
    return window.localStorage.getItem(SECRET_KEY)
  } catch {
    return null
  }
}

export function saveScreenSecret(secret: string): void {
  try {
    window.localStorage.setItem(SECRET_KEY, secret)
  } catch {
    // storage blocked: the screen has to be paired again after a restart
  }
}

export function forgetScreenSecret(): void {
  try {
    window.localStorage.removeItem(SECRET_KEY)
  } catch {
    // nothing to forget
  }
}

export interface PairingStart {
  code: string
  deviceSecret: string
  expiresAt: string
}

export interface PairingState {
  status: 'waiting' | 'paired' | 'expired'
  shopName?: string | null
}

export interface ScreenToken {
  accessToken: string
  expiresIn: number
  shopId: string
  shopName: string
}

const headers = { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }

/** The calls a screen makes before it has a token. They need no login: the secret is the proof. */
export const deviceApi = {
  async startPairing(): Promise<PairingStart> {
    return (await axios.post<PairingStart>(`${API_URL}/screens/pairing/start`, {}, { headers })).data
  },

  async pollPairing(deviceSecret: string): Promise<PairingState> {
    return (await axios.post<PairingState>(`${API_URL}/screens/pairing/poll`, { deviceSecret }, { headers })).data
  },

  /** Trades the secret for an access token. A 401 means this screen was removed or is not paired. */
  async exchange(deviceSecret: string): Promise<ScreenToken> {
    return (await axios.post<ScreenToken>(`${API_URL}/screens/token`, {}, { headers: { ...headers, 'X-Screen-Token': deviceSecret } })).data
  },
}

/** Fetches a picture and returns it as a data address, or null when it cannot be fetched or is too big to keep. */
export async function toDataAddress(url: string): Promise<string | null> {
  try {
    const response = await fetch(url)
    if (!response.ok) return null
    const blob = await response.blob()
    const data = await new Promise<string>((resolve, reject) => {
      const reader = new FileReader()
      reader.onload = () => resolve(String(reader.result))
      reader.onerror = () => reject(reader.error)
      reader.readAsDataURL(blob)
    })
    return data.length <= MAX_CACHED_LOGO_CHARS ? data : null
  } catch {
    return null
  }
}
