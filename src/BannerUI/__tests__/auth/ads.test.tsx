import React from 'react'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  usePathname: () => '/ads',
}))
jest.mock('@/components/layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }))
jest.mock('@/hooks/useShopTimeZone', () => ({
  useShopTimeZone: () => ({ timeZoneId: 'Asia/Kolkata', source: 'shop', ownTimeZoneId: null, loaded: true }),
  forgetShopTimeZone: jest.fn(),
}))

let mockRoles: string[] = ['ShopOwner']
jest.mock('@/context/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'u1', email: 'o@example.com', name: 'Olive', shopId: 'shop-1', roles: mockRoles },
    hasRole: (role: string) => mockRoles.includes(role),
  }),
}))

jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
  API_ORIGIN: 'http://api.test',
  getErrorMessage: (err: { response?: { data?: { message?: string } } }, fallback: string) => err?.response?.data?.message ?? fallback,
}))
jest.mock('@/components/media/MediaPickerDialog', () => ({
  MediaPickerDialog: ({ onSelect }: { onSelect: (item: unknown) => void }) => (
    <button onClick={() => onSelect({ id: 'pic-1', fileName: 'cafe.png', url: 'http://x/cafe.png' })}>pick picture</button>
  ),
}))

import { apiClient } from '@/api/client'
import { adService, ShopAd } from '@/api/adService'
import AdsPage from '@/app/ads/page'
import { AdForm } from '@/components/ads/AdForm'
import { AdTile, ScreenWithAds, popupIsUp } from '@/components/Display/AdLayout'

const api = apiClient as jest.Mocked<typeof apiClient>

const ad = (overrides: Partial<ShopAd> = {}): ShopAd => ({
  id: 'a1', shopId: 'shop-1', shopName: 'Olive Mart', source: 'ShopOwner', advertiserName: 'Olive Cafe', headline: 'Two for one', body: null,
  mediaFileId: null, mediaUrl: null, background: '#ffeecc', textColor: '#112233', kind: 'Side', placement: 'Left', spacePercent: 25,
  popupSeconds: 0, popupEveryMinutes: 0, startAt: '2035-01-08T03:30:00Z', endAt: '2035-01-15T03:30:00Z', dailyStartMinutes: null, dailyEndMinutes: null,
  activeDays: 127, status: 'Draft', decidedByName: null, decidedAt: null, decisionNote: null, createdByUserId: 'u1', pricePerHour: null, shopSharePercent: null, stoppedAt: null, complianceNote: null, can: ['edit', 'submit', 'cancel'], ...overrides,
})

beforeEach(() => {
  jest.clearAllMocks()
  mockRoles = ['ShopOwner']
})

describe('popupIsUp', () => {
  it('is up for its seconds at the start of each cycle, from the clock', () => {
    const popup = { popupSeconds: 10, popupEveryMinutes: 5 }

    expect(popupIsUp(popup, 0)).toBe(true)
    expect(popupIsUp(popup, 9_999)).toBe(true)
    expect(popupIsUp(popup, 10_000)).toBe(false)
    expect(popupIsUp(popup, 299_999)).toBe(false)
    expect(popupIsUp(popup, 300_000)).toBe(true)
    expect(popupIsUp({ popupSeconds: 0, popupEveryMinutes: 0 }, 0)).toBe(false)
  })
})

