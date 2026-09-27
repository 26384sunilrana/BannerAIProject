'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { SubscriptionPlan } from '@/types/subscription';
import { useSubscriptionPlans } from '@/hooks/useSubscriptionPlans';
import { SubscriptionPlansTable } from '@/components/admin/SubscriptionPlansTable';

export default function SubscriptionPlansPage() {
  const { loading, error, getPlans, deactivatePlan } = useSubscriptionPlans();
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [showInactive, setShowInactive] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    loadPlans();
  }, [showInactive]);

  const loadPlans = async () => {
    try {
      const data = await getPlans(showInactive);
      setPlans(data);
    } catch (err) {
      setMessage({ type: 'error', text: 'Failed to load plans' });
    }
  };

  const handleDeactivate = async (plan: SubscriptionPlan) => {
    if (!confirm(`Are you sure you want to deactivate "${plan.name}"?`)) return;

    try {
      await deactivatePlan(plan.id);
      setMessage({ type: 'success', text: `Plan "${plan.name}" deactivated successfully` });
      await loadPlans();
    } catch (err) {
      setMessage({ type: 'error', text: `Failed to deactivate plan: ${error}` });
    }
  };

  const handleEdit = (plan: SubscriptionPlan) => {
    // Would navigate to edit page or open modal
    window.location.href = `/admin/subscription-plans/${plan.id}/edit`;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Subscription Plans</h1>
          <p className="text-gray-600 mt-1">Manage your subscription plan offerings</p>
        </div>
        <Link
          href="/admin/subscription-plans/new"
          className="px-6 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700"
        >
          + New Plan
        </Link>
      </div>

      {/* Messages */}
      {message && (
        <div
          className={`p-4 rounded-lg ${
            message.type === 'success'
              ? 'bg-green-50 text-green-800 border border-green-200'
              : 'bg-red-50 text-red-800 border border-red-200'
          }`}
        >
          {message.text}
        </div>
      )}

      {/* Filters */}
      <div className="flex gap-4">
        <label className="flex items-center">
          <input
            type="checkbox"
            checked={showInactive}
            onChange={(e) => {
              setShowInactive(e.target.checked);
              // Will reload in useEffect
            }}
            className="mr-2"
          />
          <span className="text-sm font-medium text-gray-700">Show Inactive Plans</span>
        </label>
      </div>

      {/* Plans Table */}
      <div className="bg-white rounded-lg shadow">
        <SubscriptionPlansTable
          plans={plans}
          onEdit={handleEdit}
          onDeactivate={handleDeactivate}
          loading={loading}
        />
      </div>

      {/* Info Box */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h3 className="font-semibold text-blue-900 mb-2">⚠️ Pricing Safety Notice</h3>
        <ul className="text-sm text-blue-800 space-y-1">
          <li>✓ Prices cannot be changed if active subscriptions exist on this plan</li>
          <li>✓ Existing subscriptions are grandfathered at original pricing</li>
          <li>✓ Create a new plan with updated pricing if you need price changes</li>
          <li>✓ Plans are soft-deleted (marked inactive) to preserve history</li>
        </ul>
      </div>
    </div>
  );
}
