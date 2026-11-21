import { apiClient } from './client'
import { AuthResult, LoginRequest, RegisterRequest, TwoFactorChallenge } from '@/types/auth'
import { clearSession, mayHaveSession, saveTokens, signalSessionChange } from '@/lib/session'

export const authService = {
  /** Signs in. For someone who uses two-step sign-in the answer is a challenge and there is no session yet: finish with completeTwoFactor. */
  async login(request: LoginRequest): Promise<AuthResult | TwoFactorChallenge> {
    const result = await apiClient.post<AuthResult | TwoFactorChallenge>('/authentication/login', {
      email: request.email.trim(),
      password: request.password,
    })
    if ('requiresTwoFactor' in result && result.requiresTwoFactor) return result

    saveTokens((result as AuthResult).tokens)
    signalSessionChange()
    return result
  },

  /** The second step: the challenge from the password step and the code from the app (or a recovery code). */
  async completeTwoFactor(challenge: string, code: string): Promise<AuthResult> {
    const result = await apiClient.post<AuthResult>('/authentication/login/two-factor', { challenge, code: code.trim() })
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

  /** Asks for a reset link. The answer is the same whether or not the address has an account. */
  async forgotPassword(email: string): Promise<string> {
    const result = await apiClient.post<{ message: string }>('/authentication/forgot-password', { email: email.trim() })
    return result.message
  },

  /** The token is the one from the link in the e-mail; no sign-in is needed. */
  async resetPassword(token: string, newPassword: string, confirmPassword: string): Promise<string> {
    const result = await apiClient.post<{ message: string }>('/authentication/reset-password', { token, newPassword, confirmPassword })
    return result.message
  },

  async verifyEmail(token: string): Promise<string> {
    const result = await apiClient.post<{ message: string }>('/authentication/verify-email', { token })
    return result.message
  },

  /** For a signed-in person who has not verified yet. */
  async resendVerification(): Promise<string> {
    const result = await apiClient.post<{ message: string }>('/authentication/resend-verification', {})
    return result.message
  },

  /** Whether the signed-in person has verified their e-mail address, and whether this installation can send mail at all. */
  getEmailStatus(): Promise<{ emailVerified: boolean; emailEnabled: boolean }> {
    return apiClient.get<{ emailVerified: boolean; emailEnabled: boolean }>('/authentication/me')
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
