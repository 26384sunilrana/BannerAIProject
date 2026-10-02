import React from 'react'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('@/components/layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }))

jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn(), putBinary: jest.fn() },
  API_ORIGIN: 'http://api.test',
  getErrorMessage: (err: { response?: { data?: { message?: string } } }, fallback: string) => err?.response?.data?.message ?? fallback,
}))

const upload = jest.fn()
jest.mock('@/hooks/useMediaUpload', () => ({
  useMediaUpload: () => ({ upload, isUploading: false, progress: 0, error: null }),
}))

import { apiClient } from '@/api/client'
import { mediaService, MediaLibraryItem } from '@/api/mediaService'
import MediaLibraryPage from '@/app/media/page'
import { MediaPickerDialog } from '@/components/media/MediaPickerDialog'
import { formatBytes, formatDuration } from '@/lib/format'

const api = apiClient as jest.Mocked<typeof apiClient>

const file = (overrides: Partial<MediaLibraryItem> = {}): MediaLibraryItem => ({
  id: 'f1', fileName: 'hero.png', fileType: 1, contentType: 'image/png', sizeBytes: 2_500_000, width: 1920, height: 1080,
  durationSeconds: null, createdAt: '2026-10-01T10:00:00Z', url: '/api/media/f1/download?expires=1&sig=x', inUse: false, ...overrides,
})

const hero = file({ inUse: true })
const clip = file({ id: 'f2', fileName: 'promo.mp4', fileType: 2, contentType: 'video/mp4', sizeBytes: 12_000_000, width: 1280, height: 720, durationSeconds: 75 })

function serve(items: MediaLibraryItem[] = [hero, clip], usage = { usedBytes: 14_500_000, fileCount: 2, imageCount: 1, videoCount: 1, limitBytes: 1_073_741_824 }) {
  api.get.mockImplementation(async (path: string) => {
    if (path.startsWith('/media/usage')) return usage
    return { items, total: items.length, page: 1, pageSize: 24 }
  })
}

beforeEach(() => {
  jest.clearAllMocks()
  serve()
})

describe('format helpers', () => {
  it('writes sizes the way file managers do', () => {
    expect(formatBytes(0)).toBe('0 B')
    expect(formatBytes(1023)).toBe('1023 B')
    expect(formatBytes(1536)).toBe('1.5 KB')
    expect(formatBytes(2 * 1024 * 1024)).toBe('2 MB')
    expect(formatBytes(1_073_741_824)).toBe('1 GB')
    expect(formatBytes(-1)).toBe('-')
  })

  it('writes lengths as minutes and seconds, hours when needed', () => {
    expect(formatDuration(75)).toBe('1:15')
    expect(formatDuration(0.3)).toBe('0:01')
    expect(formatDuration(3725)).toBe('1:02:05')
    expect(formatDuration(NaN)).toBe('-')
  })
})

describe('mediaService library calls', () => {
  it('asks for a page with its filters, and makes the links absolute', async () => {
    const page = await mediaService.list({ type: 'video', search: ' promo ', page: 2, pageSize: 12 })

    expect(api.get).toHaveBeenCalledWith('/media?type=video&search=promo&page=2&pageSize=12')
    expect(page.items[0].url).toBe('http://api.test/api/media/f1/download?expires=1&sig=x')
  })

  it('asks for the plain list when nothing is filtered', async () => {
    await mediaService.list()

    expect(api.get).toHaveBeenCalledWith('/media')
  })
})

