import React from 'react'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  usePathname: () => '/ads',
}))
jest.mock('@/components/layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }))
jest.mock('@/hooks/useShopTimeZone', () => ({
  useShopTimeZone: () => ({ timeZoneId: 'UTC', source: 'shop', ownTimeZoneId: null, loaded: true }),
  forgetShopTimeZone: jest.fn(),
}))
jest.mock('@/components/location/LocationPicker', () => ({
  LocationPicker: ({ onChange }: { onChange: (v: unknown) => void }) => (
    <div>
      <button type="button" onClick={() => onChange({ countryCode: 'IN' })}>choose India</button>
      <button type="button" onClick={() => onChange({ countryCode: 'IN', stateId: 7, cityId: 70 })}>choose Mumbai</button>
    </div>
  ),
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

import { apiClient } from '@/api/client'
import { AdRate, ShopAd } from '@/api/adService'
import AdsPage from '@/app/ads/page'
import StatementPage from '@/app/ads/statement/page'
import AdRatesPage from '@/app/admin/ad-rates/page'

const api = apiClient as jest.Mocked<typeof apiClient>

const ad = (overrides: Partial<ShopAd> = {}): ShopAd => ({
  id: 'a1', shopId: 'shop-1', shopName: 'Olive Mart', source: 'Admin', advertiserName: 'City Bank', headline: 'Save more', body: null,
  mediaFileId: null, mediaUrl: null, background: '#ffffff', textColor: '#000000', kind: 'Side', placement: 'Left', spacePercent: 25,
  popupSeconds: 0, popupEveryMinutes: 0, startAt: '2035-01-08T00:00:00Z', endAt: '2035-01-15T00:00:00Z', dailyStartMinutes: null, dailyEndMinutes: null,
  activeDays: 127, status: 'Approved', decidedByName: 'Admin', decidedAt: null, decisionNote: null, createdByUserId: 'admin', pricePerHour: 50, shopSharePercent: 70,
  stoppedAt: null, can: ['override'], ...overrides,
})

const rate = (overrides: Partial<AdRate> = {}): AdRate => ({
  id: 'r1', level: 'City', countryCode: null, stateId: null, cityId: 70, place: 'Mumbai', kind: null, pricePerHour: 120, shopSharePercent: 70, isActive: true, ...overrides,
})

beforeEach(() => {
  jest.clearAllMocks()
  mockRoles = ['ShopOwner']
})

describe('Ads page: pricing and override', () => {
  it('shows the price of an administrator ad and lets the owner override it with a reason', async () => {
    api.get.mockResolvedValue([ad()])
    api.post.mockResolvedValue(ad({ status: 'Overridden', can: [] }))
    render(<AdsPage />)
    const row = await screen.findByTestId('ad-row-Save more')

    expect(within(row).getByTestId('ad-price')).toHaveTextContent('50.00 an hour, the shop is paid 70% of it')
    expect(within(row).queryByRole('button', { name: 'Cancel ad' })).toBeNull()

    await userEvent.click(within(row).getByRole('button', { name: 'Override this ad' }))
    expect(within(row).getByText(/the shop is paid for the hours it ran/)).toBeInTheDocument()
    await userEvent.type(within(row).getByLabelText('Reason (optional)'), 'A local shop pays more')
    await userEvent.click(within(row).getByRole('button', { name: 'Yes, stop this ad' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-ads/a1/override', { reason: 'A local shop pays more' }))
    expect(await screen.findByText('The ad was stopped. The administrator has been told.')).toBeInTheDocument()
  })

  it('shows a stopped ad as stopped by the shop, with no actions', async () => {
    api.get.mockResolvedValue([ad({ status: 'Overridden', can: [], stoppedAt: '2035-01-10T10:00:00Z' })])
    render(<AdsPage />)
    const row = await screen.findByTestId('ad-row-Save more')

    expect(row).toHaveTextContent('Stopped by the shop')
    expect(row).toHaveTextContent('Stopped ')
    expect(within(row).queryByRole('button', { name: 'Override this ad' })).toBeNull()
  })

  it('links to the statement for owners and administrators, not for executives', async () => {
    api.get.mockResolvedValue([])
    const { unmount } = render(<AdsPage />)
    expect(await screen.findByRole('link', { name: 'Monthly statement' })).toHaveAttribute('href', '/ads/statement')
    unmount()

    mockRoles = ['SalesExecutive']
    render(<AdsPage />)
    await screen.findByText(/No ads yet/)
    expect(screen.queryByRole('link', { name: 'Monthly statement' })).toBeNull()
  })
})

describe('Statement page', () => {
  const statement = {
    month: '2030-01',
    totalPayout: 420,
    shops: [
      {
        shopId: 'shop-1', shopName: 'Olive Mart', timeZoneId: 'UTC', adminAdHours: 48, ownAdHours: 24, payout: 420,
        lines: [
          { adId: 'a', headline: 'Save more', advertiserName: 'City Bank', kind: 'Side', source: 'Admin', status: 'Approved', hours: 48, pricePerHour: 12.5, shopSharePercent: 70, payout: 420 },
          { adId: 'b', headline: 'Own offer', advertiserName: 'Olive Cafe', kind: 'Popup', source: 'ShopOwner', status: 'Approved', hours: 24, pricePerHour: null, shopSharePercent: null, payout: null },
        ],
      },
    ],
  }

  it('lists hours and what the shop is paid, and says own ads are not tracked', async () => {
    api.get.mockResolvedValue(statement)
    render(<StatementPage />)

    const shop = await screen.findByTestId('statement-Olive Mart')
    expect(shop).toHaveTextContent('Administrator ads 48 h · own ads 24 h')
    expect(shop).toHaveTextContent('paid 420.00')
    const paid = within(screen.getByTestId('line-Save more'))
    expect(paid.getByText('Administrator')).toBeInTheDocument()
    expect(paid.getByText('12.50')).toBeInTheDocument()
    expect(paid.getByText('70%')).toBeInTheDocument()
    expect(within(screen.getByTestId('line-Own offer')).getByText('not tracked')).toBeInTheDocument()
    expect(screen.getByTestId('statement-total')).toHaveTextContent('Total paid to shops: 420.00')
    expect(api.get).toHaveBeenCalledWith(expect.stringMatching(/^\/ad-statements\?month=\d{4}-\d{2}$/))
  })

  it('asks for another month, and says when nothing ran', async () => {
    api.get.mockResolvedValue({ month: '2030-02', shops: [], totalPayout: 0 })
    render(<StatementPage />)
    expect(await screen.findByTestId('statement-empty')).toBeInTheDocument()

    fireEvent.change(screen.getByLabelText('Month'), { target: { value: '2030-02' } })

    await waitFor(() => expect(api.get).toHaveBeenCalledWith('/ad-statements?month=2030-02'))
  })

  it('lets the administrator narrow it to one shop', async () => {
    mockRoles = ['Admin']
    api.get.mockImplementation(async (path: string) => (path.startsWith('/shops?') ? { items: [{ id: 'shop-9', name: 'Olive Mart' }] } : { month: '2030-01', shops: [], totalPayout: 0 }))
    render(<StatementPage />)
    await screen.findByRole('option', { name: 'Olive Mart' })

    await userEvent.selectOptions(screen.getByLabelText('Shop'), 'shop-9')

    await waitFor(() => expect(api.get).toHaveBeenCalledWith(expect.stringMatching(/^\/ad-statements\?month=\d{4}-\d{2}&shopId=shop-9$/)))
  })

  it('shows why the statement could not be loaded', async () => {
    api.get.mockRejectedValue({ response: { data: { message: 'Only the shop owner and the administrator can see the statement.' } } })
    render(<StatementPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Only the shop owner')
  })
})

describe('Ad rates page', () => {
  beforeEach(() => {
    mockRoles = ['Admin']
  })

  it('lists the rates and says what is switched off', async () => {
    api.get.mockResolvedValue([rate(), rate({ id: 'r2', level: 'All', place: 'Every shop', kind: 'Popup', pricePerHour: 30, isActive: false })])
    render(<AdRatesPage />)

    const city = await screen.findByTestId('rate-Mumbai-all')
    expect(city).toHaveTextContent('Mumbai (City)')
    expect(city).toHaveTextContent('120.00')
    expect(city).toHaveTextContent('70%')
    const off = screen.getByTestId('rate-Every shop-Popup')
    expect(off).toHaveTextContent('switched off')
    expect(within(off).queryByRole('button')).toBeNull()
  })

  it('adds a rate for the deepest place chosen', async () => {
    api.get.mockResolvedValue([])
    api.post.mockResolvedValue(rate())
    render(<AdRatesPage />)
    await screen.findByText(/No rates yet/)

    await userEvent.click(screen.getByRole('button', { name: 'Add a rate' }))
    expect(screen.getByText(/Now: Every shop/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'choose Mumbai' }))
    expect(screen.getByText(/Now: City/)).toBeInTheDocument()
    await userEvent.selectOptions(screen.getByLabelText('Kind of ad'), 'Mega')
    await userEvent.type(screen.getByLabelText('Price for one hour'), '120.50')
    await userEvent.clear(screen.getByLabelText('Paid to the shop (%)'))
    await userEvent.type(screen.getByLabelText('Paid to the shop (%)'), '65')
    await userEvent.click(screen.getByRole('button', { name: 'Save rate' }))

    await waitFor(() =>
      expect(api.post).toHaveBeenCalledWith('/ad-rates', { level: 'City', countryCode: null, stateId: null, cityId: 70, kind: 'Mega', pricePerHour: 120.5, shopSharePercent: 65 })
    )
    expect(await screen.findByText('The rate was saved.')).toBeInTheDocument()
  })

  it('adds a rate for every shop when no place is chosen', async () => {
    api.get.mockResolvedValue([])
    api.post.mockResolvedValue(rate())
    render(<AdRatesPage />)
    await screen.findByText(/No rates yet/)

    await userEvent.click(screen.getByRole('button', { name: 'Add a rate' }))
    await userEvent.type(screen.getByLabelText('Price for one hour'), '10')
    await userEvent.click(screen.getByRole('button', { name: 'Save rate' }))

    await waitFor(() =>
      expect(api.post).toHaveBeenCalledWith('/ad-rates', { level: 'All', countryCode: null, stateId: null, cityId: null, kind: null, pricePerHour: 10, shopSharePercent: 100 })
    )
  })

  it('checks the price and share before sending, and shows the server reason', async () => {
    api.get.mockResolvedValue([])
    api.post.mockRejectedValue({ response: { data: { message: 'There is already a rate for this place and kind of ad. Change that one instead.' } } })
    render(<AdRatesPage />)
    await screen.findByText(/No rates yet/)
    await userEvent.click(screen.getByRole('button', { name: 'Add a rate' }))

    await userEvent.click(screen.getByRole('button', { name: 'Save rate' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Enter the price for one hour')

    await userEvent.type(screen.getByLabelText('Price for one hour'), '10')
    fireEvent.change(screen.getByLabelText('Paid to the shop (%)'), { target: { value: '150' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save rate' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('between 0 and 100')
    expect(api.post).not.toHaveBeenCalled()

    fireEvent.change(screen.getByLabelText('Paid to the shop (%)'), { target: { value: '70' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save rate' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('already a rate for this place')
  })

  it('changes and switches off a rate', async () => {
    api.get.mockResolvedValue([rate()])
    api.put.mockResolvedValue(rate({ pricePerHour: 150 }))
    api.delete.mockResolvedValue(rate({ isActive: false }))
    render(<AdRatesPage />)
    const row = await screen.findByTestId('rate-Mumbai-all')

    await userEvent.click(within(row).getByRole('button', { name: 'Change' }))
    expect(screen.getByLabelText('Price for one hour')).toHaveValue(120)
    fireEvent.change(screen.getByLabelText('Price for one hour'), { target: { value: '150' } })
    await userEvent.click(screen.getByRole('button', { name: 'Save rate' }))
    await waitFor(() => expect(api.put).toHaveBeenCalledWith('/ad-rates/r1', expect.objectContaining({ level: 'City', cityId: 70, pricePerHour: 150 })))

    await userEvent.click(within(await screen.findByTestId('rate-Mumbai-all')).getByRole('button', { name: 'Switch off' }))
    await waitFor(() => expect(api.delete).toHaveBeenCalledWith('/ad-rates/r1'))
  })
})

describe('Booking for a whole place', () => {
  beforeEach(() => {
    mockRoles = ['Admin']
    api.get.mockImplementation(async (path: string) => (path.startsWith('/shops?') ? { items: [] } : []))
  })

  const fill = async () => {
    await userEvent.type(screen.getByLabelText('Advertiser (business name)'), 'Big Brand')
    await userEvent.type(screen.getByLabelText('Headline'), 'Festival sale')
    fireEvent.change(screen.getByLabelText('Starts'), { target: { value: '2035-01-08T09:00' } })
    fireEvent.change(screen.getByLabelText('Ends'), { target: { value: '2035-01-15T09:00' } })
  }

  it('books on every shop of the chosen city and lists the shops it skipped', async () => {
    api.post.mockResolvedValue({ shops: 3, booked: 2, skipped: [{ shopId: 's3', shopName: 'Busy Mart', reason: 'The screen is already taken at that time by "Already here".' }] })
    render(<AdsPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Book for a whole place' }))
    await userEvent.click(screen.getByRole('button', { name: 'choose Mumbai' }))
    await fill()
    await userEvent.click(screen.getByRole('button', { name: 'Book on all these shops' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-ads/campaign', expect.objectContaining({ headline: 'Festival sale', countryCode: 'IN', stateId: 7, cityId: 70 })))
    const result = await screen.findByTestId('campaign-result')
    expect(result).toHaveTextContent('Booked on 2 of 3 shops.')
    expect(result).toHaveTextContent('Busy Mart: The screen is already taken')
  })

  it('shows why a campaign was refused and keeps the form', async () => {
    api.post.mockRejectedValue({ response: { data: { message: 'There is no active shop in that place.' } } })
    render(<AdsPage />)
    await userEvent.click(await screen.findByRole('button', { name: 'Book for a whole place' }))
    await fill()

    await userEvent.click(screen.getByRole('button', { name: 'Book on all these shops' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('no active shop in that place')
    expect(screen.getByLabelText('Headline')).toHaveValue('Festival sale')
  })

  it('is only for the administrator', async () => {
    mockRoles = ['ShopOwner']
    render(<AdsPage />)
    await screen.findByText(/No ads yet/)

    expect(screen.queryByRole('button', { name: 'Book for a whole place' })).toBeNull()
  })
})
