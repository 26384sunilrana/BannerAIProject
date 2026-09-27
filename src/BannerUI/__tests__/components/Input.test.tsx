import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Input } from '@/components/Common/Input'

describe('Input', () => {
  it('renders input element', () => {
    const { container } = render(<Input />)
    const input = container.querySelector('input')
    expect(input).toBeInTheDocument()
  })

  it('renders with label when provided', () => {
    render(<Input label="Username" id="username" />)
    expect(screen.getByText('Username')).toBeInTheDocument()
    expect(screen.getByLabelText('Username')).toBeInTheDocument()
  })

  it('renders with placeholder text', () => {
    render(<Input placeholder="Enter text" />)
    expect(screen.getByPlaceholderText('Enter text')).toBeInTheDocument()
  })

  it('renders with initial value', () => {
    const { container } = render(<Input value="initial" readOnly />)
    const input = container.querySelector('input') as HTMLInputElement
    expect(input.value).toBe('initial')
  })

  it('handles onChange events', async () => {
    const { container } = render(<Input />)
    const input = container.querySelector('input') as HTMLInputElement

    await userEvent.type(input, 'hello')
    expect(input.value).toBe('hello')
  })

  it('displays error message when error prop is provided', () => {
    render(<Input error="This field is required" />)
    expect(screen.getByText('This field is required')).toBeInTheDocument()
  })

  it('applies error styling when error is present', () => {
    const { container } = render(<Input error="Error message" />)
    const input = container.querySelector('input')
    expect(input).toHaveClass('border-red-500')
  })

  it('displays helper text when no error exists', () => {
    render(<Input helperText="Must be at least 8 characters" />)
    expect(screen.getByText('Must be at least 8 characters')).toBeInTheDocument()
  })

  it('hides helper text when error is present', () => {
    render(
      <Input
        error="Invalid"
        helperText="Helper text should not appear with error"
      />
    )
    expect(screen.queryByText('Helper text should not appear with error')).not.toBeInTheDocument()
  })

  it('disables input when disabled prop is true', async () => {
    const { container } = render(<Input disabled />)
    const input = container.querySelector('input') as HTMLInputElement
    expect(input).toBeDisabled()
    expect(input).toHaveClass('disabled:bg-gray-100')
  })

  it('applies custom className', () => {
    const { container } = render(<Input className="custom-class" />)
    const input = container.querySelector('input')
    expect(input).toHaveClass('custom-class')
  })

  it('handles focus state', async () => {
    const { container } = render(<Input />)
    const input = container.querySelector('input') as HTMLInputElement

    await userEvent.click(input)
    expect(input).toHaveFocus()
  })

  it('renders with border styling', () => {
    const { container } = render(<Input />)
    const input = container.querySelector('input')
    expect(input).toHaveClass('border-2', 'rounded-lg')
  })

  it('applies blue focus border when no error', () => {
    const { container } = render(<Input />)
    const input = container.querySelector('input')
    expect(input).toHaveClass('focus:border-blue-500')
  })

  it('applies red focus border when error exists', () => {
    const { container } = render(<Input error="Error" />)
    const input = container.querySelector('input')
    expect(input).toHaveClass('focus:border-red-600')
  })

  it('supports input types', () => {
    const { container } = render(<Input type="email" />)
    const input = container.querySelector('input') as HTMLInputElement
    expect(input.type).toBe('email')
  })

  it('supports required attribute', async () => {
    const { container } = render(<Input required />)
    const input = container.querySelector('input') as HTMLInputElement
    expect(input.required).toBe(true)
  })

  it('forwards ref correctly', () => {
    const ref = React.createRef<HTMLInputElement>()
    render(<Input ref={ref} />)
    expect(ref.current).toBeInstanceOf(HTMLInputElement)
  })
})
