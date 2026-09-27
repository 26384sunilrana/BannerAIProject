'use client';

import React, { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { SubscriptionPlan, PlanSubscription } from '@/types/subscription';
import { useSubscriptionPlans } from '@/hooks/useSubscriptionPlans';
import Link from 'next/link';


export default function PlanDetailPage() {
  const router = useRouter();
  const params = useParams();
  const planId = params.planId as string;
  const { getPlanById, getPlanSubscriptions } = useSubscriptionPlans();
  const [plan, setPlan] = useState<SubscriptionPlan | null>(null);
  const [subscriptions, setSubscriptions] = useState<PlanSubscription[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [pageLoading, setPageLoading] = useState(true);

  useEffect(() => {
    loadDetails();
  }, []);

  const loadDetails = async () => {
    try {
      setPageLoading(true);
      const [planData, subsData] = await Promise.all([
        getPlanById(planId),
        getPlanSubscriptions(planId),
      ]);
      setPlan(planData);
      setSubscriptions(subsData);
    } catch (err) {
      setError('Failed to load plan details');
    } finally {
      setPageLoading(false);
    }
  };

  if (pageLoading) {
    return <div className="text-center py-8">Loading plan details...</div>;
  }

  if (!plan) {
    return <div className="text-center py-8 text-red-600">Plan not found</div>;
  }

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">{plan.name}</h1>
          <p className="text-gray-600 mt-1">{plan.description}</p>
        </div>
        <div className="flex gap-2">
          <Link
            href={`/admin/subscription-plans/${plan.id}/edit`}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700"
          >
            Edit Plan
          </Link>
          <button
            onClick={() => router.back()}
            className="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg font-semibold hover:bg-gray-50"
          >
            Back
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-800 text-sm font-semibold">{error}</p>
        </div>
      )}

      {/* Plan Details Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Pricing Card */}
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Pricing</h3>
          <div className="space-y-3">
            <div>
              <p className="text-sm text-gray-600">Monthly Price</p>
              <p className="text-2xl font-bold text-gray-900">${plan.monthlyPrice.toFixed(2)}</p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Annual Price</p>
              <p className="text-2xl font-bold text-gray-900">${plan.annualPrice.toFixed(2)}</p>
            </div>
          </div>
        </div>

        {/* Status Card */}
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Status</h3>
          <div className="space-y-3">
            <div>
              <p className="text-sm text-gray-600">Plan Status</p>
              <div>
                <span
                  className={`inline-block px-3 py-1 rounded-full text-xs font-semibold ${
                    plan.isActive
                      ? 'bg-green-100 text-green-800'
                      : 'bg-gray-100 text-gray-800'
                  }`}
                >
                  {plan.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
            </div>
            <div>
              <p className="text-sm text-gray-600">Display Order</p>
              <p className="text-lg font-semibold text-gray-900">{plan.displayOrder}</p>
            </div>
            <div>
              <p className="text-sm text-gray-600">Active Subscriptions</p>
              <p className="text-lg font-semibold text-gray-900">
                {subscriptions.filter((s) => s.status === 'active').length}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Features */}
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Features</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <p className="text-sm text-gray-600">Max Banners</p>
            <p className="text-xl font-semibold text-gray-900">{plan.features.maxBanners}</p>
          </div>
          <div>
            <p className="text-sm text-gray-600">Max Shops</p>
            <p className="text-xl font-semibold text-gray-900">{plan.features.maxShops}</p>
          </div>
          <div>
            <p className="text-sm text-gray-600">Max Users</p>
            <p className="text-xl font-semibold text-gray-900">{plan.features.maxUsers}</p>
          </div>
          <div>
            <p className="text-sm text-gray-600">Storage</p>
            <p className="text-xl font-semibold text-gray-900">{plan.features.maxStorageGB} GB</p>
          </div>
          <div>
            <p className="text-sm text-gray-600">SLA Percentage</p>
            <p className="text-xl font-semibold text-gray-900">{plan.features.slaPercentage}%</p>
          </div>
        </div>

        <div className="mt-6 pt-6 border-t space-y-2">
          {plan.features.apiAccess && <p className="text-sm text-green-700">✓ API Access</p>}
          {plan.features.customDomain && <p className="text-sm text-green-700">✓ Custom Domain</p>}
          {plan.features.advancedAnalytics && (
            <p className="text-sm text-green-700">✓ Advanced Analytics</p>
          )}
          {plan.features.dedicatedSupport && (
            <p className="text-sm text-green-700">✓ Dedicated Support</p>
          )}
          {plan.features.priorityQueue && <p className="text-sm text-green-700">✓ Priority Queue</p>}
        </div>
      </div>

      {/* Subscriptions */}
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">
          Active Subscriptions ({subscriptions.length})
        </h3>
        {subscriptions.length === 0 ? (
          <p className="text-gray-500 text-center py-8">No subscriptions for this plan</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full border-collapse border border-gray-300">
              <thead className="bg-gray-100">
                <tr>
                  <th className="border border-gray-300 px-4 py-2 text-left">Shop</th>
                  <th className="border border-gray-300 px-4 py-2 text-left">Email</th>
                  <th className="border border-gray-300 px-4 py-2 text-left">Billing Cycle</th>
                  <th className="border border-gray-300 px-4 py-2 text-left">Status</th>
                  <th className="border border-gray-300 px-4 py-2 text-left">Start Date</th>
                  <th className="border border-gray-300 px-4 py-2 text-left">Renewal Date</th>
                </tr>
              </thead>
              <tbody>
                {subscriptions.map((sub) => (
                  <tr key={sub.id} className="hover:bg-gray-50">
                    <td className="border border-gray-300 px-4 py-2 font-semibold">{sub.shopName || 'N/A'}</td>
                    <td className="border border-gray-300 px-4 py-2 text-sm">{sub.email || 'N/A'}</td>
                    <td className="border border-gray-300 px-4 py-2 text-sm">
                      {sub.currentBillingCycle || 'N/A'}
                    </td>
                    <td className="border border-gray-300 px-4 py-2 text-sm">
                      <span
                        className={`inline-block px-2 py-1 rounded text-xs font-semibold ${
                          sub.status === 'active'
                            ? 'bg-green-100 text-green-800'
                            : 'bg-gray-100 text-gray-800'
                        }`}
                      >
                        {sub.status}
                      </span>
                    </td>
                    <td className="border border-gray-300 px-4 py-2 text-sm">{new Date(sub.startDate).toLocaleDateString()}</td>
                    <td className="border border-gray-300 px-4 py-2 text-sm">{new Date(sub.renewalDate).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Pricing Safety Note */}
      {subscriptions.length > 0 && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
          <h3 className="font-semibold text-yellow-900 mb-2">📌 Pricing Safety</h3>
          <p className="text-sm text-yellow-800 mb-3">
            This plan has {subscriptions.length} active subscription(s). To prevent disruption:
          </p>
          <ul className="text-sm text-yellow-800 space-y-1">
            <li>✓ Price changes are blocked to protect subscriber agreements</li>
            <li>✓ Create a new plan if you need to offer different pricing</li>
            <li>✓ Existing subscriptions use grandfathered pricing indefinitely</li>
          </ul>
        </div>
      )}
    </div>
  );
}