describe('ScreenWithAds', () => {
  it('leaves the screen alone when there are no ads', () => {
    render(<ScreenWithAds ads={[]}><p>banner</p></ScreenWithAds>)

    expect(screen.getByText('banner')).toBeInTheDocument()
    expect(screen.queryByTestId('ad-layout')).toBeNull()
  })

  it('puts side strips on their edges with their share of the screen', () => {
    render(
      <ScreenWithAds ads={[ad({ id: 'l', headline: 'Left ad', placement: 'Left', spacePercent: 25 }), ad({ id: 'b', headline: 'Bottom ad', placement: 'Bottom', spacePercent: 15 })]}>
        <p>banner</p>
      </ScreenWithAds>
    )

    expect(screen.getByText('banner')).toBeInTheDocument()
    expect(screen.getByTestId('ad-l').parentElement).toHaveStyle({ width: '25%' })
    expect(screen.getByTestId('ad-b').parentElement).toHaveStyle({ height: '15%' })
    expect(screen.getByText('Left ad')).toBeInTheDocument()
  })

  it('gives a mega ad the larger share', () => {
    render(<ScreenWithAds ads={[ad({ id: 'm', kind: 'Mega', placement: 'Right', spacePercent: 70 })]}><p>banner</p></ScreenWithAds>)

    expect(screen.getByTestId('ad-m').parentElement).toHaveStyle({ width: '70%' })
  })

  it('shows a popup only while it is up, and a minor ad in its corner', () => {
    const popup = ad({ id: 'p', kind: 'Popup', placement: 'Center', spacePercent: 0, popupSeconds: 10, popupEveryMinutes: 5, headline: 'Pop!' })
    const corner = ad({ id: 'c', kind: 'Minor', placement: 'BottomRight', spacePercent: 0, headline: 'Corner' })

    const { rerender } = render(<ScreenWithAds ads={[popup, corner]} now={5_000}><p>banner</p></ScreenWithAds>)
    expect(screen.getByText('Pop!')).toBeInTheDocument()
    expect(screen.getByTestId('ad-c').parentElement).toHaveStyle({ bottom: '1.5vmin', right: '1.5vmin' })

    rerender(<ScreenWithAds ads={[popup, corner]} now={60_000}><p>banner</p></ScreenWithAds>)
    expect(screen.queryByText('Pop!')).toBeNull()
    expect(screen.getByText('Corner')).toBeInTheDocument()
  })

  it('draws a picture, text and the advertiser in a tile', () => {
    render(<AdTile ad={ad({ body: 'Fresh coffee', mediaUrl: 'http://x/c.png' })} />)

    expect(screen.getByText('Two for one')).toBeInTheDocument()
    expect(screen.getByText('Fresh coffee')).toBeInTheDocument()
    expect(screen.getByText('Olive Cafe')).toBeInTheDocument()
    expect(document.querySelector('img')).toHaveAttribute('src', 'http://x/c.png')
  })
})

