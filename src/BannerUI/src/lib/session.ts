export interface SessionUser {
  id: string
  email: string
  name: string
  shopId: string | null
  roles: string[]
}

export interface DecodedToken extends SessionUser {
  /** Expiry as seconds since the epoch. */
  exp: number
}

export interface TokenPair {
  accessToken: string
  refreshToken: string
}

const ACCESS_KEY = 'auth_token'
const REFRESH_KEY = 'refresh_token'

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
  return typeof window !== 'undefined' ? window.localStorage : null
}

export function getAccessToken(): string | null {
  return storage()?.getItem(ACCESS_KEY) ?? null
}

export function getRefreshToken(): string | null {
  return storage()?.getItem(REFRESH_KEY) ?? null
}

export function saveTokens(tokens: TokenPair): void {
  storage()?.setItem(ACCESS_KEY, tokens.accessToken)
  storage()?.setItem(REFRESH_KEY, tokens.refreshToken)
}

export function clearSession(): void {
  storage()?.removeItem(ACCESS_KEY)
  storage()?.removeItem(REFRESH_KEY)
}

/** The signed-in user, or null. An expired access token still counts while a refresh token exists. */
export function getSessionUser(): SessionUser | null {
  const token = getAccessToken()
  if (!token) return null

  const decoded = decodeToken(token)
  if (!decoded) return null

  if (isExpired(decoded.exp) && !getRefreshToken()) return null

  const { exp: _exp, ...user } = decoded
  return user
}

export function hasRole(user: SessionUser | null, role: string): boolean {
  return !!user && user.roles.some((r) => r.toLowerCase() === role.toLowerCase())
}

export const Roles = {
  Admin: 'Admin',
  ShopOwner: 'ShopOwner',
  SalesExecutive: 'SalesExecutive',
} as const
