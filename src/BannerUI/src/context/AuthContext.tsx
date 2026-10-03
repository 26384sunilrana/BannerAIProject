'use client'

import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { authService } from '@/api/authService'
import { LoginRequest, RegisterRequest } from '@/types/auth'
import {
  SESSION_SIGNAL_KEY,
  SessionUser,
  clearSession,
  getSessionUser,
  hasRole as userHasRole,
  purgeLegacyTokens,
} from '@/lib/session'

interface AuthContextValue {
  user: SessionUser | null
  /** False until the session has been looked up (the server renders without it, and the cookie is exchanged after load). */
  ready: boolean
  /** Resolves with a challenge when a code is needed next (two-step sign-in); with nothing when the person is signed in. */
  login: (request: LoginRequest) => Promise<{ challenge: string } | void>
  loginWithCode: (challenge: string, code: string) => Promise<void>
  register: (request: RegisterRequest) => Promise<void>
  /** Signs out. Pages that need a session then send the person to redirectTo (default: the sign-in page, remembering where they were). */
  logout: (redirectTo?: string) => Promise<void>
  /** Where to send the person after a sign-out that said so; null for the usual sign-in page. */
  signedOutRedirect: string | null
  /** Looks for a session again (for example a shop screen that started offline). Returns whether there is one. */
  refresh: () => Promise<boolean>
  hasRole: (role: string) => boolean
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null)
  const [ready, setReady] = useState(false)
  const [signedOutRedirect, setSignedOutRedirect] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    const ok = await authService.restore()
    setUser(ok ? getSessionUser() : null)
    return ok
  }, [])

  useEffect(() => {
    purgeLegacyTokens()
    let alive = true

    ;(async () => {
      // already signed in during this page's life (a client-side move), otherwise exchange the cookie for a token
      if (getSessionUser()) setUser(getSessionUser())
      else await refresh()
      if (alive) setReady(true)
    })()

    // Another tab signed in or out: look again. A tab that is signed in keeps its own token until the next call says otherwise.
    const onStorage = (event: StorageEvent) => {
      if (event.key !== SESSION_SIGNAL_KEY) return
      authService.restore().then((ok) => {
        if (!alive) return
        if (!ok) clearSession()
        setUser(ok ? getSessionUser() : null)
      })
    }
    window.addEventListener('storage', onStorage)
    return () => {
      alive = false
      window.removeEventListener('storage', onStorage)
    }
  }, [refresh])

  const login = useCallback(async (request: LoginRequest) => {
    const result = await authService.login(request)
    if (result && 'requiresTwoFactor' in result && result.requiresTwoFactor) return { challenge: result.challenge }
    setSignedOutRedirect(null)
    setUser(getSessionUser())
    return undefined
  }, [])

  const loginWithCode = useCallback(async (challenge: string, code: string) => {
    await authService.completeTwoFactor(challenge, code)
    setSignedOutRedirect(null)
    setUser(getSessionUser())
  }, [])

  const register = useCallback(async (request: RegisterRequest) => {
    await authService.register(request)
    setUser(getSessionUser())
  }, [])

  const logout = useCallback(async (redirectTo?: string) => {
    await authService.logout()
    setSignedOutRedirect(redirectTo ?? null)
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, ready, login, loginWithCode, register, logout, refresh, signedOutRedirect, hasRole: (role) => userHasRole(user, role) }),
    [user, ready, login, loginWithCode, register, logout, refresh, signedOutRedirect]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>')
  return context
}
