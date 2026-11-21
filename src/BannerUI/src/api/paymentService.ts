import { apiClient } from './client'

/** What the web app may know about online payment. Never a secret. */
export interface PaymentConfig {
  enabled: boolean
  provider: string | null
  currency: string
  country: string
}

/** What the provider's own payment form needs. */
export interface CheckoutSession {
  provider: string
  orderId: string
  amount: number
  /** The amount in the currency's smallest unit (paise, cents), as the provider's form wants it. */
  amountMinor: number
  currency: string
  publicKeyId: string
}

/** What the provider's payment form hands back after a successful payment. */
export interface CheckoutResult {
  orderId: string
  paymentId: string
  signature: string
}

export const paymentService = {
  getConfig(): Promise<PaymentConfig> {
    return apiClient.get<PaymentConfig>('/payments/config')
  },

  startCheckout(subscriptionId: string): Promise<CheckoutSession> {
    return apiClient.post<CheckoutSession>(`/subscriptions/${subscriptionId}/checkout`, {})
  },

  /** Tells the server what the payment form returned. The server checks the signature; the provider's own report confirms it again. */
  async confirm(subscriptionId: string, result: CheckoutResult): Promise<void> {
    await apiClient.post(`/subscriptions/${subscriptionId}/checkout/confirm`, result)
  },
}
