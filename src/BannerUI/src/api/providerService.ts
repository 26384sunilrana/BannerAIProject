import { apiClient } from './client'

export interface ProviderStatus {
  /** payments, sms or email */
  name: 'payments' | 'sms' | 'email'
  provider: string
  configured: boolean
  /** Names of the settings that are empty (never their values). */
  missing: string[]
}

export interface ProviderTestResult {
  sent: boolean
  reason?: string | null
}

export const providerService = {
  getStatus(): Promise<ProviderStatus[]> {
    return apiClient.get<ProviderStatus[]>('/admin/providers')
  },

  /** Without an address the test goes to the administrator's own. */
  sendTestEmail(to?: string): Promise<ProviderTestResult> {
    return apiClient.post<ProviderTestResult>('/admin/providers/test-email', { to: to?.trim() || null })
  },

  sendTestSms(to: string): Promise<ProviderTestResult> {
    return apiClient.post<ProviderTestResult>('/admin/providers/test-sms', { to: to.trim() })
  },
}
