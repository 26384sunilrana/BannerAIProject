export interface SessionUser {
  id: string
  email: string
  name: string
  shopId: string | null
  roles: string[]
  /** An administrator who must set up two-step sign-in before anything else is open. */
  twoFactorSetupRequired?: boolean
}

export interface DecodedToken extends SessionUser {
  /** Expiry as seconds since the epoch. */
  exp: number
}

export interface TokenPair {
  accessToken: string
  /** Not kept by the page: the API keeps the refresh token in an HttpOnly cookie. Accepted so older callers still compile. */
  refreshToken?: string
}

// Older versions kept both tokens in local storage, where any script on the page could read them.
const LEGACY_KEYS = ['auth_token', 'refresh_token']

/** Written (never read for its value) whenever this browser signs in or out, so other tabs notice. Holds no secret. */
export const SESSION_SIGNAL_KEY = 'session_signal'

/** Tells this browser a session probably exists, so a visitor who never signed in does not trigger a refresh call on every page. */
const SESSION_HINT_KEY = 'session_hint'

// The API writes long claim URIs into the token; short names are accepted too.
const CLAIM_ID = ['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier', 'nameid', 'sub']
const CLAIM_EMAIL = ['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress', 'email']
const CLAIM_NAME = ['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name', 'unique_name', 'name']
const CLAIM_ROLE = ['http://schemas.microsoft.com/ws/2008/06/identity/claims/role', 'role', 'roles']
const CLAIM_SHOP = ['shop_id', 'ShopId']

function firstClaim(payload: Record<string, unknown>, names: string[]): unknown {
  for (const name of names) {
    if (payload[name] !== undefined && payload[name] !== null) return payload[name]
  }
  return undefined
}

function base64UrlDecode(value: string): string {
  const padded = value.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(value.length / 4) * 4, '=')
  const binary = atob(padded)
  // Percent-encode the bytes so multi-byte (UTF-8) characters survive
  return decodeURIComponent(
    binary
      .split('')
      .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
      .join('')
  )
}

/** Reads the claims of a JWT. This does not verify the signature: the API does that on every call. */
export function decodeToken(token: string): DecodedToken | null {
  try {
    const part = token.split('.')[1]
    if (!part) return null

    const payload = JSON.parse(base64UrlDecode(part)) as Record<string, unknown>
    const role = firstClaim(payload, CLAIM_ROLE)
    const shopId = firstClaim(payload, CLAIM_SHOP)
    const id = firstClaim(payload, CLAIM_ID)
    const exp = Number(payload.exp)

    if (typeof id !== 'string' || !Number.isFinite(exp)) return null

    return {
      id,
      email: String(firstClaim(payload, CLAIM_EMAIL) ?? ''),
      name: String(firstClaim(payload, CLAIM_NAME) ?? ''),
      shopId: typeof shopId === 'string' && shopId ? shopId : null,
      roles: Array.isArray(role) ? role.map(String) : role ? [String(role)] : [],
      twoFactorSetupRequired: payload.two_factor_setup_required === 'true' || payload.two_factor_setup_required === true || undefined,
      exp,
    }
  } catch {
    return null
  }
}

export function isExpired(exp: number, skewSeconds = 0, now = Date.now()): boolean {
  return exp * 1000 - skewSeconds * 1000 <= now
}

function storage(): Storage | null {
  try {
    return typeof window !== 'undefined' ? window.localStorage : null
  } catch {
    return null // storage blocked
  }
}

// The access token lives only in the page's memory: it is gone when the tab closes and no script can copy it from storage.
// The long-lived refresh token is an HttpOnly cookie that this code never sees.
let accessToken: string | null = null

export function getAccessToken(): string | null {
  return accessToken
}

export function saveTokens(tokens: TokenPair): void {
  accessToken = tokens.accessToken
  storage()?.setItem(SESSION_HINT_KEY, '1')
}

export function clearSession(): void {
  accessToken = null
  storage()?.removeItem(SESSION_HINT_KEY)
}

/** Whether this browser has signed in and not signed out since (the cookie itself cannot be checked from here). */
export function mayHaveSession(): boolean {
  return storage()?.getItem(SESSION_HINT_KEY) === '1'
}

/** Removes the tokens older versions stored in the clear. */
export function purgeLegacyTokens(): void {
  for (const key of LEGACY_KEYS) storage()?.removeItem(key)
}

/** Lets other tabs of this browser know the session changed (they check again with the cookie). */
export function signalSessionChange(): void {
  storage()?.setItem(SESSION_SIGNAL_KEY, String(Date.now()))
}

/**
 * The signed-in user, or null. Having a token is enough: the API decides whether it is still good, and
 * the client quietly gets a new one with the cookie when it is not.
 */
export function getSessionUser(): SessionUser | null {
  if (!accessToken) return null

  const decoded = decodeToken(accessToken)
  if (!decoded) return null

  const { exp: _exp, ...user } = decoded
  return user
}

export function hasRole(user: SessionUser | null, role: string): boolean {
  return !!user && user.roles.some((r) => r.toLowerCase() === role.toLowerCase())
}

export const Roles = {
  /** The product owner. Always also holds Admin, so every administrator screen is open to the owner. */
  SuperAdmin: 'SuperAdmin',
  Admin: 'Admin',
  ShopOwner: 'ShopOwner',
  SalesExecutive: 'SalesExecutive',
} as const