describe('AdForm', () => {
  const setup = (props: Partial<React.ComponentProps<typeof AdForm>> = {}) => {
    const onSubmit = jest.fn()
    render(<AdForm zone="Asia/Kolkata" allowPicture busy={false} error={null} onSubmit={onSubmit} onCancel={jest.fn()} {...props} />)
    return onSubmit
  }

  const fillBasics = async () => {
    await userEvent.type(screen.getByLabelText('Advertiser (business name)'), 'Olive Cafe')
    await userEvent.type(screen.getByLabelText('Headline'), 'Two for one')
    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-08T09:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-15T09:00' } })
  }

  it('sends a side strip with its share, edge and the dates as the shop clock', async () => {
    const onSubmit = setup()
    await fillBasics()
    await userEvent.selectOptions(screen.getByLabelText('Edge'), 'Left')
    fireEvent.change(screen.getByLabelText(/Share of the screen/), { target: { value: '30' } })

    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))

    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      advertiserName: 'Olive Cafe', headline: 'Two for one', kind: 'Side', placement: 'Left', spacePercent: 30, popupSeconds: 0,
      startAt: '2035-01-08T03:30:00.000Z', endAt: '2035-01-15T03:30:00.000Z', daily: null, mediaFileId: null,
    }))
  })

  it('changes what is asked for with the kind of ad', async () => {
    setup()

    await userEvent.selectOptions(screen.getByLabelText('Kind of ad'), 'Popup')
    expect(screen.queryByLabelText('Edge')).toBeNull()
    expect(screen.getByLabelText('Stays for (seconds)')).toBeInTheDocument()
    expect(screen.getByLabelText('Comes back every (minutes)')).toBeInTheDocument()

    await userEvent.selectOptions(screen.getByLabelText('Kind of ad'), 'Minor')
    expect(screen.getByLabelText('Corner')).toHaveValue('BottomRight')
    expect(screen.queryByLabelText(/Share of the screen/)).toBeNull()

    await userEvent.selectOptions(screen.getByLabelText('Kind of ad'), 'Mega')
    expect(screen.getByLabelText(/Share of the screen: 60%/)).toHaveAttribute('min', '50')
  })

  it('sends a popup in the centre with its timing, and daily hours', async () => {
    const onSubmit = setup()
    await fillBasics()
    await userEvent.selectOptions(screen.getByLabelText('Kind of ad'), 'Popup')
    await userEvent.click(screen.getByLabelText('Only at certain hours each day'))
    fireEvent.change(screen.getByLabelText('From'), { target: { value: '12:00' } })
    fireEvent.change(screen.getByLabelText('Until'), { target: { value: '14:30' } })
    await userEvent.selectOptions(screen.getByLabelText('On'), 'Monday to Friday')

    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))

    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      kind: 'Popup', placement: 'Center', spacePercent: 0, popupSeconds: 10, popupEveryMinutes: 5,
      daily: { startMinutes: 720, endMinutes: 870, days: 62 },
    }))
  })

  it('asks for what is missing before sending anything', async () => {
    const onSubmit = setup()

    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('business name')

    await userEvent.type(screen.getByLabelText('Advertiser (business name)'), 'Cafe')
    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('headline')

    await userEvent.type(screen.getByLabelText('Headline'), 'Sale')
    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('starts and when it ends')

    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-15T09:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-08T09:00' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('end must be after the start')
    expect(onSubmit).not.toHaveBeenCalled()
  })

  it('shows the server reason, and a live preview', async () => {
    setup({ error: 'The screen is already taken at that time.' })
    expect(screen.getByRole('alert')).toHaveTextContent('already taken')

    await userEvent.type(screen.getByLabelText('Headline'), 'Fresh bread')
    expect(within(screen.getByTestId('ad-preview')).getByText('Fresh bread')).toBeInTheDocument()
  })

  it('offers a picture from the shop files, but not to the administrator', async () => {
    const onSubmit = setup()
    await fillBasics()
    await userEvent.click(screen.getByRole('button', { name: 'Choose a picture from my files' }))
    await userEvent.click(screen.getByText('pick picture'))
    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ mediaFileId: 'pic-1' }))
  })

  it('has no picture control for the administrator', () => {
    setup({ allowPicture: false })

    expect(screen.queryByRole('button', { name: /picture/i })).toBeNull()
  })
})

