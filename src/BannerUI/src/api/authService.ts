import { apiClient } from './client'
import { AuthResult, LoginRequest, RegisterRequest } from '@/types/auth'
import { clearSession, getRefreshToken, saveTokens } from '@/lib/session'

export const authService = {
  async login(request: LoginRequest): Promise<AuthResult> {
    const result = await apiClient.post<AuthResult>('/authentication/login', {
      email: request.email.trim(),
      password: request.password,
    })
    saveTokens(result.tokens)
    return result
  },

  async register(request: RegisterRequest): Promise<AuthResult> {
    const result = await apiClient.post<AuthResult>('/authentication/register', {
      ...request,
      email: request.email.trim(),
      shopName: request.shopName.trim(),
    })
    saveTokens(result.tokens)
    return result
  },

  /** Ends the session on this device. The server call is best effort: the local session is cleared either way. */
  async logout(): Promise<void> {
    const refreshToken = getRefreshToken()
    try {
      if (refreshToken) {
        await apiClient.post('/authentication/logout', { refreshToken })
      }
    } catch {
      // already signed out or offline
    } finally {
      clearSession()
    }
  },
}
