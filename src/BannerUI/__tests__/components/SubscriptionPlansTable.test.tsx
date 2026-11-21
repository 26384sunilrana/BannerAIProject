import React from 'react'
import { render, screen, within } from '@testing-library/react'
import { SubscriptionPlansTable } from '@/components/admin/SubscriptionPlansTable'
import { SubscriptionPlan } from '@/types/subscription'

const features = {
  maxBanners: 5, maxShops: 1, maxUsers: 1, maxStorageGB: 1, apiAccess: false, customDomain: false,
  advancedAnalytics: false, dedicatedSupport: false, slaPercentage: 99, priorityQueue: false,
}

const plan = (overrides: Partial<SubscriptionPlan>): SubscriptionPlan => ({
  id: 'p1', name: 'Silver', description: 'Silver plan', monthlyPrice: 79.99, annualPrice: 799.99, features, isActive: true, displayOrder: 1, createdAt: '2026-01-01T00:00:00Z', ...overrides,
})

describe('SubscriptionPlansTable: one way of showing money', () => {
  it('shows prices in the plan currency, rupees when the plan does not say', () => {
    render(
      <SubscriptionPlansTable
        plans={[plan({ id: 'a', name: 'Silver', currency: 'INR' }), plan({ id: 'b', name: 'Legacy' }), plan({ id: 'c', name: 'Visitor', currency: 'USD', monthlyPrice: 9.99, annualPrice: 99.99 })]}
        onDeactivate={jest.fn()}
        onEdit={jest.fn()}
      />
    )

    const silver = within(screen.getByText('Silver').closest('tr') as HTMLElement)
    expect(silver.getByText('₹79.99/mo')).toBeInTheDocument()
    expect(silver.getByText('₹799.99/yr')).toBeInTheDocument()
    expect(within(screen.getByText('Legacy').closest('tr') as HTMLElement).getByText('₹79.99/mo')).toBeInTheDocument()
    const visitor = within(screen.getByText('Visitor').closest('tr') as HTMLElement)
    expect(visitor.getByText('$9.99/mo')).toBeInTheDocument()
    expect(visitor.getByText('$99.99/yr')).toBeInTheDocument()
  })
})
