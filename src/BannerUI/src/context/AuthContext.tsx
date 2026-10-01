'use client'

import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { authService } from '@/api/authService'
import { LoginRequest, RegisterRequest } from '@/types/auth'
import { SessionUser, getSessionUser, hasRole as userHasRole } from '@/lib/session'

interface AuthContextValue {
  user: SessionUser | null
  /** False until the stored session has been read (the server renders without it). */
  ready: boolean
  login: (request: LoginRequest) => Promise<void>
  register: (request: RegisterRequest) => Promise<void>
  logout: () => Promise<void>
  hasRole: (role: string) => boolean
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    setUser(getSessionUser())
    setReady(true)

    // Another tab signed in or out
    const onStorage = () => setUser(getSessionUser())
    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [])

  const login = useCallback(async (request: LoginRequest) => {
    await authService.login(request)
    setUser(getSessionUser())
  }, [])

  const register = useCallback(async (request: RegisterRequest) => {
    await authService.register(request)
    setUser(getSessionUser())
  }, [])

  const logout = useCallback(async () => {
    await authService.logout()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, ready, login, register, logout, hasRole: (role) => userHasRole(user, role) }),
    [user, ready, login, register, logout]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>')
  return context
}
