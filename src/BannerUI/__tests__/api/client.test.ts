jest.mock('axios', () => {
  const instance = {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    patch: jest.fn(),
    delete: jest.fn(),
    interceptors: { request: { use: jest.fn() }, response: { use: jest.fn() } },
  }
  return { __esModule: true, default: { create: jest.fn(() => instance) } }
})

import axios from 'axios'
import { getErrorMessage } from '@/api/client'

const create = axios.create as jest.Mock
const instance = create.mock.results[0].value

describe('apiClient', () => {
  it('creates the HTTP client with a base URL, timeout and JSON headers', () => {
    expect(create).toHaveBeenCalledWith(
      expect.objectContaining({
        baseURL: expect.any(String),
        timeout: 30000,
        headers: expect.objectContaining({ 'Content-Type': 'application/json' }),
      })
    )
  })

  it('adds the token on the way out and handles errors on the way back', () => {
    expect(instance.interceptors.request.use).toHaveBeenCalled()
    expect(instance.interceptors.response.use).toHaveBeenCalled()
  })
})

describe('getErrorMessage', () => {
  const failure = (status: number, data?: unknown) => ({ response: { status, data } })

  it('prefers the message the API sent', () => {
    expect(getErrorMessage(failure(400, { message: 'Name is required' }))).toBe('Name is required')
    expect(getErrorMessage(failure(400, { error: { message: 'Nested' } }))).toBe('Nested')
    expect(getErrorMessage(failure(400, 'plain text'))).toBe('plain text')
  })

  it('takes the first validation error', () => {
    expect(getErrorMessage(failure(400, { errors: { Email: ['Email is invalid'] } }))).toBe('Email is invalid')
  })

  it('explains 403, 401 and a lost connection', () => {
    expect(getErrorMessage(failure(403))).toMatch(/permission/i)
    expect(getErrorMessage(failure(401))).toMatch(/expired/i)
    expect(getErrorMessage({ code: 'ERR_NETWORK' })).toMatch(/reach the server/i)
  })

  it('falls back to a generic message', () => {
    expect(getErrorMessage(new Error('x'))).toMatch(/something went wrong/i)
    expect(getErrorMessage(new Error('x'), 'Custom')).toBe('Custom')
  })
})
