import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ErrorBoundary } from '@/components/Common/ErrorBoundary'

// Component that throws an error
function ThrowError() {
  throw new Error('Test error message')
}

// Component that renders normally
function SafeComponent() {
  return <div>Safe content</div>
}

describe('ErrorBoundary', () => {
  beforeEach(() => {
    // Suppress console.error for these tests
    jest.spyOn(console, 'error').mockImplementation(() => {})
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  it('renders children successfully when no error occurs', () => {
    render(
      <ErrorBoundary>
        <SafeComponent />
      </ErrorBoundary>
    )
    expect(screen.getByText('Safe content')).toBeInTheDocument()
  })

  it('catches JavaScript errors in child components', () => {
    render(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    )
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('displays error message from caught error', () => {
    render(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    )
    expect(screen.getByText('Test error message')).toBeInTheDocument()
  })

  it('shows reload button in error state', () => {
    render(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    )
    const reloadButton = screen.getByText('Reload Page')
    expect(reloadButton).toBeInTheDocument()
    expect(reloadButton).toHaveClass('bg-red-600')
  })

  it('renders fallback UI when provided', () => {
    const fallback = <div>Custom error fallback</div>
    render(
      <ErrorBoundary fallback={fallback}>
        <ThrowError />
      </ErrorBoundary>
    )
    expect(screen.getByText('Custom error fallback')).toBeInTheDocument()
  })

  it('calls onError callback when error is caught', () => {
    const onError = jest.fn()
    render(
      <ErrorBoundary onError={onError}>
        <ThrowError />
      </ErrorBoundary>
    )
    expect(onError).toHaveBeenCalled()
    expect(onError).toHaveBeenCalledWith(
      expect.any(Error),
      expect.objectContaining({ componentStack: expect.any(String) })
    )
  })

  it('logs error to console', () => {
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {})
    render(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    )
    expect(consoleSpy).toHaveBeenCalledWith(
      expect.stringContaining('Error caught by boundary'),
      expect.any(Error),
      expect.any(Object)
    )
    consoleSpy.mockRestore()
  })

  it('renders default error UI with proper styling', () => {
    const { container } = render(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    )
    const errorContainer = container.querySelector('[class*="min-h-screen"]')
    expect(errorContainer).toBeInTheDocument()
    expect(errorContainer).toHaveClass('bg-red-50')
  })

  it('handles multiple error instances separately', () => {
    const { rerender } = render(
      <ErrorBoundary>
        <SafeComponent />
      </ErrorBoundary>
    )
    expect(screen.getByText('Safe content')).toBeInTheDocument()

    rerender(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    )
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('error message displays in readable format', () => {
    render(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    )
    const errorText = screen.getByText('Test error message')
    expect(errorText).toHaveClass('text-gray-600')
  })

  it('reload button has proper styling', () => {
    render(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    )
    const reloadButton = screen.getByText('Reload Page')
    expect(reloadButton).toHaveClass('px-4', 'py-2', 'text-white', 'rounded-lg')
  })
})
