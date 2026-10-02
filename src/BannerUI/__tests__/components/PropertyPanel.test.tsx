import React from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { PropertyPanel } from '@/components/PropertyPanel/PropertyPanel'
import { BannerComponent } from '@/types/banner'

describe('PropertyPanel', () => {
  const mockComponent: BannerComponent = {
    id: 'comp-1',
    bannerId: 'banner-1',
    type: 'text',
    x: 50,
    y: 50,
    width: 200,
    height: 100,
    zIndex: 10,
    rotation: 0,
    opacity: 1,
    isVisible: true,
    data: {
      content: 'Test Text',
      fontSize: 24,
      fontFamily: 'Arial',
      fontWeight: '400',
      color: '#000000',
      textAlign: 'left',
      lineHeight: 1.5,
    },
    effects: [],
    createdAt: '2026-09-26T00:00:00Z',
    updatedAt: '2026-09-26T00:00:00Z',
  }

  const defaultProps = {
    selectedComponent: mockComponent,
    onPropertyChange: jest.fn(),
    onDeleteComponent: jest.fn(),
  }

  it('shows message when no component selected', () => {
    render(
      <PropertyPanel
        {...defaultProps}
        selectedComponent={null}
      />
    )
    expect(screen.getByText(/Select a component/i)).toBeInTheDocument()
  })

  it('displays component properties when selected', () => {
    render(<PropertyPanel {...defaultProps} />)
    expect(screen.getByLabelText('X')).toHaveValue(50)
    expect(screen.getByLabelText('Y')).toHaveValue(50)
    expect(screen.getByLabelText('Width')).toHaveValue(200)
    expect(screen.getByLabelText('Height')).toHaveValue(100)
  })

  it('displays component type', () => {
    render(<PropertyPanel {...defaultProps} />)
    expect(screen.getByText(/Type:/i)).toBeInTheDocument()
    expect(screen.getByText('text')).toBeInTheDocument()
  })

  it('calls onPropertyChange when position changes', async () => {
    const { rerender } = render(<PropertyPanel {...defaultProps} />)
    const xInput = screen.getByLabelText('X')

    fireEvent.change(xInput, { target: { value: '100' } })

    expect(defaultProps.onPropertyChange).toHaveBeenCalledWith('x', 100)
  })

  it('displays text-specific properties', () => {
    render(<PropertyPanel {...defaultProps} />)
    expect(screen.getByLabelText('Content')).toHaveValue('Test Text')
    expect(screen.getByLabelText('Font Size')).toHaveValue(24)
    expect(screen.getByLabelText('Color')).toBeInTheDocument()
  })

  it('displays appearance controls', () => {
    render(<PropertyPanel {...defaultProps} />)
    expect(screen.getByLabelText('Z-Index')).toHaveValue(10)
    expect(screen.getByLabelText('Rotation (°)')).toHaveValue(0)
    expect(screen.getByLabelText('Opacity')).toHaveValue(1)
  })

  it('toggles visibility', async () => {
    render(<PropertyPanel {...defaultProps} />)
    const visibilityCheckbox = screen.getByRole('checkbox', { name: /Visible/i })

    expect(visibilityCheckbox).toBeChecked()

    await userEvent.click(visibilityCheckbox)

    expect(defaultProps.onPropertyChange).toHaveBeenCalledWith('isVisible', false)
  })

  it('calls onDeleteComponent when delete button clicked', async () => {
    render(<PropertyPanel {...defaultProps} />)
    const deleteButton = screen.getByRole('button', { name: /Delete Component/i })

    await userEvent.click(deleteButton)

    expect(defaultProps.onDeleteComponent).toHaveBeenCalled()
  })

  it('shows graphics properties for graphics type', () => {
    const graphicsComponent = { ...mockComponent, type: 'graphics' as const }
    render(
      <PropertyPanel
        {...defaultProps}
        selectedComponent={graphicsComponent}
      />
    )
    expect(screen.getByLabelText('Fill Color')).toBeInTheDocument()
    expect(screen.getByLabelText('Stroke Color')).toBeInTheDocument()
    expect(screen.getByLabelText('Stroke Width')).toBeInTheDocument()
  })

  it('validates property changes', async () => {
    render(<PropertyPanel {...defaultProps} />)
    const widthInput = screen.getByLabelText('Width')

    await userEvent.clear(widthInput)
    await userEvent.type(widthInput, '5')

    // Should not call onPropertyChange for invalid value
    expect(defaultProps.onPropertyChange).not.toHaveBeenCalledWith('width', 5)
  })
})
