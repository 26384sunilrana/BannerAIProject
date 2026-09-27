'use client';

import React from 'react';
import Link from 'next/link';
import { SubscriptionPlan } from '@/types/subscription';

interface SubscriptionPlansTableProps {
  plans: SubscriptionPlan[];
  onEdit: (plan: SubscriptionPlan) => void;
  onDeactivate: (plan: SubscriptionPlan) => void;
  loading?: boolean;
}

export const SubscriptionPlansTable: React.FC<SubscriptionPlansTableProps> = ({
  plans,
  onEdit,
  onDeactivate,
  loading = false,
}) => {
  if (loading) {
    return <div className="text-center py-8">Loading plans...</div>;
  }

  if (plans.length === 0) {
    return (
      <div className="text-center py-8 text-gray-500">
        <p>No subscription plans found</p>
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full border-collapse border border-gray-300">
        <thead className="bg-gray-100">
          <tr>
            <th className="border border-gray-300 px-4 py-2 text-left">Plan Name</th>
            <th className="border border-gray-300 px-4 py-2 text-left">Description</th>
            <th className="border border-gray-300 px-4 py-2 text-right">Monthly Price</th>
            <th className="border border-gray-300 px-4 py-2 text-right">Annual Price</th>
            <th className="border border-gray-300 px-4 py-2 text-center">Status</th>
            <th className="border border-gray-300 px-4 py-2 text-center">Actions</th>
          </tr>
        </thead>
        <tbody>
          {plans.map((plan) => (
            <tr key={plan.id} className="hover:bg-gray-50">
              <td className="border border-gray-300 px-4 py-2 font-semibold">{plan.name}</td>
              <td className="border border-gray-300 px-4 py-2 text-sm text-gray-600">
                {plan.description}
              </td>
              <td className="border border-gray-300 px-4 py-2 text-right">
                ${plan.monthlyPrice.toFixed(2)}/mo
              </td>
              <td className="border border-gray-300 px-4 py-2 text-right">
                ${plan.annualPrice.toFixed(2)}/yr
              </td>
              <td className="border border-gray-300 px-4 py-2 text-center">
                <span
                  className={`inline-block px-3 py-1 rounded-full text-xs font-semibold ${
                    plan.isActive
                      ? 'bg-green-100 text-green-800'
                      : 'bg-gray-100 text-gray-800'
                  }`}
                >
                  {plan.isActive ? 'Active' : 'Inactive'}
                </span>
              </td>
              <td className="border border-gray-300 px-4 py-2 text-center">
                <div className="flex gap-2 justify-center">
                  <button
                    onClick={() => onEdit(plan)}
                    className="text-blue-600 hover:text-blue-800 text-sm font-semibold"
                  >
                    Edit
                  </button>
                  <Link
                    href={`/admin/subscription-plans/${plan.id}`}
                    className="text-purple-600 hover:text-purple-800 text-sm font-semibold"
                  >
                    View
                  </Link>
                  {plan.isActive && (
                    <button
                      onClick={() => onDeactivate(plan)}
                      className="text-red-600 hover:text-red-800 text-sm font-semibold"
                    >
                      Deactivate
                    </button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
