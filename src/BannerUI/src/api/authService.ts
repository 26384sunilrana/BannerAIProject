import { apiClient } from './client'
import { AuthResult, LoginRequest, RegisterRequest } from '@/types/auth'
import { clearSession, mayHaveSession, saveTokens, signalSessionChange } from '@/lib/session'

export const authService = {
  async login(request: LoginRequest): Promise<AuthResult> {
    const result = await apiClient.post<AuthResult>('/authentication/login', {
      email: request.email.trim(),
      password: request.password,
    })
    saveTokens(result.tokens)
    signalSessionChange()
    return result
  },

  async register(request: RegisterRequest): Promise<AuthResult> {
    const result = await apiClient.post<AuthResult>('/authentication/register', {
      ...request,
      email: request.email.trim(),
      shopName: request.shopName.trim(),
    })
    saveTokens(result.tokens)
    signalSessionChange()
    return result
  },

  /**
   * Picks the session up again after a page load or in a new tab: the refresh cookie is exchanged for an access token.
   * Skipped when this browser never signed in. Returns whether there is a session now.
   */
  async restore(): Promise<boolean> {
    if (!mayHaveSession()) return false
    const ok = await apiClient.refreshSession()
    if (!ok) clearSession()
    return ok
  },

  /** Ends the session on this device. The server call is best effort: the local session is cleared either way. */
  async logout(): Promise<void> {
    try {
      await apiClient.post('/authentication/logout', {})
    } catch {
      // already signed out or offline
    } finally {
      clearSession()
      signalSessionChange()
    }
  },
}
