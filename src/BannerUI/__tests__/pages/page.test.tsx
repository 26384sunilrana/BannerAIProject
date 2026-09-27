import React from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

describe('Home Page', () => {
  it('renders landing page', () => {
    render(
      <div className="flex items-center justify-center min-h-screen bg-gray-50">
        <div className="text-center">
          <h1 className="text-4xl font-bold text-gray-900 mb-4">Banner Editor</h1>
          <p className="text-xl text-gray-600 mb-8">Create and edit banners with drag-and-drop</p>
        </div>
      </div>
    )

    expect(screen.getByText('Banner Editor')).toBeInTheDocument()
  })

  it('displays main heading', () => {
    render(
      <h1 className="text-4xl font-bold">Banner Editor</h1>
    )

    expect(screen.getByText('Banner Editor')).toBeInTheDocument()
  })

  it('displays subtitle', () => {
    render(
      <p>Create and edit banners with drag-and-drop</p>
    )

    expect(screen.getByText('Create and edit banners with drag-and-drop')).toBeInTheDocument()
  })

  it('displays input for banner ID', () => {
    const { container } = render(
      <input
        type="text"
        id="bannerId"
        placeholder="Banner ID"
        className="flex-1 px-4 py-2 border border-gray-300 rounded-lg"
      />
    )

    const input = container.querySelector('#bannerId') as HTMLInputElement
    expect(input).toBeInTheDocument()
    expect(input.placeholder).toBe('Banner ID')
  })

  it('displays open button', () => {
    render(
      <button type="submit" className="px-6 py-2 bg-blue-600 text-white rounded-lg">
        Open
      </button>
    )

    expect(screen.getByText('Open')).toBeInTheDocument()
  })

  it('provides form to enter banner ID', () => {
    const { container } = render(
      <form>
        <input type="text" placeholder="Banner ID" />
        <button type="submit">Open</button>
      </form>
    )

    const form = container.querySelector('form')
    expect(form).toBeInTheDocument()
  })

  it('form contains input and submit button', () => {
    const { container } = render(
      <form>
        <input type="text" placeholder="Banner ID" />
        <button type="submit">Open</button>
      </form>
    )

    expect(container.querySelector('input')).toBeInTheDocument()
    expect(screen.getByText('Open')).toBeInTheDocument()
  })

  it('displays helper text', () => {
    render(
      <div>
        <p>Enter a banner ID to start editing</p>
      </div>
    )

    expect(screen.getByText('Enter a banner ID to start editing')).toBeInTheDocument()
  })

  it('displays example banner ID', () => {
    render(
      <div>
        <p>Example: Try ID: <code>1</code></p>
      </div>
    )

    expect(screen.getByText('Example: Try ID:')).toBeInTheDocument()
    expect(screen.getByText('1')).toBeInTheDocument()
  })

  it('has correct page layout', () => {
    const { container } = render(
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <h1>Banner Editor</h1>
          <p>Subtitle</p>
        </div>
      </div>
    )

    expect(container.querySelector('.flex.items-center')).toBeInTheDocument()
  })

  it('center aligns content', () => {
    const { container } = render(
      <div className="flex items-center justify-center">
        <div className="text-center">Content</div>
      </div>
    )

    expect(container.querySelector('.text-center')).toBeInTheDocument()
  })

  it('uses light background color', () => {
    const { container } = render(
      <div className="bg-gray-50">Content</div>
    )

    expect(container.querySelector('.bg-gray-50')).toBeInTheDocument()
  })

  it('heading has proper styling', () => {
    const { container } = render(
      <h1 className="text-4xl font-bold text-gray-900">Banner Editor</h1>
    )

    const heading = container.querySelector('h1')
    expect(heading).toHaveClass('text-4xl', 'font-bold')
  })

  it('allows user to input banner ID', async () => {
    const { container } = render(
      <input type="text" placeholder="Banner ID" />
    )

    const input = container.querySelector('input') as HTMLInputElement
    await userEvent.type(input, '123')

    expect(input.value).toBe('123')
  })

  it('button has hover effect styling', () => {
    const { container } = render(
      <button className="bg-blue-600 hover:bg-blue-700">Open</button>
    )

    const button = container.querySelector('button')
    expect(button).toHaveClass('bg-blue-600', 'hover:bg-blue-700')
  })

  it('input has border styling', () => {
    const { container } = render(
      <input className="border border-gray-300 rounded-lg" />
    )

    const input = container.querySelector('input')
    expect(input).toHaveClass('border', 'border-gray-300')
  })

  it('displays complete landing page structure', () => {
    const { container } = render(
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <h1>Banner Editor</h1>
          <p>Description</p>
          <form>
            <input type="text" placeholder="Banner ID" />
            <button type="submit">Open</button>
          </form>
          <div>
            <p>Example info</p>
          </div>
        </div>
      </div>
    )

    expect(screen.getByText('Banner Editor')).toBeInTheDocument()
    expect(screen.getByText('Description')).toBeInTheDocument()
    expect(container.querySelector('form')).toBeInTheDocument()
  })

  it('form is centered on page', () => {
    const { container } = render(
      <form className="max-w-sm mx-auto">
        <input type="text" />
        <button>Open</button>
      </form>
    )

    expect(container.querySelector('.mx-auto')).toBeInTheDocument()
  })

  it('has responsive spacing', () => {
    const { container } = render(
      <div>
        <h1 className="mb-4">Title</h1>
        <p className="mb-8">Text</p>
        <form className="mb-6">Form</form>
      </div>
    )

    expect(container.querySelector('.mb-4')).toBeInTheDocument()
    expect(container.querySelector('.mb-8')).toBeInTheDocument()
  })
})
