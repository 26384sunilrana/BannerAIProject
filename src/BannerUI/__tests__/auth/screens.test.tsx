import React from 'react'
import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  usePathname: () => '/screens',
}))
jest.mock('@/components/layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }))
jest.mock('@/hooks/useShopTimeZone', () => ({
  useShopTimeZone: () => ({ timeZoneId: 'UTC', source: 'shop', ownTimeZoneId: null, loaded: true }),
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
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn(), useDeviceRefresher: jest.fn() },
  API_URL: 'http://api.test/api',
  API_ORIGIN: 'http://api.test',
  getErrorMessage: (err: { response?: { data?: { message?: string } } }, fallback: string) => err?.response?.data?.message ?? fallback,
}))

jest.mock('@/lib/deviceScreen', () => ({
  deviceApi: { startPairing: jest.fn(), pollPairing: jest.fn(), exchange: jest.fn() },
  loadScreenSecret: jest.fn(),
  saveScreenSecret: jest.fn(),
  forgetScreenSecret: jest.fn(),
  toDataAddress: jest.fn().mockResolvedValue(null),
}))
jest.mock('@/components/Display/ScreenPlayer', () => ({
  ScreenPlayer: ({ shopName, onLost }: { shopName: string; onLost: () => void }) => (
    <div data-testid="player-running">
      playing for {shopName}
      <button onClick={onLost}>lose it</button>
    </div>
  ),
}))

import { apiClient } from '@/api/client'
import { deviceApi, forgetScreenSecret, loadScreenSecret, saveScreenSecret } from '@/lib/deviceScreen'
import ScreensPage from '@/app/screens/page'
import PlayerPage from '@/app/player/page'

const api = apiClient as jest.Mocked<typeof apiClient>
const device = deviceApi as jest.Mocked<typeof deviceApi>
const load = loadScreenSecret as jest.Mock

const screenInfo = (overrides = {}) => ({ id: 's1', name: 'Shop window', online: true, createdAt: '2030-01-07T10:00:00Z', lastSeenAt: '2030-01-07T10:30:00Z', appVersion: 'web-player-1', status: 'Active', ...overrides })
const emptyReport = { from: '2030-01-01', to: '2030-01-07', rows: [], days: [] }

function serve(screens: unknown[], report: unknown = emptyReport) {
  api.get.mockImplementation(async (path: string) => {
    if (path.endsWith('/screens')) return screens
    if (path.includes('/screens/report')) return report
    throw new Error(path)
  })
}

beforeEach(() => {
  jest.clearAllMocks()
  mockRoles = ['ShopOwner']
})