describe('My files page', () => {
  it('shows each file with its size, dimensions, length and whether a banner uses it', async () => {
    render(<MediaLibraryPage />)

    const image = await screen.findByTestId('file-hero.png')
    expect(image).toHaveTextContent('2.4 MB · 1920×1080')
    expect(within(image).getByText('Used in a banner')).toBeInTheDocument()
    expect(within(image).getByRole('img', { name: 'hero.png' })).toHaveAttribute('src', 'http://api.test/api/media/f1/download?expires=1&sig=x')

    const video = screen.getByTestId('file-promo.mp4')
    expect(video).toHaveTextContent('11.4 MB · 1280×720 · 1:15')
    expect(within(video).getByText('Not used')).toBeInTheDocument()
  })

  it('shows how much of the plan allowance is used', async () => {
    render(<MediaLibraryPage />)

    const box = await screen.findByTestId('storage-usage')
    expect(box).toHaveTextContent('13.8 MB of 1 GB used · 1 image · 1 video')
    expect(within(box).getByRole('progressbar', { name: 'Storage used' })).toHaveAttribute('aria-valuenow', '1')
  })

  it('shows the amount alone when the shop has no plan', async () => {
    serve([hero], { usedBytes: 2_500_000, fileCount: 1, imageCount: 1, videoCount: 0, limitBytes: null as unknown as number })
    render(<MediaLibraryPage />)

    const box = await screen.findByTestId('storage-usage')
    expect(box).toHaveTextContent('2.4 MB used')
    expect(within(box).queryByRole('progressbar')).toBeNull()
  })

  it('filters by kind and by name, from the first page', async () => {
    render(<MediaLibraryPage />)
    await screen.findByTestId('file-hero.png')

    fireEvent.change(screen.getByLabelText('Show'), { target: { value: 'video' } })
    await waitFor(() => expect(api.get).toHaveBeenCalledWith(expect.stringMatching(/^\/media\?type=video&page=1/)))

    await userEvent.type(screen.getByLabelText('Search by name'), 'promo')
    await waitFor(() => expect(api.get).toHaveBeenCalledWith(expect.stringContaining('search=promo')))
  })

  it('says so when nothing has been uploaded, and when nothing matches', async () => {
    serve([])
    const { unmount } = render(<MediaLibraryPage />)
    expect(await screen.findByText(/have not uploaded anything yet/)).toBeInTheDocument()
    unmount()

    render(<MediaLibraryPage />)
    await userEvent.type(await screen.findByLabelText('Search by name'), 'zzz')
    expect(await screen.findByText('No file matches.')).toBeInTheDocument()
  })

  it('asks first, then deletes and reloads', async () => {
    api.delete.mockResolvedValue({ message: 'ok' })
    render(<MediaLibraryPage />)

    await userEvent.click(within(await screen.findByTestId('file-promo.mp4')).getByRole('button', { name: 'Delete' }))
    expect(api.delete).not.toHaveBeenCalled()
    const dialog = screen.getByText('Delete this file?').closest('div')!.parentElement!
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(api.delete).toHaveBeenCalledWith('/media/f2'))
    expect(await screen.findByText('promo.mp4 deleted')).toBeInTheDocument()
  })

  it('shows which banners block a delete', async () => {
    api.delete.mockRejectedValue({ response: { data: { message: 'hero.png is used by Summer Sale. Take it out first.' } } })
    render(<MediaLibraryPage />)

    await userEvent.click(within(await screen.findByTestId('file-hero.png')).getByRole('button', { name: 'Delete' }))
    const dialog = screen.getByText('Delete this file?').closest('div')!.parentElement!
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByText('hero.png is used by Summer Sale. Take it out first.')).toBeInTheDocument()
    expect(screen.getByTestId('file-hero.png')).toBeInTheDocument()
  })

  it('uploads a picked file as an image or a video by its type, then reloads', async () => {
    upload.mockResolvedValue({ mediaFileId: 'new', url: 'x' })
    render(<MediaLibraryPage />)
    await screen.findByTestId('file-hero.png')

    const input = screen.getByLabelText('Upload a file') as HTMLInputElement
    await userEvent.upload(input, new File(['x'], 'clip.webm', { type: 'video/webm' }))

    await waitFor(() => expect(upload).toHaveBeenCalledWith(expect.objectContaining({ name: 'clip.webm' }), 'video'))
    expect(await screen.findByText('clip.webm uploaded')).toBeInTheDocument()
  })
})

describe('MediaPickerDialog', () => {
  it('lists only the chosen kind and hands back the file picked', async () => {
    serve([file({ id: 'p1', fileName: 'banner-bg.png' })])
    const onSelect = jest.fn()
    render(<MediaPickerDialog kind="image" onSelect={onSelect} onClose={jest.fn()} />)

    await userEvent.click(await screen.findByTestId('pick-banner-bg.png'))

    expect(api.get).toHaveBeenCalledWith(expect.stringContaining('type=image'))
    expect(onSelect).toHaveBeenCalledWith(expect.objectContaining({ id: 'p1' }))
  })

  it('tells the person when there is nothing to pick, and closes', async () => {
    serve([])
    const onClose = jest.fn()
    render(<MediaPickerDialog kind="video" onSelect={jest.fn()} onClose={onClose} />)

    expect(await screen.findByText('You have not uploaded any videos yet.')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Close' }))
    expect(onClose).toHaveBeenCalled()
  })

  it('shows why the files could not be loaded', async () => {
    api.get.mockRejectedValue({ response: { data: { message: 'Not allowed' } } })
    render(<MediaPickerDialog kind="image" onSelect={jest.fn()} onClose={jest.fn()} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Not allowed')
  })
})
