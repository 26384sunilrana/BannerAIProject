import { apiClient } from './client'
import { clearSession, saveTokens, signalSessionChange } from '@/lib/session'

export interface Account {
  id: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string | null
  shopId: string | null
  roles: string[]
  emailVerified: boolean
  createdAt: string
}

export interface PasswordChange {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export const accountService = {
  get(): Promise<Account> {
    return apiClient.get<Account>('/account')
  },

  update(details: { firstName: string; lastName: string; phoneNumber: string | null }): Promise<Account> {
    return apiClient.put<Account>('/account', details)
  },

  /** Changes the password. Every other device is signed out; this one gets a new session straight away. */
  async changePassword(change: PasswordChange): Promise<void> {
    const result = await apiClient.post<{ tokens: { accessToken: string } }>('/account/change-password', change)
    saveTokens({ accessToken: result.tokens.accessToken })
    signalSessionChange()
  },

  /** Ends every session of this person, on every device, this one included. */
  async signOutEverywhere(): Promise<void> {
    try {
      await apiClient.post('/account/sign-out-everywhere', {})
    } finally {
      clearSession()
      signalSessionChange()
    }
  },
}
