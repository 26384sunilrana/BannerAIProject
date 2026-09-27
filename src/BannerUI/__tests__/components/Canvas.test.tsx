import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Canvas } from '@/components/Canvas/Canvas'
import { BannerComponent } from '@/types/banner'

describe('Canvas', () => {
  const mockComponent: BannerComponent = {
    id: 'comp-1',
    bannerId: 'banner-1',
    type: 'text',
    x: 50,
    y: 50,
    width: 200,
    height: 100,
    zIndex: 0,
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
    components: [mockComponent],
    backgroundColor: '#ffffff',
    width: 1200,
    height: 600,
    isPreviewMode: false,
    selectedComponentId: null,
    onComponentSelect: jest.fn(),
    onComponentMove: jest.fn(),
    onComponentResize: jest.fn(),
  }

  it('renders canvas container', () => {
    const { container } = render(<Canvas {...defaultProps} />)
    expect(container.querySelector('[class*="wrapper"]')).toBeInTheDocument()
  })

  it('renders components on canvas', () => {
    render(<Canvas {...defaultProps} />)
    expect(screen.getByText('Test Text')).toBeInTheDocument()
  })

  it('calls onComponentSelect when component clicked', async () => {
    const { rerender } = render(<Canvas {...defaultProps} />)

    const textElement = screen.getByText('Test Text')
    await userEvent.click(textElement)

    expect(defaultProps.onComponentSelect).toHaveBeenCalledWith('comp-1')
  })

  it('shows zoom controls in edit mode', () => {
    render(<Canvas {...defaultProps} />)
    expect(screen.getByText('+')).toBeInTheDocument()
    expect(screen.getByText('−')).toBeInTheDocument()
    expect(screen.getByText('Fit')).toBeInTheDocument()
    expect(screen.getByText('Reset')).toBeInTheDocument()
  })

  it('hides zoom controls in preview mode', () => {
    render(<Canvas {...defaultProps} isPreviewMode={true} />)
    expect(screen.queryByText('+')).not.toBeInTheDocument()
  })

  it('hides resize handles in preview mode', () => {
    const { container } = render(<Canvas {...defaultProps} isPreviewMode={true} />)
    const handles = container.querySelectorAll('[class*="handle"]')
    expect(handles.length).toBe(0)
  })

  it('shows selected component with outline', () => {
    const { container } = render(
      <Canvas {...defaultProps} selectedComponentId="comp-1" />
    )
    const selected = container.querySelector('[class*="selected"]')
    expect(selected).toBeInTheDocument()
  })

  it('renders multiple components', () => {
    const multiComponent = {
      ...defaultProps,
      components: [
        mockComponent,
        { ...mockComponent, id: 'comp-2', x: 300, y: 200 },
      ],
    }
    const { container } = render(<Canvas {...multiComponent} />)
    const components = container.querySelectorAll('[class*="component"]')
    expect(components.length).toBeGreaterThanOrEqual(2)
  })

  it('handles component visibility', () => {
    const invisibleComponent = { ...mockComponent, isVisible: false }
    render(
      <Canvas
        {...defaultProps}
        components={[invisibleComponent]}
      />
    )
    // Invisible components should still render but with opacity handling
    expect(screen.getByText('Test Text')).toBeInTheDocument()
  })
})
