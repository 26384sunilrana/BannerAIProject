'use client';

import React, { useState } from 'react';
import { SubscriptionPlan, CreatePlanPayload, UpdatePlanPayload, SubscriptionFeatures } from '@/types/subscription';

interface PlanFormProps {
  plan?: SubscriptionPlan;
  onSubmit: (payload: CreatePlanPayload | UpdatePlanPayload) => Promise<void>;
  onCancel: () => void;
  loading?: boolean;
  error?: string;
}

export const PlanForm: React.FC<PlanFormProps> = ({
  plan,
  onSubmit,
  onCancel,
  loading = false,
  error,
}) => {
  const [formData, setFormData] = useState({
    name: plan?.name || '',
    description: plan?.description || '',
    monthlyPrice: plan?.monthlyPrice || 0,
    annualPrice: plan?.annualPrice || 0,
    isActive: plan?.isActive !== undefined ? plan.isActive : true,
    displayOrder: plan?.displayOrder || 1,
  });

  const [features, setFeatures] = useState<SubscriptionFeatures>(
    plan?.features || {
      maxBanners: 5,
      maxShops: 1,
      maxUsers: 1,
      maxStorageGB: 1,
      apiAccess: false,
      customDomain: false,
      advancedAnalytics: false,
      dedicatedSupport: false,
      slaPercentage: 99.0,
      priorityQueue: false,
    }
  );

  const [validationError, setValidationError] = useState<string | null>(null);

  const validateForm = (): boolean => {
    if (!formData.name.trim()) {
      setValidationError('Plan name is required');
      return false;
    }
    if (formData.monthlyPrice < 0 || formData.annualPrice < 0) {
      setValidationError('Prices cannot be negative');
      return false;
    }
    if (formData.monthlyPrice === 0 && formData.annualPrice === 0) {
      setValidationError('At least one price must be greater than zero');
      return false;
    }
    setValidationError(null);
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      const payload = plan
        ? {
            description: formData.description,
            monthlyPrice: formData.monthlyPrice,
            annualPrice: formData.annualPrice,
            features,
            isActive: formData.isActive,
            displayOrder: formData.displayOrder,
          }
        : {
            name: formData.name,
            description: formData.description,
            monthlyPrice: formData.monthlyPrice,
            annualPrice: formData.annualPrice,
            features,
            isActive: formData.isActive,
            displayOrder: formData.displayOrder,
          };

      await onSubmit(payload);
    } catch (err) {
      // Error is handled by parent
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6 max-w-2xl">
      {(error || validationError) && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-800 text-sm font-semibold">{error || validationError}</p>
        </div>
      )}

      {/* Plan Name */}
      <div>
        <label className="block text-sm font-semibold text-gray-700 mb-2">Plan Name *</label>
        <input
          type="text"
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
          disabled={!!plan}
          placeholder="e.g., Silver"
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
        />
      </div>

      {/* Description */}
      <div>
        <label className="block text-sm font-semibold text-gray-700 mb-2">Description</label>
        <textarea
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          placeholder="Plan description"
          rows={3}
          className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* Pricing */}
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-semibold text-gray-700 mb-2">Monthly Price ($)</label>
          <input
            type="number"
            value={formData.monthlyPrice}
            onChange={(e) => setFormData({ ...formData, monthlyPrice: parseFloat(e.target.value) })}
            placeholder="0.00"
            step="0.01"
            min="0"
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div>
          <label className="block text-sm font-semibold text-gray-700 mb-2">Annual Price ($)</label>
          <input
            type="number"
            value={formData.annualPrice}
            onChange={(e) => setFormData({ ...formData, annualPrice: parseFloat(e.target.value) })}
            placeholder="0.00"
            step="0.01"
            min="0"
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      {/* Features */}
      <div className="border-t pt-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Features</h3>
        <div className="grid grid-cols-2 gap-4">
          {[
            { key: 'maxBanners', label: 'Max Banners', type: 'number' },
            { key: 'maxShops', label: 'Max Shops', type: 'number' },
            { key: 'maxUsers', label: 'Max Users', type: 'number' },
            { key: 'maxStorageGB', label: 'Max Storage (GB)', type: 'number' },
            { key: 'slaPercentage', label: 'SLA Percentage', type: 'number' },
          ].map((field) => (
            <div key={field.key}>
              <label className="block text-sm font-medium text-gray-700 mb-1">{field.label}</label>
              <input
                type={field.type}
                value={String(features[field.key as keyof SubscriptionFeatures])}
                onChange={(e) =>
                  setFeatures({
                    ...features,
                    [field.key]: field.type === 'number' ? parseFloat(e.target.value) : e.target.value,
                  })
                }
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
              />
            </div>
          ))}
        </div>

        <div className="mt-4 space-y-3">
          {[
            { key: 'apiAccess', label: 'API Access' },
            { key: 'customDomain', label: 'Custom Domain' },
            { key: 'advancedAnalytics', label: 'Advanced Analytics' },
            { key: 'dedicatedSupport', label: 'Dedicated Support' },
            { key: 'priorityQueue', label: 'Priority Queue' },
          ].map((field) => (
            <label key={field.key} className="flex items-center">
              <input
                type="checkbox"
                checked={features[field.key as keyof SubscriptionFeatures] as boolean}
                onChange={(e) =>
                  setFeatures({
                    ...features,
                    [field.key]: e.target.checked,
                  })
                }
                className="mr-3"
              />
              <span className="text-sm font-medium text-gray-700">{field.label}</span>
            </label>
          ))}
        </div>
      </div>

      {/* Status & Display Order */}
      <div className="grid grid-cols-2 gap-4 border-t pt-6">
        <div>
          <label className="flex items-center">
            <input
              type="checkbox"
              checked={formData.isActive}
              onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
              className="mr-3"
            />
            <span className="text-sm font-medium text-gray-700">Active</span>
          </label>
        </div>
        <div>
          <label className="block text-sm font-semibold text-gray-700 mb-2">Display Order</label>
          <input
            type="number"
            value={formData.displayOrder}
            onChange={(e) => setFormData({ ...formData, displayOrder: parseInt(e.target.value) })}
            min="1"
            className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>

      {/* Buttons */}
      <div className="flex gap-4 pt-6 border-t">
        <button
          type="submit"
          disabled={loading}
          className="px-6 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400"
        >
          {loading ? 'Saving...' : plan ? 'Update Plan' : 'Create Plan'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={loading}
          className="px-6 py-2 border border-gray-300 text-gray-700 rounded-lg font-semibold hover:bg-gray-50 disabled:bg-gray-100"
        >
          Cancel
        </button>
      </div>
    </form>
  );
};