describe('Screens page', () => {
  it('tells the owner how to add a screen, and adds it with the code typed in', async () => {
    serve([])
    api.post.mockResolvedValue(screenInfo())
    render(<ScreensPage />)

    expect(await screen.findByTestId('no-screen')).toBeInTheDocument()
    expect(screen.getByText(/\/player/)).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Add the screen' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Type the six characters')

    await userEvent.type(screen.getByLabelText('Code on the screen'), 'k7q-f92')
    await userEvent.type(screen.getByLabelText('Name (optional)'), 'Shop window')
    await userEvent.click(screen.getByRole('button', { name: 'Add the screen' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shops/shop-1/screens/pair', { code: 'K7Q-F92', name: 'Shop window' }))
    expect(await screen.findByText(/starts by itself/)).toBeInTheDocument()
  })

  it('shows why a code was refused', async () => {
    serve([])
    api.post.mockRejectedValue({ response: { data: { message: 'That code is not right, or it has expired.' } } })
    render(<ScreensPage />)
    await screen.findByTestId('no-screen')

    await userEvent.type(screen.getByLabelText('Code on the screen'), 'ZZZZZZ')
    await userEvent.click(screen.getByRole('button', { name: 'Add the screen' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('not right')
  })

  it('shows the screen online or offline, and no add form while the shop has one', async () => {
    serve([screenInfo({ online: false, lastSeenAt: null })])
    render(<ScreensPage />)

    const card = await screen.findByTestId('screen-Shop window')
    expect(within(card).getByTestId('screen-state')).toHaveTextContent('Offline')
    expect(card).toHaveTextContent('Not heard from yet')
    expect(screen.queryByLabelText('Code on the screen')).toBeNull()
  })

  it('renames and removes the screen after a confirmation', async () => {
    serve([screenInfo()])
    api.put.mockResolvedValue(screenInfo({ name: 'Window TV' }))
    api.delete.mockResolvedValue({})
    render(<ScreensPage />)
    const card = await screen.findByTestId('screen-Shop window')

    await userEvent.click(within(card).getByRole('button', { name: 'Rename' }))
    await userEvent.clear(screen.getByLabelText('Name'))
    await userEvent.type(screen.getByLabelText('Name'), 'Window TV')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))
    await waitFor(() => expect(api.put).toHaveBeenCalledWith('/shops/shop-1/screens/s1', { name: 'Window TV' }))

    await userEvent.click(within(await screen.findByTestId('screen-Shop window')).getByRole('button', { name: 'Remove this screen' }))
    await userEvent.click(await screen.findByRole('button', { name: 'Remove the screen' }))
    await waitFor(() => expect(api.delete).toHaveBeenCalledWith('/shops/shop-1/screens/s1'))
  })

  it('lets an executive look but not change anything', async () => {
    mockRoles = ['SalesExecutive']
    serve([screenInfo()])
    render(<ScreensPage />)

    const card = await screen.findByTestId('screen-Shop window')
    expect(within(card).queryByRole('button', { name: 'Rename' })).toBeNull()
    expect(within(card).queryByRole('button', { name: 'Remove this screen' })).toBeNull()
  })

  it('shows what was on the screen, and asks for another period', async () => {
    serve([screenInfo()], {
      from: '2030-01-01', to: '2030-01-07', days: [],
      rows: [
        { kind: 'Banner', refId: 'b1', label: 'Lunch special', hours: 12.5 },
        { kind: 'Ad', refId: 'a1', label: 'Fresh bread', hours: 3 },
        { kind: 'DefaultBoard', refId: null, label: 'Default board', hours: 1.25 },
      ],
    })
    render(<ScreensPage />)

    expect(await screen.findByTestId('play-Lunch special')).toHaveTextContent('12.50')
    expect(screen.getByTestId('play-Fresh bread')).toHaveTextContent('Ad')
    expect(screen.getByTestId('play-Default board')).toHaveTextContent('1.25')

    await userEvent.selectOptions(screen.getByLabelText('Period'), '30')
    await waitFor(() => expect(api.get).toHaveBeenCalledWith('/shops/shop-1/screens/report?days=30'))
  })

  it('says so when nothing was reported', async () => {
    serve([screenInfo()])
    render(<ScreensPage />)

    expect(await screen.findByTestId('no-play')).toBeInTheDocument()
  })
})

describe('Player page: the television', () => {
  beforeEach(() => {
    jest.useFakeTimers({ advanceTimers: true })
    window.history.pushState({}, '', '/player')
  })
  afterEach(() => jest.useRealTimers())

  it('shows a code when it is new, and starts playing once an owner has typed it', async () => {
    load.mockReturnValue(null)
    device.startPairing.mockResolvedValue({ code: 'K7QF92', deviceSecret: 'secret-1', expiresAt: new Date(Date.now() + 900_000).toISOString() })
    device.pollPairing.mockResolvedValueOnce({ status: 'waiting' }).mockResolvedValue({ status: 'paired', shopName: 'Olive Mart' })
    device.exchange.mockResolvedValue({ accessToken: 'jwt', expiresIn: 3600, shopId: 'shop-1', shopName: 'Olive Mart' })
    render(<PlayerPage />)

    expect(await screen.findByTestId('pairing-code')).toHaveTextContent('K7Q F92')
    expect(saveScreenSecret).toHaveBeenCalledWith('secret-1')
    expect(screen.getByText(/Screens/)).toBeInTheDocument()

    await act(async () => { jest.advanceTimersByTime(4100) })
    await act(async () => { jest.advanceTimersByTime(4100) })

    expect(await screen.findByTestId('player-running')).toHaveTextContent('playing for Olive Mart')
    expect(device.exchange).toHaveBeenCalledWith('secret-1')
    expect(apiClient.useDeviceRefresher).toHaveBeenCalled()
  })

  it('goes straight to playing when it already holds a secret', async () => {
    load.mockReturnValue('saved-secret')
    device.exchange.mockResolvedValue({ accessToken: 'jwt', expiresIn: 3600, shopId: 'shop-1', shopName: 'Olive Mart' })
    render(<PlayerPage />)

    expect(await screen.findByTestId('player-running')).toBeInTheDocument()
    expect(device.startPairing).not.toHaveBeenCalled()
  })

  it('forgets the secret and shows a new code when the screen was removed (401)', async () => {
    load.mockReturnValue('old-secret')
    device.exchange.mockRejectedValue({ response: { status: 401 } })
    device.startPairing.mockResolvedValue({ code: 'ABCDEF', deviceSecret: 'new-secret', expiresAt: new Date(Date.now() + 900_000).toISOString() })
    render(<PlayerPage />)

    expect(await screen.findByTestId('pairing-code')).toHaveTextContent('ABC DEF')
    expect(forgetScreenSecret).toHaveBeenCalled()
  })

  it('keeps trying when there is no connection, and does not forget the screen', async () => {
    load.mockReturnValue('saved-secret')
    device.exchange.mockRejectedValueOnce(new Error('Network Error')).mockResolvedValue({ accessToken: 'jwt', expiresIn: 3600, shopId: 'shop-1', shopName: 'Olive Mart' })
    render(<PlayerPage />)

    expect(await screen.findByText('No connection')).toBeInTheDocument()
    expect(forgetScreenSecret).not.toHaveBeenCalled()

    await act(async () => { jest.advanceTimersByTime(15_100) })
    expect(await screen.findByTestId('player-running')).toBeInTheDocument()
  })

  it('starts pairing again when the playing screen is told it was removed', async () => {
    load.mockReturnValue('saved-secret')
    device.exchange.mockResolvedValue({ accessToken: 'jwt', expiresIn: 3600, shopId: 'shop-1', shopName: 'Olive Mart' })
    device.startPairing.mockResolvedValue({ code: 'QWERTY', deviceSecret: 'again', expiresAt: new Date(Date.now() + 900_000).toISOString() })
    render(<PlayerPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'lose it' }))

    expect(await screen.findByTestId('pairing-code')).toHaveTextContent('QWE RTY')
    expect(forgetScreenSecret).toHaveBeenCalled()
  })

  it('makes a new code when the old one expires', async () => {
    load.mockReturnValue(null)
    device.startPairing
      .mockResolvedValueOnce({ code: 'FIRST2', deviceSecret: 's1', expiresAt: new Date(Date.now() + 900_000).toISOString() })
      .mockResolvedValue({ code: 'SECND3', deviceSecret: 's2', expiresAt: new Date(Date.now() + 900_000).toISOString() })
    device.pollPairing.mockResolvedValueOnce({ status: 'expired' }).mockResolvedValue({ status: 'waiting' })
    render(<PlayerPage />)
    expect(await screen.findByTestId('pairing-code')).toHaveTextContent('FIR ST2')

    await act(async () => { jest.advanceTimersByTime(4100) })

    expect(await screen.findByText('SEC ND3')).toBeInTheDocument()
  })
})
