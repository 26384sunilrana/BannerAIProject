import React from 'react'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('next/navigation', () => ({
  useParams: () => ({ bannerId: 'b1' }),
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  usePathname: () => '/banners/b1/editor',
}))
jest.mock('@/components/auth/RequireAuth', () => ({ RequireAuth: ({ children }: { children: React.ReactNode }) => <>{children}</> }))
jest.mock('@/api/bannerService', () => ({
  bannerService: {
    getBanner: jest.fn(),
    updateBanner: jest.fn(),
    addComponent: jest.fn(),
    updateComponent: jest.fn(),
    deleteComponent: jest.fn(),
  },
}))
jest.mock('@/api/layerService', () => ({ layerService: { reorderComponent: jest.fn() } }))
jest.mock('@/hooks/useMediaUpload', () => ({
  useMediaUpload: () => ({ upload: jest.fn(), isUploading: false, progress: 0, error: null }),
}))

import EditorPage from '@/app/banners/[bannerId]/editor/page'
import { bannerService } from '@/api/bannerService'

const banner = bannerService as jest.Mocked<typeof bannerService>

const loadedBanner = {
  id: 'b1', title: 'Summer Sale', description: 'd', width: 1200, height: 600, backgroundColor: '#ffffff', components: [],
}
const textComponent = {
  id: 'c1', bannerId: 'b1', type: 'text', x: 50, y: 50, width: 200, height: 100, zIndex: 0, rotation: 0, opacity: 1,
  isVisible: true, data: { content: 'New Text', fontSize: 24 }, effects: [],
}

beforeEach(() => {
  jest.clearAllMocks()
  banner.getBanner.mockResolvedValue(loadedBanner as never)
  banner.updateBanner.mockResolvedValue(undefined as never)
})

describe('Editor page', () => {
  it('loads the banner and shows its name, the toolbar and the property panel', async () => {
    render(<EditorPage />)

    expect(await screen.findByText('Summer Sale')).toBeInTheDocument()
    expect(banner.getBanner).toHaveBeenCalledWith('b1')
    expect(screen.getByRole('button', { name: /text/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /image/i })).toBeInTheDocument()
  })

  it('shows why the banner could not be loaded', async () => {
    banner.getBanner.mockRejectedValue({ response: { status: 403 } })
    render(<EditorPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/permission/i)
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
  })

  it('adds a text component on the free layer and selects it', async () => {
    banner.addComponent.mockResolvedValue(textComponent as never)
    render(<EditorPage />)
    await screen.findByText('Summer Sale')

    await userEvent.click(screen.getByRole('button', { name: /text/i }))

    await waitFor(() =>
      expect(banner.addComponent).toHaveBeenCalledWith(
        'b1',
        expect.objectContaining({ type: 'text', zIndex: 0, data: expect.objectContaining({ content: 'New Text' }) })
      )
    )
    expect(await screen.findByText('text component added')).toBeInTheDocument()
  })

  it('reports a failure to add a component', async () => {
    banner.addComponent.mockRejectedValue({ response: { status: 400, data: { message: 'Layer taken' } } })
    render(<EditorPage />)
    await screen.findByText('Summer Sale')

    await userEvent.click(screen.getByRole('button', { name: /text/i }))

    expect(await screen.findByText('Layer taken')).toBeInTheDocument()
  })

  it('saves the banner details and says so once something has changed', async () => {
    banner.addComponent.mockResolvedValue(textComponent as never)
    banner.updateComponent.mockResolvedValue(textComponent as never)
    render(<EditorPage />)
    await screen.findByText('Summer Sale')
    expect(screen.getByRole('button', { name: /^save/i })).toBeDisabled()

    await userEvent.click(screen.getByRole('button', { name: /text/i }))
    fireEvent.change(await screen.findByLabelText('X'), { target: { value: '120' } })
    await userEvent.click(screen.getByRole('button', { name: /^save/i }))

    await waitFor(() =>
      expect(banner.updateBanner).toHaveBeenCalledWith('b1', { title: 'Summer Sale', description: 'd', width: 1200, height: 600 })
    )
    expect(await screen.findByText('Banner saved')).toBeInTheDocument()
  })
})
