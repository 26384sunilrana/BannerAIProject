import {
  clearSession,
  decodeToken,
  getSessionUser,
  hasRole,
  isExpired,
  saveTokens,
} from '@/lib/session'
import { safeReturnUrl } from '@/lib/redirect'
import { BillingPeriod, formatMoney, priceFor } from '@/lib/pricing'

function makeToken(payload: Record<string, unknown>): string {
  const encode = (value: object) =>
    Buffer.from(JSON.stringify(value)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.signature`
}

// The claim names the API really writes into its tokens
const apiPayload = (overrides: Record<string, unknown> = {}) => ({
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': 'user-1',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'olive@example.com',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Olive Owner',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'ShopOwner',
  shop_id: 'shop-1',
  exp: Math.floor(Date.now() / 1000) + 600,
  ...overrides,
})

describe('decodeToken', () => {
  it('reads the claims the API writes', () => {
    const decoded = decodeToken(makeToken(apiPayload()))

    expect(decoded).toMatchObject({
      id: 'user-1',
      email: 'olive@example.com',
      name: 'Olive Owner',
      shopId: 'shop-1',
      roles: ['ShopOwner'],
    })
  })

  it('accepts several roles', () => {
    const decoded = decodeToken(
      makeToken(apiPayload({ 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': ['Admin', 'ShopOwner'] }))
    )

    expect(decoded?.roles).toEqual(['Admin', 'ShopOwner'])
  })

  it('handles non-ASCII names', () => {
    const decoded = decodeToken(makeToken(apiPayload({ 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Zoë Ñandú' })))

    expect(decoded?.name).toBe('Zoë Ñandú')
  })

  it('returns null for garbage or a token without an id or expiry', () => {
    expect(decodeToken('not-a-token')).toBeNull()
    expect(decodeToken(makeToken({ exp: 1 }))).toBeNull()
    expect(decodeToken(makeToken({ sub: 'x' }))).toBeNull()
  })
})

describe('session storage', () => {
  beforeEach(() => localStorage.clear())

  it('returns the signed-in user from a valid token', () => {
    saveTokens({ accessToken: makeToken(apiPayload()), refreshToken: 'r' })

    const user = getSessionUser()

    expect(user?.id).toBe('user-1')
    expect(hasRole(user, 'shopowner')).toBe(true)
    expect(hasRole(user, 'Admin')).toBe(false)
  })

  it('keeps an expired access token while a refresh token exists, drops it otherwise', () => {
    const expired = makeToken(apiPayload({ exp: Math.floor(Date.now() / 1000) - 60 }))

    saveTokens({ accessToken: expired, refreshToken: 'r' })
    expect(getSessionUser()).not.toBeNull()

    localStorage.removeItem('refresh_token')
    expect(getSessionUser()).toBeNull()
  })

  it('clears both tokens', () => {
    saveTokens({ accessToken: makeToken(apiPayload()), refreshToken: 'r' })

    clearSession()

    expect(localStorage.getItem('auth_token')).toBeNull()
    expect(localStorage.getItem('refresh_token')).toBeNull()
    expect(getSessionUser()).toBeNull()
  })

  it('isExpired honours a skew', () => {
    const now = 1_000_000
    expect(isExpired(now / 1000 + 5, 0, now)).toBe(false)
    expect(isExpired(now / 1000 + 5, 10, now)).toBe(true)
  })
})

describe('safeReturnUrl', () => {
  it.each([
    ['/team', '/team'],
    ['/approvals?x=1', '/approvals?x=1'],
    [null, '/dashboard'],
    ['https://evil.example.com', '/dashboard'],
    ['//evil.example.com', '/dashboard'],
    ['/\\evil.example.com', '/dashboard'],
    ['/login', '/dashboard'],
    ['/register', '/dashboard'],
  ])('%s -> %s', (input, expected) => {
    expect(safeReturnUrl(input as string | null)).toBe(expected)
  })
})

describe('priceFor', () => {
  const plan = { monthlyPrice: 79.99, annualPrice: 799.99 }

  it('uses the plan prices for monthly and yearly', () => {
    expect(priceFor(plan, BillingPeriod.Monthly)).toBe(79.99)
    expect(priceFor(plan, BillingPeriod.Yearly)).toBe(799.99)
  })

  it('spreads the yearly price over quarterly and half-yearly periods', () => {
    expect(priceFor(plan, BillingPeriod.Quarterly)).toBe(200)
    expect(priceFor(plan, BillingPeriod.HalfYearly)).toBe(400)
  })

  it('formats rupees', () => {
    expect(formatMoney(1999.5)).toContain('1,999.5')
  })
})
