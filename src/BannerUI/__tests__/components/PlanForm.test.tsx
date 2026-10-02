import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { PlanForm } from '@/components/admin/PlanForm'
import { SubscriptionPlan } from '@/types/subscription'

describe('PlanForm', () => {
  const mockPlan: SubscriptionPlan = {
    id: '1',
    name: 'Professional',
    description: 'Professional plan',
    monthlyPrice: 29.99,
    annualPrice: 299.99,
    features: {
      maxBanners: 10, maxShops: 2, maxUsers: 3, maxStorageGB: 5, apiAccess: false, customDomain: false,
      advancedAnalytics: false, dedicatedSupport: false, slaPercentage: 99, priorityQueue: false,
    },
    isActive: true,
    displayOrder: 2,
    createdAt: '2026-01-01T00:00:00Z',
  }

  const onSubmit = jest.fn().mockResolvedValue(undefined)
  const onCancel = jest.fn()
  const renderForm = (props: Partial<React.ComponentProps<typeof PlanForm>> = {}) =>
    render(<PlanForm onSubmit={onSubmit} onCancel={onCancel} {...props} />)

  const fill = async (name: string, monthly: string, annual: string) => {
    await userEvent.type(screen.getByLabelText(/plan name/i), name)
    await userEvent.clear(screen.getByLabelText(/monthly price/i))
    await userEvent.type(screen.getByLabelText(/monthly price/i), monthly)
    await userEvent.clear(screen.getByLabelText(/annual price/i))
    await userEvent.type(screen.getByLabelText(/annual price/i), annual)
  }

  beforeEach(() => jest.clearAllMocks())

  it('starts empty when creating a plan', () => {
    renderForm()

    expect(screen.getByLabelText(/plan name/i)).toHaveValue('')
    expect(screen.getByLabelText(/monthly price/i)).toHaveValue(0)
    expect(screen.getByRole('button', { name: 'Create Plan' })).toBeInTheDocument()
  })

  it('is filled in when editing, and the name cannot change', () => {
    renderForm({ plan: mockPlan })

    expect(screen.getByLabelText(/plan name/i)).toHaveValue('Professional')
    expect(screen.getByLabelText(/plan name/i)).toBeDisabled()
    expect(screen.getByLabelText(/monthly price/i)).toHaveValue(29.99)
    expect(screen.getByLabelText(/annual price/i)).toHaveValue(299.99)
    expect(screen.getByLabelText(/max banners/i)).toHaveValue(10)
    expect(screen.getByRole('button', { name: 'Update Plan' })).toBeInTheDocument()
  })

  it('needs a name', async () => {
    renderForm()

    fireEvent.click(screen.getByRole('button', { name: 'Create Plan' }))

    expect(await screen.findByText('Plan name is required')).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('needs a price', async () => {
    renderForm()
    await userEvent.type(screen.getByLabelText(/plan name/i), 'Free')

    fireEvent.click(screen.getByRole('button', { name: 'Create Plan' }))

    expect(await screen.findByText(/at least one price/i)).toBeInTheDocument()
  })

  it('rejects an annual price below the monthly price', async () => {
    renderForm()
    await fill('Odd', '100', '50')

    fireEvent.click(screen.getByRole('button', { name: 'Create Plan' }))

    expect(await screen.findByText(/annual price must be at least the monthly price/i)).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('does not accept a cleared price as valid', async () => {
    renderForm()
    await userEvent.type(screen.getByLabelText(/plan name/i), 'Blank')
    await userEvent.clear(screen.getByLabelText(/monthly price/i))

    fireEvent.click(screen.getByRole('button', { name: 'Create Plan' }))

    expect(await screen.findByText(/enter both prices/i)).toBeInTheDocument()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('submits a valid plan with its limits', async () => {
    renderForm()
    await fill('Basic', '9.99', '99.99')
    await userEvent.type(screen.getByLabelText(/description/i), 'A basic plan')

    fireEvent.click(screen.getByRole('button', { name: 'Create Plan' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Basic',
        description: 'A basic plan',
        monthlyPrice: 9.99,
        annualPrice: 99.99,
        features: expect.objectContaining({ maxBanners: 5, maxStorageGB: 1 }),
      })
    )
  })

  it('leaves the name out when updating', async () => {
    renderForm({ plan: mockPlan })

    fireEvent.click(screen.getByRole('button', { name: 'Update Plan' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1))
    expect(onSubmit.mock.calls[0][0]).not.toHaveProperty('name')
  })

  it('lets the limits and feature switches change', async () => {
    renderForm({ plan: mockPlan })

    await userEvent.clear(screen.getByLabelText(/max banners/i))
    await userEvent.type(screen.getByLabelText(/max banners/i), '25')
    await userEvent.click(screen.getByLabelText(/api access/i))
    fireEvent.click(screen.getByRole('button', { name: 'Update Plan' }))

    await waitFor(() => expect(onSubmit).toHaveBeenCalled())
    expect(onSubmit.mock.calls[0][0].features).toMatchObject({ maxBanners: 25, apiAccess: true })
  })

  it('calls onCancel', () => {
    renderForm()

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalled()
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('is disabled while saving', () => {
    renderForm({ loading: true })

    expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  it('shows an error from the parent', () => {
    renderForm({ error: 'Failed to save plan' })

    expect(screen.getByText('Failed to save plan')).toBeInTheDocument()
  })
})
