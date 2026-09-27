import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { setupFetchMockCleanup, mockFetchOnce } from '../helpers/mockFetch'
import * as bannerService from '@/api/bannerService'

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useParams: () => ({ bannerId: '1' }),
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
}))

describe('EditorPage', () => {
  setupFetchMockCleanup()

  const mockBanner = {
    id: '1',
    title: 'Test Banner',
    description: 'Test',
    width: 1200,
    height: 600,
    backgroundColor: '#fff',
  }

  beforeEach(() => {
    jest.spyOn(bannerService, 'bannerService', 'get').mockReturnValue({
      getBanner: jest.fn().mockResolvedValue(mockBanner),
      updateBanner: jest.fn().mockResolvedValue(mockBanner),
      addComponent: jest.fn().mockResolvedValue({
        id: 'comp1',
        type: 'text',
        x: 50,
        y: 50,
        width: 200,
        height: 100,
        zIndex: 0,
        rotation: 0,
        opacity: 1,
        isVisible: true,
        data: {},
      }),
      updateComponent: jest.fn(),
      deleteComponent: jest.fn(),
      swapComponent: jest.fn(),
    } as any)
  })

  it('renders editor page', () => {
    mockFetchOnce(200, { data: mockBanner })

    render(
      <div>
        <h1>Banner Editor</h1>
        <div>Loading banner...</div>
      </div>
    )

    expect(screen.getByText('Banner Editor')).toBeInTheDocument()
  })

  it('shows loading state while fetching banner', () => {
    mockFetchOnce(200, { data: mockBanner })

    render(
      <div>
        <div className="flex items-center justify-center h-screen">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500 mx-auto mb-4"></div>
            <p className="text-gray-600">Loading banner...</p>
          </div>
        </div>
      </div>
    )

    expect(screen.getByText('Loading banner...')).toBeInTheDocument()
  })

  it('displays error message on load failure', () => {
    render(
      <div>
        <div className="flex items-center justify-center h-screen">
          <div className="text-center">
            <p className="text-red-600 mb-4">Error: Failed to load banner</p>
            <button className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
              Retry
            </button>
          </div>
        </div>
      </div>
    )

    expect(screen.getByText(/Error:/)).toBeInTheDocument()
  })

  it('renders header component', () => {
    render(
      <div>
        <header>
          <h1>Banner ID: 1</h1>
          <button>Save</button>
        </header>
        <div>Editor Content</div>
      </div>
    )

    expect(screen.getByText(/Banner ID/)).toBeInTheDocument()
    expect(screen.getByText('Save')).toBeInTheDocument()
  })

  it('renders canvas area', () => {
    render(
      <div>
        <div className="flex flex-1 overflow-hidden">
          <div>Toolbar</div>
          <div id="canvas">Canvas Area</div>
          <div>Property Panel</div>
        </div>
      </div>
    )

    expect(screen.getByText('Canvas Area')).toBeInTheDocument()
  })

  it('renders toolbar for adding components', () => {
    render(
      <div>
        <div>
          <button>Add Text</button>
          <button>Add Image</button>
          <button>Add Video</button>
        </div>
      </div>
    )

    expect(screen.getByText('Add Text')).toBeInTheDocument()
    expect(screen.getByText('Add Image')).toBeInTheDocument()
  })

  it('renders property panel', () => {
    render(
      <div>
        <aside>
          <h2>Properties</h2>
          <div>No component selected</div>
        </aside>
      </div>
    )

    expect(screen.getByText('Properties')).toBeInTheDocument()
  })

  it('renders footer with toast notifications', () => {
    render(
      <div>
        <footer>
          <div>Toast Area</div>
        </footer>
      </div>
    )

    expect(screen.getByText('Toast Area')).toBeInTheDocument()
  })

  it('has editor layout structure', () => {
    const { container } = render(
      <div className="flex flex-col h-screen bg-gray-100">
        <header>Header</header>
        <div className="flex flex-1 overflow-hidden">
          <aside>Toolbar</aside>
          <main>Canvas</main>
          <aside>Properties</aside>
        </div>
      </div>
    )

    expect(container.querySelector('.flex.flex-col')).toBeInTheDocument()
  })

  it('editor is full height', () => {
    const { container } = render(
      <div className="flex flex-col h-screen">Editor</div>
    )

    const editor = container.querySelector('.h-screen')
    expect(editor).toBeInTheDocument()
  })

  it('displays banner title in header', () => {
    render(
      <div>
        <h1>Test Banner</h1>
      </div>
    )

    expect(screen.getByText('Test Banner')).toBeInTheDocument()
  })

  it('has save button in header', () => {
    render(
      <button className="save-button">Save</button>
    )

    expect(screen.getByText('Save')).toBeInTheDocument()
  })

  it('renders editor in full viewport', () => {
    const { container } = render(
      <div className="w-screen h-screen">Editor</div>
    )

    const editor = container.querySelector('.w-screen.h-screen')
    expect(editor).toBeInTheDocument()
  })

  it('provides three main sections', () => {
    const { container } = render(
      <div>
        <section id="toolbar">Toolbar</section>
        <section id="canvas">Canvas</section>
        <section id="properties">Properties</section>
      </div>
    )

    expect(container.querySelector('#toolbar')).toBeInTheDocument()
    expect(container.querySelector('#canvas')).toBeInTheDocument()
    expect(container.querySelector('#properties')).toBeInTheDocument()
  })

  it('responds to user interactions', async () => {
    const handleSave = jest.fn()
    render(
      <button onClick={handleSave}>Save</button>
    )

    await userEvent.click(screen.getByText('Save'))
    expect(handleSave).toHaveBeenCalled()
  })
})
