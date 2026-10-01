import { apiClient } from './client'

export interface PlanSummary {
  id: string
  name: string
  description: string
  monthlyPrice: number
  annualPrice: number
  isActive: boolean
}

export interface ShopSubscription {
  id: string
  shopId: string
  planId: string
  planName: string
  status: number
  billingPeriod: number
  currentPrice: number
  startDate: string
  renewalDate: string
  autoRenew: boolean
  pendingPlanId?: string | null
  pendingPlanEffectiveAt?: string | null
}

export const subscriptionService = {
  getPlans(): Promise<PlanSummary[]> {
    return apiClient.get<PlanSummary[]>('/subscriptions/plans')
  },

  /** The shop's current subscription, or null when it has none. */
  getCurrent(shopId: string): Promise<ShopSubscription | null> {
    return apiClient.get<ShopSubscription>(`/subscriptions/${shopId}`).catch((error) => {
      if (error?.response?.status === 404) return null
      throw error
    })
  },

  subscribe(shopId: string, planId: string, billingPeriod: number): Promise<ShopSubscription> {
    return apiClient.post<ShopSubscription>('/subscriptions', { shopId, planId, billingPeriod })
  },

  async setAutoRenew(subscriptionId: string, autoRenew: boolean): Promise<void> {
    await apiClient.put(`/subscriptions/${subscriptionId}/auto-renew`, { autoRenew })
  },

  async changePlan(subscriptionId: string, newPlanId: string, upgrade: boolean): Promise<void> {
    await apiClient.post(`/subscriptions/${subscriptionId}/${upgrade ? 'upgrade' : 'downgrade'}`, { newPlanId })
  },

  async changeBillingPeriod(subscriptionId: string, newPeriod: number): Promise<void> {
    await apiClient.post(`/subscriptions/${subscriptionId}/change-billing`, { newPeriod })
  },
}
