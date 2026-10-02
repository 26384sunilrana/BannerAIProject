import { useState, useCallback } from 'react';
import { authFetch } from '@/api/client';
import {
  SubscriptionPlan,
  PlansListResponse,
  PlanDetailResponse,
  CreatePlanPayload,
  UpdatePlanPayload,
  PlanSubscriptionsResponse,
  PlanSubscription,
} from '@/types/subscription';


export const useSubscriptionPlans = () => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Get all plans
  const getPlans = useCallback(async (includeInactive = false): Promise<SubscriptionPlan[]> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(
        `/admin/subscription-plans?includeInactive=${includeInactive}`,
        {
          headers: { 'Content-Type': 'application/json' },
        }
      );

      if (!response.ok) {
        throw new Error(`Failed to fetch plans: ${response.statusText}`);
      }

      const data: PlansListResponse = await response.json();
      return data.data || [];
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch plans';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  // Get single plan with details
  const getPlanById = useCallback(async (planId: string): Promise<SubscriptionPlan> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(`/admin/subscription-plans/${planId}`, {
        headers: { 'Content-Type': 'application/json' },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch plan: ${response.statusText}`);
      }

      const data: PlanDetailResponse = await response.json();
      return data.data;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch plan';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  // Create plan
  const createPlan = useCallback(async (payload: CreatePlanPayload): Promise<SubscriptionPlan> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(`/admin/subscription-plans`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || `Failed to create plan: ${response.statusText}`);
      }

      const data = await response.json();
      return data.data;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to create plan';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  // Update plan
  const updatePlan = useCallback(
    async (planId: string, payload: UpdatePlanPayload): Promise<SubscriptionPlan> => {
      setLoading(true);
      setError(null);
      try {
        const response = await authFetch(`/admin/subscription-plans/${planId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        });

        if (!response.ok) {
          const errorData = await response.json();
          throw new Error(errorData.message || `Failed to update plan: ${response.statusText}`);
        }

        const data = await response.json();
        return data.data;
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to update plan';
        setError(message);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    []
  );

  // Deactivate plan
  const deactivatePlan = useCallback(async (planId: string): Promise<void> => {
    setLoading(true);
    setError(null);
    try {
      const response = await authFetch(`/admin/subscription-plans/${planId}/deactivate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || `Failed to deactivate plan: ${response.statusText}`);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to deactivate plan';
      setError(message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  // Get plan subscriptions
  const getPlanSubscriptions = useCallback(
    async (planId: string, status = 'all'): Promise<PlanSubscription[]> => {
      setLoading(true);
      setError(null);
      try {
        const response = await authFetch(
          `/admin/subscription-plans/${planId}/subscriptions?status=${status}`,
          {
            headers: { 'Content-Type': 'application/json' },
          }
        );

        if (!response.ok) {
          throw new Error(`Failed to fetch subscriptions: ${response.statusText}`);
        }

        const data: PlanSubscriptionsResponse = await response.json();
        return data.subscriptions || [];
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to fetch subscriptions';
        setError(message);
        throw err;
      } finally {
        setLoading(false);
      }
    },
    []
  );

  return {
    loading,
    error,
    getPlans,
    getPlanById,
    createPlan,
    updatePlan,
    deactivatePlan,
    getPlanSubscriptions,
  };
};
