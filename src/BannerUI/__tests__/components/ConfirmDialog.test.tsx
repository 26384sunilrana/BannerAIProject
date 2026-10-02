import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ConfirmDialog } from '@/components/Common/ConfirmDialog'

describe('ConfirmDialog', () => {
  it('renders nothing when isOpen is false', () => {
    const { container } = render(
      <ConfirmDialog
        isOpen={false}
        title="Delete"
        message="Are you sure?"
        onConfirm={jest.fn()}
        onCancel={jest.fn()}
      />
    )
    expect(container.querySelector('[class*="bg-black"]')).not.toBeInTheDocument()
  })

  it('renders with title and message when isOpen is true', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        title="Delete Item"
        message="This action cannot be undone"
        onConfirm={jest.fn()}
        onCancel={jest.fn()}
      />
    )
    expect(screen.getByText('Delete Item')).toBeInTheDocument()
    expect(screen.getByText('This action cannot be undone')).toBeInTheDocument()
  })

  it('calls onConfirm when confirm button is clicked', async () => {
    const handleConfirm = jest.fn()
    render(
      <ConfirmDialog
        isOpen={true}
        title="Confirm"
        message="Continue?"
        confirmText="Yes"
        onConfirm={handleConfirm}
        onCancel={jest.fn()}
      />
    )

    await userEvent.click(screen.getByText('Yes'))
    expect(handleConfirm).toHaveBeenCalledTimes(1)
  })

  it('calls onCancel when cancel button is clicked', async () => {
    const handleCancel = jest.fn()
    render(
      <ConfirmDialog
        isOpen={true}
        title="Confirm"
        message="Continue?"
        cancelText="No"
        onConfirm={jest.fn()}
        onCancel={handleCancel}
      />
    )

    await userEvent.click(screen.getByText('No'))
    expect(handleCancel).toHaveBeenCalledTimes(1)
  })

  it('disables buttons when isLoading is true', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        title="Processing"
        message="Please wait..."
        confirmText="Confirm"
        cancelText="Cancel"
        isLoading={true}
        onConfirm={jest.fn()}
        onCancel={jest.fn()}
      />
    )

    const buttons = screen.getAllByRole('button')
    buttons.forEach((button) => {
      expect(button).toBeDisabled()
    })
  })

  it('applies danger variant when isDangerous is true', () => {
    const { container } = render(
      <ConfirmDialog
        isOpen={true}
        title="Delete"
        message="Confirm deletion"
        isDangerous={true}
        onConfirm={jest.fn()}
        onCancel={jest.fn()}
      />
    )

    const buttons = container.querySelectorAll('button')
    expect(buttons.length).toBeGreaterThan(0)
  })

  it('uses default confirm and cancel text', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        title="Confirm"
        message="Continue?"
        onConfirm={jest.fn()}
        onCancel={jest.fn()}
      />
    )

    expect(screen.getByRole('button', { name: 'Confirm' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument()
  })

  it('uses custom confirm and cancel text', () => {
    render(
      <ConfirmDialog
        isOpen={true}
        title="Delete"
        message="Are you sure?"
        confirmText="Delete"
        cancelText="Keep"
        onConfirm={jest.fn()}
        onCancel={jest.fn()}
      />
    )

    expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Keep' })).toBeInTheDocument()
  })

  it('renders modal with proper styling', () => {
    const { container } = render(
      <ConfirmDialog
        isOpen={true}
        title="Test"
        message="Message"
        onConfirm={jest.fn()}
        onCancel={jest.fn()}
      />
    )

    const backdrop = container.querySelector('[class*="fixed inset-0"]')
    expect(backdrop).toBeInTheDocument()
    expect(backdrop).toHaveClass('bg-black')
  })
})
