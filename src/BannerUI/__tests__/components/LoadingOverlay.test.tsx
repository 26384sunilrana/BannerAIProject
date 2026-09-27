import React from 'react'
import { render, screen } from '@testing-library/react'
import { LoadingOverlay } from '@/components/Common/LoadingOverlay'

describe('LoadingOverlay', () => {
  it('renders nothing when isVisible is false', () => {
    const { container } = render(<LoadingOverlay isVisible={false} />)
    expect(container.querySelector('[class*="fixed"]')).not.toBeInTheDocument()
  })

  it('renders overlay when isVisible is true', () => {
    const { container } = render(<LoadingOverlay isVisible={true} />)
    const overlay = container.querySelector('[class*="fixed"]')
    expect(overlay).toBeInTheDocument()
  })

  it('displays loading spinner', () => {
    const { container } = render(<LoadingOverlay isVisible={true} />)
    const spinner = container.querySelector('[class*="animate-spin"]')
    expect(spinner).toBeInTheDocument()
  })

  it('displays custom message when provided', () => {
    render(<LoadingOverlay isVisible={true} message="Loading data..." />)
    expect(screen.getByText('Loading data...')).toBeInTheDocument()
  })

  it('does not display message when not provided', () => {
    const { container } = render(<LoadingOverlay isVisible={true} />)
    const messageElement = container.querySelector('p')
    expect(messageElement).not.toBeInTheDocument()
  })

  it('displays progress bar when progress is provided', () => {
    const { container } = render(<LoadingOverlay isVisible={true} progress={50} />)
    const progressBar = container.querySelector('[class*="bg-blue-600"]')
    expect(progressBar).toBeInTheDocument()
  })

  it('sets progress bar width correctly', () => {
    const { container } = render(<LoadingOverlay isVisible={true} progress={75} />)
    const progressFill = container.querySelector('[style*="width"]')
    expect(progressFill).toHaveStyle({ width: '75%' })
  })

  it('displays progress percentage text', () => {
    render(<LoadingOverlay isVisible={true} progress={60} />)
    expect(screen.getByText('60%')).toBeInTheDocument()
  })

  it('handles zero progress', () => {
    render(<LoadingOverlay isVisible={true} progress={0} />)
    expect(screen.getByText('0%')).toBeInTheDocument()
  })

  it('handles 100% progress', () => {
    render(<LoadingOverlay isVisible={true} progress={100} />)
    expect(screen.getByText('100%')).toBeInTheDocument()
  })

  it('renders with proper backdrop styling', () => {
    const { container } = render(<LoadingOverlay isVisible={true} />)
    const backdrop = container.querySelector('[class*="bg-black"]')
    expect(backdrop).toBeInTheDocument()
    expect(backdrop).toHaveClass('bg-opacity-30')
  })

  it('renders loading box with proper styling', () => {
    const { container } = render(<LoadingOverlay isVisible={true} />)
    const box = container.querySelector('[class*="bg-white"]')
    expect(box).toBeInTheDocument()
    expect(box).toHaveClass('rounded-lg', 'shadow-xl')
  })

  it('centers content', () => {
    const { container } = render(<LoadingOverlay isVisible={true} />)
    const container_elem = container.querySelector('[class*="flex items-center"]')
    expect(container_elem).toBeInTheDocument()
  })

  it('handles message with progress together', () => {
    render(
      <LoadingOverlay
        isVisible={true}
        message="Uploading files..."
        progress={45}
      />
    )
    expect(screen.getByText('Uploading files...')).toBeInTheDocument()
    expect(screen.getByText('45%')).toBeInTheDocument()
  })

  it('applies z-index for layering', () => {
    const { container } = render(<LoadingOverlay isVisible={true} />)
    const overlay = container.querySelector('[class*="z-50"]')
    expect(overlay).toBeInTheDocument()
  })

  it('message has proper styling', () => {
    render(<LoadingOverlay isVisible={true} message="Processing..." />)
    const message = screen.getByText('Processing...')
    expect(message).toHaveClass('text-gray-700', 'text-center')
  })

  it('progress bar has proper styling', () => {
    const { container } = render(<LoadingOverlay isVisible={true} progress={50} />)
    const progressContainer = container.querySelector('[class*="bg-gray-200"]')
    expect(progressContainer).toHaveClass('h-2', 'rounded-full')
  })
})
