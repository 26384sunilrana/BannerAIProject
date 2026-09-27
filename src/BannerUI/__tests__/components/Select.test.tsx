import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Select, SelectOption } from '@/components/Common/Select'

describe('Select', () => {
  const options: SelectOption[] = [
    { value: '1', label: 'Option 1' },
    { value: '2', label: 'Option 2' },
    { value: '3', label: 'Option 3' },
  ]

  it('renders select element', () => {
    const { container } = render(<Select options={options} />)
    const select = container.querySelector('select')
    expect(select).toBeInTheDocument()
  })

  it('renders with label when provided', () => {
    render(<Select label="Choose option" options={options} id="select-1" />)
    expect(screen.getByText('Choose option')).toBeInTheDocument()
    expect(screen.getByLabelText('Choose option')).toBeInTheDocument()
  })

  it('renders default placeholder option', () => {
    render(<Select options={options} />)
    expect(screen.getByText('Select an option')).toBeInTheDocument()
  })

  it('renders all provided options', () => {
    render(<Select options={options} />)
    expect(screen.getByText('Option 1')).toBeInTheDocument()
    expect(screen.getByText('Option 2')).toBeInTheDocument()
    expect(screen.getByText('Option 3')).toBeInTheDocument()
  })

  it('handles change event', async () => {
    const handleChange = jest.fn()
    const { container } = render(
      <Select options={options} onChange={handleChange} />
    )
    const select = container.querySelector('select') as HTMLSelectElement

    await userEvent.selectOptions(select, '2')
    expect(select.value).toBe('2')
    expect(handleChange).toHaveBeenCalled()
  })

  it('displays error message when error prop is provided', () => {
    render(<Select error="This field is required" options={options} />)
    expect(screen.getByText('This field is required')).toBeInTheDocument()
  })

  it('applies error styling when error is present', () => {
    const { container } = render(
      <Select error="Error message" options={options} />
    )
    const select = container.querySelector('select')
    expect(select).toHaveClass('border-red-500')
  })

  it('displays helper text when no error exists', () => {
    render(
      <Select helperText="Choose wisely" options={options} />
    )
    expect(screen.getByText('Choose wisely')).toBeInTheDocument()
  })

  it('hides helper text when error is present', () => {
    render(
      <Select
        error="Invalid"
        helperText="This should not appear"
        options={options}
      />
    )
    expect(screen.queryByText('This should not appear')).not.toBeInTheDocument()
  })

  it('disables select when disabled prop is true', async () => {
    const { container } = render(<Select disabled options={options} />)
    const select = container.querySelector('select') as HTMLSelectElement
    expect(select).toBeDisabled()
  })

  it('applies custom className', () => {
    const { container } = render(
      <Select options={options} className="custom-class" />
    )
    const select = container.querySelector('select')
    expect(select).toHaveClass('custom-class')
  })

  it('renders with border styling', () => {
    const { container } = render(<Select options={options} />)
    const select = container.querySelector('select')
    expect(select).toHaveClass('border-2', 'rounded-lg')
  })

  it('applies blue focus border when no error', () => {
    const { container } = render(<Select options={options} />)
    const select = container.querySelector('select')
    expect(select).toHaveClass('focus:border-blue-500')
  })

  it('applies red focus border when error exists', () => {
    const { container } = render(<Select error="Error" options={options} />)
    const select = container.querySelector('select')
    expect(select).toHaveClass('focus:border-red-600')
  })

  it('forwards ref correctly', () => {
    const ref = React.createRef<HTMLSelectElement>()
    render(<Select ref={ref} options={options} />)
    expect(ref.current).toBeInstanceOf(HTMLSelectElement)
  })

  it('renders with numeric values', () => {
    const numericOptions: SelectOption[] = [
      { value: 1, label: 'One' },
      { value: 2, label: 'Two' },
    ]
    const { container } = render(<Select options={numericOptions} />)
    const select = container.querySelector('select') as HTMLSelectElement

    const options_elem = select.querySelectorAll('option')
    expect(options_elem[1].value).toBe('1')
    expect(options_elem[2].value).toBe('2')
  })

  it('handles selection with default value', () => {
    const { container } = render(
      <Select options={options} defaultValue="2" />
    )
    const select = container.querySelector('select') as HTMLSelectElement
    expect(select.value).toBe('2')
  })

  it('renders with proper label styling', () => {
    render(<Select label="Test Label" options={options} id="test-select" />)
    const label = screen.getByText('Test Label')
    expect(label).toHaveClass('text-sm', 'font-medium')
  })

  it('empty options array renders only placeholder', () => {
    render(<Select options={[]} />)
    const options_elem = screen.getByText('Select an option')
    expect(options_elem).toBeInTheDocument()
  })
})
