import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Toast, ToastMessage } from '@/components/Common/Toast'

describe('Toast', () => {
  it('renders empty list when no messages', () => {
    const { container } = render(<Toast messages={[]} onRemove={jest.fn()} />)
    const toastContainer = container.querySelector('[class*="fixed"]')
    expect(toastContainer?.children.length).toBe(0)
  })

  it('renders single toast message', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Operation successful' }
    ]
    render(<Toast messages={messages} onRemove={jest.fn()} />)
    expect(screen.getByText('Operation successful')).toBeInTheDocument()
  })

  it('renders multiple toast messages', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Success message' },
      { id: '2', type: 'error', message: 'Error message' },
    ]
    render(<Toast messages={messages} onRemove={jest.fn()} />)
    expect(screen.getByText('Success message')).toBeInTheDocument()
    expect(screen.getByText('Error message')).toBeInTheDocument()
  })

  it('applies success styling to success toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Success' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const toast = container.querySelector('[class*="bg-green-50"]')
    expect(toast).toBeInTheDocument()
  })

  it('applies error styling to error toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'error', message: 'Error' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const toast = container.querySelector('[class*="bg-red-50"]')
    expect(toast).toBeInTheDocument()
  })

  it('applies warning styling to warning toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'warning', message: 'Warning' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const toast = container.querySelector('[class*="bg-yellow-50"]')
    expect(toast).toBeInTheDocument()
  })

  it('applies info styling to info toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'info', message: 'Info' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const toast = container.querySelector('[class*="bg-blue-50"]')
    expect(toast).toBeInTheDocument()
  })

  it('displays icon for success toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Success' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    expect(container.textContent).toContain('✓')
  })

  it('displays icon for error toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'error', message: 'Error' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    expect(container.textContent).toContain('✕')
  })

  it('displays icon for warning toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'warning', message: 'Warning' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    expect(container.textContent).toContain('⚠')
  })

  it('displays icon for info toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'info', message: 'Info' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    expect(container.textContent).toContain('ℹ')
  })

  it('calls onRemove when close button is clicked', async () => {
    const onRemove = jest.fn()
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Message' }
    ]
    render(<Toast messages={messages} onRemove={onRemove} />)

    const closeButtons = screen.getAllByLabelText('Close')
    await userEvent.click(closeButtons[0])
    expect(onRemove).toHaveBeenCalledWith('1')
  })

  it('auto-dismisses after duration', async () => {
    jest.useFakeTimers()
    const onRemove = jest.fn()
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Message', duration: 3000 }
    ]
    render(<Toast messages={messages} onRemove={onRemove} />)

    jest.advanceTimersByTime(3000)
    await waitFor(() => {
      expect(onRemove).toHaveBeenCalledWith('1')
    })

    jest.useRealTimers()
  })

  it('uses default duration when not specified', async () => {
    jest.useFakeTimers()
    const onRemove = jest.fn()
    const messages: ToastMessage[] = [
      { id: '1', type: 'info', message: 'Message' }
    ]
    render(<Toast messages={messages} onRemove={onRemove} />)

    // Default is 5000ms
    jest.advanceTimersByTime(5000)
    await waitFor(() => {
      expect(onRemove).toHaveBeenCalledWith('1')
    })

    jest.useRealTimers()
  })

  it('renders toast with proper styling structure', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'info', message: 'Test message' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const toast = container.querySelector('[role="alert"]')
    expect(toast).toHaveClass('flex', 'items-center', 'gap-3', 'p-4', 'rounded-lg')
  })

  it('stacks multiple toasts correctly', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'First' },
      { id: '2', type: 'error', message: 'Second' },
      { id: '3', type: 'info', message: 'Third' },
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const alerts = container.querySelectorAll('[role="alert"]')
    expect(alerts.length).toBe(3)
  })

  it('has close button for each toast', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Message 1' },
      { id: '2', type: 'error', message: 'Message 2' },
    ]
    render(<Toast messages={messages} onRemove={jest.fn()} />)
    const closeButtons = screen.getAllByLabelText('Close')
    expect(closeButtons.length).toBe(2)
  })

  it('renders with max-width constraint', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Long message that should be constrained' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const container_elem = container.querySelector('[class*="max-w-md"]')
    expect(container_elem).toBeInTheDocument()
  })

  it('message is flexible with content', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'info', message: 'Test' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const message = container.querySelector('[class*="flex-1"]')
    expect(message).toBeInTheDocument()
  })

  it('renders with proper z-index', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Message' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const toastContainer = container.querySelector('[class*="z-50"]')
    expect(toastContainer).toBeInTheDocument()
  })

  it('close button has hover effect', () => {
    const messages: ToastMessage[] = [
      { id: '1', type: 'success', message: 'Message' }
    ]
    const { container } = render(<Toast messages={messages} onRemove={jest.fn()} />)
    const closeButton = container.querySelector('[class*="hover:opacity"]')
    expect(closeButton).toBeInTheDocument()
  })
})
