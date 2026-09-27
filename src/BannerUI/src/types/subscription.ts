export interface SubscriptionFeatures {
  maxBanners: number;
  maxShops: number;
  maxUsers: number;
  maxStorageGB: number;
  apiAccess: boolean;
  customDomain: boolean;
  advancedAnalytics: boolean;
  dedicatedSupport: boolean;
  slaPercentage: number;
  priorityQueue: boolean;
}

export interface SubscriptionPlan {
  id: string;
  name: string;
  description: string;
  monthlyPrice: number;
  annualPrice: number;
  features: SubscriptionFeatures;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
}

export interface PlanMetadata {
  canDelete: boolean;
  deleteWarning?: string;
  activeSubscriptions?: number;
}

export interface PlanDetailResponse {
  success: boolean;
  data: SubscriptionPlan;
  activeSubscriptions: number;
  metadata: PlanMetadata;
}

export interface PlansListResponse {
  success: boolean;
  count: number;
  data: SubscriptionPlan[];
}

export interface CreatePlanPayload {
  name: string;
  description: string;
  monthlyPrice: number;
  annualPrice: number;
  features?: SubscriptionFeatures;
  isActive: boolean;
  displayOrder: number;
}

export interface UpdatePlanPayload {
  description?: string;
  monthlyPrice: number;
  annualPrice: number;
  features?: SubscriptionFeatures;
  isActive: boolean;
  displayOrder: number;
}

export interface PlanSubscription {
  id: string;
  shopId: string;
  shopName: string;
  email: string;
  status: string;
  currentBillingCycle: string;
  startDate: string;
  renewalDate: string;
  currentPrice: number;
}

export interface PlanSubscriptionsResponse {
  success: boolean;
  planName: string;
  totalSubscriptions: number;
  byStatus: Record<string, number>;
  subscriptions: PlanSubscription[];
}