describe('Ads page', () => {
  it('lists the ads with what the person may do, and sends one in', async () => {
    api.get.mockResolvedValue([
      ad({ id: 'a1', headline: 'Two for one' }),
      ad({ id: 'a2', headline: 'Admin deal', source: 'Admin', status: 'Approved', can: [] }),
    ])
    api.post.mockResolvedValue(ad({ status: 'Approved' }))
    render(<AdsPage />)

    const mine = await screen.findByTestId('ad-row-Two for one')
    expect(mine).toHaveTextContent('Olive Cafe · Side strip, 25% on the left')
    expect(mine).toHaveTextContent('Draft')
    expect(screen.getByTestId('ad-row-Admin deal')).toHaveTextContent('Booked by the administrator')
    expect(within(screen.getByTestId('ad-row-Admin deal')).queryByRole('button', { name: 'Cancel ad' })).toBeNull()

    await userEvent.click(within(mine).getByRole('button', { name: 'Send in' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-ads/a1/submit', {}))
    expect(await screen.findByText('The ad is booked.')).toBeInTheDocument()
  })

  it('lets the owner approve, or send back with a reason', async () => {
    api.get.mockResolvedValue([ad({ id: 'a3', headline: 'Exec idea', source: 'SalesExecutive', status: 'PendingApproval', can: ['approve', 'reject'] })])
    api.post.mockResolvedValue(ad({ status: 'Rejected' }))
    render(<AdsPage />)
    const row = await screen.findByTestId('ad-row-Exec idea')

    await userEvent.click(within(row).getByRole('button', { name: 'Send back' }))
    expect(within(row).getByRole('button', { name: 'Send it back' })).toBeDisabled()
    await userEvent.type(within(row).getByLabelText('Why is it sent back?'), 'Too loud')
    await userEvent.click(within(row).getByRole('button', { name: 'Send it back' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-ads/a3/reject', { reason: 'Too loud' }))
  })

  it('shows the history of an ad', async () => {
    api.get.mockImplementation(async (path: string) => {
      if (path.endsWith('/history')) return [{ action: 'Created', userName: 'Olive', note: null, at: '2030-01-07T10:00:00Z' }, { action: 'Approved', userName: 'Olive', note: 'Looks good', at: '2030-01-07T11:00:00Z' }]
      return [ad()]
    })
    render(<AdsPage />)
    const row = await screen.findByTestId('ad-row-Two for one')

    await userEvent.click(within(row).getByRole('button', { name: 'History' }))

    const history = await within(row).findByRole('list', { name: 'History' })
    expect(history).toHaveTextContent('Created by Olive')
    expect(history).toHaveTextContent('Approved by Olive')
    expect(history).toHaveTextContent('Looks good')
  })

  it('books a new ad and shows why the server refuses a clash', async () => {
    api.get.mockResolvedValue([])
    api.post.mockRejectedValue({ response: { data: { message: 'The screen is already taken at that time by "Big sale".' } } })
    render(<AdsPage />)
    await screen.findByText(/No ads yet/)

    await userEvent.click(screen.getByRole('button', { name: 'Book an ad' }))
    await userEvent.type(screen.getByLabelText('Advertiser (business name)'), 'Olive Cafe')
    await userEvent.type(screen.getByLabelText('Headline'), 'Two for one')
    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-08T09:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-15T09:00' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-ads', expect.objectContaining({ headline: 'Two for one', kind: 'Side' })))
    expect(await screen.findByRole('alert')).toHaveTextContent('Big sale')
  })

  it('asks the administrator to choose a shop first, then books for that shop', async () => {
    mockRoles = ['Admin']
    api.get.mockImplementation(async (path: string) => {
      if (path.startsWith('/shops?')) return { items: [{ id: 'shop-9', name: 'Olive Mart' }] }
      if (path.endsWith('/time-zone')) return { timeZoneId: 'Asia/Kolkata' }
      return []
    })
    api.post.mockResolvedValue(ad())
    render(<AdsPage />)

    expect(await screen.findByText('Choose a shop to see its ads.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Book an ad' })).toBeNull()

    await screen.findByRole('option', { name: 'Olive Mart' })
    await userEvent.selectOptions(screen.getByLabelText('Shop'), 'shop-9')
    await waitFor(() => expect(api.get).toHaveBeenCalledWith('/shop-ads?shopId=shop-9'))

    await userEvent.click(await screen.findByRole('button', { name: 'Book an ad' }))
    expect(screen.queryByRole('button', { name: /picture/i })).toBeNull()
    await userEvent.type(screen.getByLabelText('Advertiser (business name)'), 'Olive Cafe')
    await userEvent.type(screen.getByLabelText('Headline'), 'Admin deal')
    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-08T09:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-15T09:00' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save as draft' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-ads', expect.objectContaining({ shopId: 'shop-9', headline: 'Admin deal' })))
  })

  it('turns the picture link of an ad into a full address', async () => {
    api.get.mockResolvedValue([ad({ mediaUrl: '/api/media/p/download?x=1' })])

    const [loaded] = await adService.list()

    expect(loaded.mediaUrl).toBe('http://api.test/api/media/p/download?x=1')
  })
})
