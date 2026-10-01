import { getErrorMessage } from '@/api/client'

const failure = (status: number, data?: unknown, code?: string) => ({ response: { status, data }, code })

describe('getErrorMessage', () => {
  it('uses the message the API sends', () => {
    expect(getErrorMessage(failure(400, { message: 'Email already registered' }))).toBe('Email already registered')
  })

  it('reads the global error envelope', () => {
    expect(getErrorMessage(failure(409, { error: { code: 'CONFLICT', message: 'JWT Key not configured' } }))).toBe(
      'JWT Key not configured'
    )
  })

  it('reads an error given as plain text', () => {
    expect(getErrorMessage(failure(400, { error: 'Checksum mismatch - data integrity failed' }))).toBe(
      'Checksum mismatch - data integrity failed'
    )
  })

  it('reads the first validation error', () => {
    expect(getErrorMessage(failure(400, { errors: { Email: ['Email is required'] } }))).toBe('Email is required')
  })

  it('explains 403 and 401 when there is no message', () => {
    expect(getErrorMessage(failure(403))).toMatch(/permission/i)
    expect(getErrorMessage(failure(401))).toMatch(/session has expired/i)
  })

  it('explains network failures and falls back otherwise', () => {
    expect(getErrorMessage({ code: 'ERR_NETWORK' })).toMatch(/cannot reach the server/i)
    expect(getErrorMessage(new Error('x'), 'custom fallback')).toBe('custom fallback')
  })
})
