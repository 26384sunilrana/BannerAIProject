import React from 'react'
import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

const mockReplace = jest.fn()
jest.mock('next/navigation', () => ({ useRouter: () => ({ replace: mockReplace }), usePathname: () => '/team' }))

jest.mock('@/api/client', () => ({
  apiClient: { post: jest.fn(), refreshSession: jest.fn() },
}))

import { apiClient } from '@/api/client'
import { AuthProvider, useAuth } from '@/context/AuthContext'
import { RequireAuth } from '@/components/auth/RequireAuth'
import { SESSION_SIGNAL_KEY, clearSession, getAccessToken, mayHaveSession, saveTokens } from '@/lib/session'

const api = apiClient as jest.Mocked<typeof apiClient>

function makeToken(overrides: Record<string, unknown> = {}): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64').replace(/=+$/, '')
  return `${encode({ alg: 'none' })}.${encode({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': 'user-1',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'olive@example.com',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Olive Owner',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'ShopOwner',
    shop_id: 'shop-1',
    exp: Math.floor(Date.now() / 1000) + 600,
    ...overrides,
  })}.sig`
}

function Probe() {
  const { user, ready, login, logout, refresh } = useAuth()
  return (
    <div>
      <p data-testid="state">{!ready ? 'loading' : user ? `in:${user.email}` : 'out'}</p>
      <button onClick={() => login({ email: 'olive@example.com', password: 'x' })}>login</button>
      <button onClick={() => logout()}>logout</button>
      <button onClick={() => refresh()}>refresh</button>
    </div>
  )
}

const mount = () => render(<AuthProvider><Probe /></AuthProvider>)

beforeEach(() => {
  jest.clearAllMocks()
  clearSession()
  localStorage.clear()
})

describe('RequireAuth after a sign-out that names a destination', () => {
  it('sends the person there instead of to the usual sign-in page', async () => {
    saveTokens({ accessToken: makeToken() })

    function Out() {
      const { logout } = useAuth()
      return <button onClick={() => logout('/login?handover=1')}>leave</button>
    }
    render(<AuthProvider><RequireAuth><Out /></RequireAuth></AuthProvider>)

    await userEvent.click(await screen.findByText('leave'))

    await waitFor(() => expect(mockReplace).toHaveBeenCalledWith('/login?handover=1'))
    expect(mockReplace).not.toHaveBeenCalledWith(expect.stringContaining('returnUrl'))
  })

  it('goes to the usual sign-in page, remembering where the person was, when no destination is named', async () => {
    saveTokens({ accessToken: makeToken() })

    function Out() {
      const { logout } = useAuth()
      return <button onClick={() => logout()}>leave</button>
    }
    render(<AuthProvider><RequireAuth><Out /></RequireAuth></AuthProvider>)

    await userEvent.click(await screen.findByText('leave'))

    await waitFor(() => expect(mockReplace).toHaveBeenCalledWith('/login?returnUrl=%2Fteam'))
  })
})

describe('AuthProvider', () => {
  it('is signed out, with no refresh call, in a browser that never signed in', async () => {
    mount()

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))
    expect(api.refreshSession).not.toHaveBeenCalled()
  })

  it('picks the session up from the cookie after a reload', async () => {
    localStorage.setItem('session_hint', '1')
    api.refreshSession.mockImplementation(async () => {
      saveTokens({ accessToken: makeToken() })
      return true
    })

    mount()

    expect(screen.getByTestId('state')).toHaveTextContent('loading')
    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('in:olive@example.com'))
  })

  it('is signed out, and forgets the hint, when the cookie no longer works', async () => {
    localStorage.setItem('session_hint', '1')
    api.refreshSession.mockResolvedValue(false)

    mount()

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))
    expect(mayHaveSession()).toBe(false)
  })

  it('removes the tokens older versions kept in local storage', async () => {
    localStorage.setItem('auth_token', 'old')
    localStorage.setItem('refresh_token', 'older')

    mount()

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))
    expect(localStorage.getItem('auth_token')).toBeNull()
    expect(localStorage.getItem('refresh_token')).toBeNull()
  })

  it('signs in with the access token in memory only, and tells the other tabs', async () => {
    api.post.mockResolvedValue({ message: 'ok', tokens: { accessToken: makeToken(), refreshToken: '' } })
    mount()
    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))

    await userEvent.click(screen.getByText('login'))

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('in:olive@example.com'))
    expect(getAccessToken()).not.toBeNull()
    expect(localStorage.getItem(SESSION_SIGNAL_KEY)).not.toBeNull()
    expect(Object.keys(localStorage).some((k) => /token/i.test(k))).toBe(false)
  })

  it('signs out: asks the API to end the session and forgets everything', async () => {
    saveTokens({ accessToken: makeToken() })
    api.post.mockResolvedValue({ message: 'bye' })
    mount()
    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('in:'))

    await userEvent.click(screen.getByText('logout'))

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))
    expect(api.post).toHaveBeenCalledWith('/authentication/logout', {})
    expect(getAccessToken()).toBeNull()
    expect(mayHaveSession()).toBe(false)
  })

  it('signs out here even when the server cannot be reached', async () => {
    saveTokens({ accessToken: makeToken() })
    api.post.mockRejectedValue(new Error('offline'))
    mount()
    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('in:'))

    await userEvent.click(screen.getByText('logout'))

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))
  })

  it('follows another tab that signs out', async () => {
    saveTokens({ accessToken: makeToken() })
    mount()
    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('in:'))

    // the other tab cleared the shared hint and announced it
    localStorage.removeItem('session_hint')
    act(() => {
      window.dispatchEvent(new StorageEvent('storage', { key: SESSION_SIGNAL_KEY, newValue: String(Date.now()) }))
    })

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))
  })

  it('follows another tab that signs in', async () => {
    mount()
    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))
    api.refreshSession.mockImplementation(async () => {
      saveTokens({ accessToken: makeToken({ 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'new@example.com' }) })
      return true
    })

    act(() => {
      localStorage.setItem('session_hint', '1')
      window.dispatchEvent(new StorageEvent('storage', { key: SESSION_SIGNAL_KEY, newValue: String(Date.now()) }))
    })

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('in:new@example.com'))
  })

  it('ignores unrelated storage changes', async () => {
    mount()
    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))

    act(() => {
      window.dispatchEvent(new StorageEvent('storage', { key: 'display_cache', newValue: '{}' }))
    })

    expect(api.refreshSession).not.toHaveBeenCalled()
  })

  it('refresh() looks for the session again (a shop screen that started offline)', async () => {
    mount()
    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('out'))
    localStorage.setItem('session_hint', '1')
    api.refreshSession.mockImplementation(async () => {
      saveTokens({ accessToken: makeToken() })
      return true
    })

    await userEvent.click(screen.getByText('refresh'))

    await waitFor(() => expect(screen.getByTestId('state')).toHaveTextContent('in:olive@example.com'))
  })
})
