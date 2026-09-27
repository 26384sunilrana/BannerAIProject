'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { CreatePlanPayload, UpdatePlanPayload } from '@/types/subscription';
import { useSubscriptionPlans } from '@/hooks/useSubscriptionPlans';
import { PlanForm } from '@/components/admin/PlanForm';

export default function NewPlanPage() {
  const router = useRouter();
  const { loading, error, createPlan } = useSubscriptionPlans();
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = async (payload: CreatePlanPayload | UpdatePlanPayload) => {
    try {
      setFormError(null);
      await createPlan(payload as CreatePlanPayload);
      router.push('/admin/subscription-plans?success=Plan created successfully');
    } catch (err) {
      setFormError(err instanceof Error ? err.message : 'Failed to create plan');
    }
  };

  const handleCancel = () => {
    router.back();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Create New Subscription Plan</h1>
        <p className="text-gray-600 mt-1">Add a new subscription plan to your offering</p>
      </div>

      {/* Form */}
      <div className="bg-white rounded-lg shadow p-8">
        <PlanForm
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          loading={loading}
          error={formError || error || undefined}
        />
      </div>

      {/* Info */}
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-6">
        <h3 className="font-semibold text-gray-900 mb-3">Plan Creation Guidelines</h3>
        <ul className="text-sm text-gray-700 space-y-2">
          <li>✓ Choose a unique, descriptive plan name</li>
          <li>✓ Set both monthly and annual pricing</li>
          <li>✓ Define features that differentiate this plan</li>
          <li>✓ Plans are active by default (ready for new subscriptions)</li>
          <li>✓ Display order determines listing position on pricing pages</li>
        </ul>
      </div>
    </div>
  );
}
