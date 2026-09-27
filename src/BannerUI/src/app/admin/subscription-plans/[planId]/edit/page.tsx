'use client';

import React, { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { SubscriptionPlan, UpdatePlanPayload } from '@/types/subscription';
import { useSubscriptionPlans } from '@/hooks/useSubscriptionPlans';
import { PlanForm } from '@/components/admin/PlanForm';

export default function EditPlanPage() {
  const router = useRouter();
  const params = useParams();
  const planId = params.planId as string;
  const { loading, error, getPlanById, updatePlan } = useSubscriptionPlans();
  const [plan, setPlan] = useState<SubscriptionPlan | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [pricingError, setPricingError] = useState<string | null>(null);
  const [pageLoading, setPageLoading] = useState(true);

  useEffect(() => {
    loadPlan();
  }, []);

  const loadPlan = async () => {
    try {
      setPageLoading(true);
      const data = await getPlanById(planId);
      setPlan(data);
    } catch (err) {
      setFormError('Failed to load plan details');
    } finally {
      setPageLoading(false);
    }
  };

  const handleSubmit = async (payload: UpdatePlanPayload) => {
    try {
      setFormError(null);
      setPricingError(null);
      await updatePlan(planId, payload);
      router.push('/admin/subscription-plans?success=Plan updated successfully');
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to update plan';
      if (errorMsg.includes('Cannot modify pricing')) {
        setPricingError(errorMsg);
      } else {
        setFormError(errorMsg);
      }
    }
  };

  const handleCancel = () => {
    router.back();
  };

  if (pageLoading) {
    return <div className="text-center py-8">Loading plan...</div>;
  }

  if (!plan) {
    return <div className="text-center py-8 text-red-600">Plan not found</div>;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Edit Subscription Plan</h1>
        <p className="text-gray-600 mt-1">Update {plan.name} plan details</p>
      </div>

      {/* Pricing Safety Warning */}
      {pricingError && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
          <h3 className="font-semibold text-yellow-900 mb-2">⚠️ Pricing Change Blocked</h3>
          <p className="text-yellow-800 text-sm mb-4">{pricingError}</p>
          <div className="text-sm text-yellow-700 space-y-2">
            <p>
              <strong>Why:</strong> Active subscriptions are locked to their original pricing.
            </p>
            <p>
              <strong>What to do:</strong> Create a new plan with updated pricing. Existing
              subscriptions will continue at the current rate (grandfathered pricing).
            </p>
          </div>
        </div>
      )}

      {/* Form */}
      <div className="bg-white rounded-lg shadow p-8">
        <PlanForm
          plan={plan}
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          loading={loading}
          error={formError || error || undefined}
        />
      </div>

      {/* Info */}
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-6">
        <h3 className="font-semibold text-gray-900 mb-3">Update Guidelines</h3>
        <ul className="text-sm text-gray-700 space-y-2">
          <li>✓ You can always update features, description, and status</li>
          <li>✓ Pricing changes are blocked if active subscriptions exist</li>
          <li>✓ Deactivate the plan when you want to stop selling it</li>
          <li>✓ Changes take effect immediately for new subscriptions</li>
        </ul>
      </div>
    </div>
  );
}
