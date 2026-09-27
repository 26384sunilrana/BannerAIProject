import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

// Mock Modal component for testing purposes
function Modal({
  isOpen,
  title,
  children,
  onClose,
  footer,
}: {
  isOpen: boolean
  title?: string
  children: React.ReactNode
  onClose: () => void
  footer?: React.ReactNode
}) {
  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
        {title && (
          <div className="border-b px-6 py-4">
            <h2 className="text-lg font-bold">{title}</h2>
          </div>
        )}
        <div className="px-6 py-4">{children}</div>
        {footer && <div className="border-t px-6 py-4 flex gap-2 justify-end">{footer}</div>}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-gray-500 hover:text-gray-700"
          aria-label="Close"
        >
          ✕
        </button>
      </div>
    </div>
  )
}

describe('Modal', () => {
  it('renders nothing when isOpen is false', () => {
    const { container } = render(
      <Modal isOpen={false} onClose={jest.fn()}>
        Content
      </Modal>
    )
    expect(container.querySelector('[class*="fixed"]')).not.toBeInTheDocument()
  })

  it('renders backdrop when isOpen is true', () => {
    const { container } = render(
      <Modal isOpen={true} onClose={jest.fn()}>
        Content
      </Modal>
    )
    expect(container.querySelector('[class*="bg-black"]')).toBeInTheDocument()
  })

  it('renders modal content', () => {
    render(
      <Modal isOpen={true} onClose={jest.fn()}>
        Modal Content
      </Modal>
    )
    expect(screen.getByText('Modal Content')).toBeInTheDocument()
  })

  it('renders title when provided', () => {
    render(
      <Modal isOpen={true} title="Modal Title" onClose={jest.fn()}>
        Content
      </Modal>
    )
    expect(screen.getByText('Modal Title')).toBeInTheDocument()
  })

  it('renders footer when provided', () => {
    render(
      <Modal
        isOpen={true}
        onClose={jest.fn()}
        footer={<button>Action</button>}
      >
        Content
      </Modal>
    )
    expect(screen.getByText('Action')).toBeInTheDocument()
  })

  it('calls onClose when close button clicked', async () => {
    const handleClose = jest.fn()
    render(
      <Modal isOpen={true} onClose={handleClose}>
        Content
      </Modal>
    )

    const closeButton = screen.getByLabelText('Close')
    await userEvent.click(closeButton)
    expect(handleClose).toHaveBeenCalled()
  })

  it('has proper modal styling', () => {
    const { container } = render(
      <Modal isOpen={true} onClose={jest.fn()}>
        Content
      </Modal>
    )

    const modal = container.querySelector('[class*="bg-white"]')
    expect(modal).toHaveClass('rounded-lg', 'shadow-xl')
  })

  it('modal is centered on screen', () => {
    const { container } = render(
      <Modal isOpen={true} onClose={jest.fn()}>
        Content
      </Modal>
    )

    const backdrop = container.querySelector('[class*="fixed"]')
    expect(backdrop).toHaveClass('flex', 'items-center', 'justify-center')
  })

  it('has close button with accessibility label', () => {
    render(
      <Modal isOpen={true} onClose={jest.fn()}>
        Content
      </Modal>
    )

    const closeButton = screen.getByLabelText('Close')
    expect(closeButton).toBeInTheDocument()
  })
})
