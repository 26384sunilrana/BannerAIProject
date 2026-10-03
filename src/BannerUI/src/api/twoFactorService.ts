import { apiClient } from './client'

export interface TwoFactorStatus {
  enabled: boolean
  recoveryCodesLeft: number
  /** Administrators must use it and cannot switch it off. */
  required: boolean
}

export interface TwoFactorSetup {
  /** The secret in groups of four, for typing into an authenticator app. */
  secret: string
  /** The otpauth:// address behind the QR picture. */
  uri: string
}

export const twoFactorService = {
  status: () => apiClient.get<TwoFactorStatus>('/account/two-factor'),

  beginSetup: (password: string) => apiClient.post<TwoFactorSetup>('/account/two-factor/setup', { password }),

  /** Confirms the first code; answers with the recovery codes (shown once). */
  async enable(code: string): Promise<string[]> {
    return (await apiClient.post<{ codes: string[] }>('/account/two-factor/enable', { code })).codes
  },

  async disable(password: string, code: string): Promise<void> {
    await apiClient.post('/account/two-factor/disable', { password, code })
  },

  async newRecoveryCodes(password: string, code: string): Promise<string[]> {
    return (await apiClient.post<{ codes: string[] }>('/account/two-factor/recovery-codes', { password, code })).codes
  },
}
