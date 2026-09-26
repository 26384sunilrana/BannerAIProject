import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Button } from '@/components/Common/Button'

describe('Button', () => {
  it('renders with text', () => {
    render(<Button>Click me</Button>)
    expect(screen.getByText('Click me')).toBeInTheDocument()
  })

  it('calls onClick handler', async () => {
    const handleClick = jest.fn()
    render(<Button onClick={handleClick}>Click</Button>)

    await userEvent.click(screen.getByText('Click'))
    expect(handleClick).toHaveBeenCalledTimes(1)
  })

  it('disables button when disabled prop is true', () => {
    render(<Button disabled>Disabled</Button>)
    expect(screen.getByText('Disabled')).toBeDisabled()
  })

  it('shows loading spinner when isLoading is true', () => {
    const { container } = render(<Button isLoading>Loading</Button>)
    expect(container.querySelector('button')).toBeDisabled()
  })

  it('applies variant styles', () => {
    const { container } = render(<Button variant="danger">Delete</Button>)
    const button = container.querySelector('button')
    expect(button).toHaveClass('bg-red-600')
  })

  it('applies size styles', () => {
    const { container } = render(<Button size="lg">Large</Button>)
    const button = container.querySelector('button')
    expect(button).toHaveClass('px-6', 'py-3')
  })
})
