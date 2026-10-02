import React from 'react'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn(), back: jest.fn() }),
  usePathname: () => '/admin/locations',
}))

jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
  getErrorMessage: (err: { response?: { data?: { message?: string } } }, fallback: string) => err?.response?.data?.message ?? fallback,
}))

const createShop = jest.fn()
const updateShop = jest.fn()
jest.mock('@/hooks/useShops', () => ({ useShops: () => ({ createShop, updateShop }) }))

import { apiClient } from '@/api/client'
import { locationService } from '@/api/locationService'
import AdminLocationsPage from '@/app/admin/locations/page'
import { LocationPicker } from '@/components/location/LocationPicker'
import { ShopForm } from '@/components/shops/ShopForm'
import { LocationItem } from '@/types/location'
import { ShopDto } from '@/types/shop'

const api = apiClient as jest.Mocked<typeof apiClient>

const item = (overrides: Partial<LocationItem> = {}): LocationItem => ({
  id: '1', uniqueId: 'CTY-AAAA-BBBB', name: 'Mumbai', code: null, parentId: '5', isActive: true, childCount: 0, shopCount: 0, ...overrides,
})

const india = item({ id: 'IN', uniqueId: 'CNT-AAAA-BBBB', name: 'India', code: 'IN', parentId: null, childCount: 1 })
const maharashtra = item({ id: '5', uniqueId: 'STA-AAAA-BBBB', name: 'Maharashtra', code: 'MH', parentId: 'IN', childCount: 1 })
const mumbai = item({ id: '7', uniqueId: 'CTY-AAAA-BBBB', name: 'Mumbai', parentId: '5', childCount: 1, shopCount: 2 })
const andheri = item({ id: '9', uniqueId: 'GRP-AAAA-BBBB', name: 'Andheri', parentId: '7', shopCount: 2 })

/** Answers the list calls the way the API does, keyed by the URL. */
function serve(overrides: Record<string, unknown> = {}) {
  const table: Record<string, unknown> = {
    '/locations/countries': [india],
    '/locations/states': [maharashtra],
    '/locations/cities': [mumbai],
    '/locations/groups': [andheri],
    '/locations/groups/9/shops': [{ id: 's1', name: 'Olive Mart', uniqueId: 'SHP-AAAA-BBBB', status: 'Active', city: 'Mumbai' }],
    ...overrides,
  }
  api.get.mockImplementation(async (path: string) => {
    const key = Object.keys(table).find((k) => path.split('?')[0] === k)
    return key ? table[key] : []
  })
}

beforeEach(() => {
  jest.clearAllMocks()
  serve()
})

describe('locationService', () => {
  it('filters each list by its parent', async () => {
    await locationService.list('states', { parentId: 'IN' })
    await locationService.list('cities', { parentId: '5', includeInactive: true })
    await locationService.list('groups', { parentId: '7' })
    await locationService.list('countries')

    expect(api.get.mock.calls.map((c) => c[0])).toEqual([
      '/locations/states?countryCode=IN',
      '/locations/cities?stateId=5&includeInactive=true',
      '/locations/groups?cityId=7',
      '/locations/countries',
    ])
  })

  it('creates each level with the body the API expects', async () => {
    await locationService.create('countries', undefined, { name: 'India', code: 'IN' })
    await locationService.create('states', 'IN', { name: 'Maharashtra', code: 'MH' })
    await locationService.create('cities', '5', { name: 'Mumbai' })
    await locationService.create('groups', '7', { name: 'Andheri' })

    expect(api.post.mock.calls).toEqual([
      ['/locations/countries', { isoCode: 'IN', name: 'India' }],
      ['/locations/states', { countryCode: 'IN', code: 'MH', name: 'Maharashtra' }],
      ['/locations/cities', { stateId: 5, name: 'Mumbai' }],
      ['/locations/groups', { cityId: 7, name: 'Andheri' }],
    ])
  })

  it('keeps a state code when renaming and sends only what changed for the rest', async () => {
    await locationService.update('states', maharashtra, { name: 'Maharashtra State' })
    await locationService.update('cities', mumbai, { isActive: false })

    expect(api.put.mock.calls).toEqual([
      ['/locations/states/5', { code: 'MH', name: 'Maharashtra State', isActive: true }],
      ['/locations/cities/7', { name: 'Mumbai', isActive: false }],
    ])
  })

  it('places a shop, with or without a group', async () => {
    await locationService.setShopLocation('shop-1', 7, 9)
    await locationService.setShopLocation('shop-1', 7)

    expect(api.put.mock.calls).toEqual([
      ['/locations/shops/shop-1', { cityId: 7, groupId: 9 }],
      ['/locations/shops/shop-1', { cityId: 7, groupId: null }],
    ])
  })
})

describe('Admin places page', () => {
  it('drills down from country to group and lists the shops in the group', async () => {
    render(<AdminLocationsPage />)

    expect(await screen.findByText('Choose a state first.')).toBeInTheDocument()
    await userEvent.click(within(await screen.findByTestId('country-India')).getByRole('button', { name: /India/ }))
    await userEvent.click(within(await screen.findByTestId('state-Maharashtra')).getByRole('button', { name: /Maharashtra/ }))
    await userEvent.click(within(await screen.findByTestId('city-Mumbai')).getByRole('button', { name: /Mumbai/ }))
    await userEvent.click(within(await screen.findByTestId('group-Andheri')).getByRole('button', { name: /Andheri/ }))

    expect(await screen.findByTestId('group-shop-Olive Mart')).toHaveTextContent('SHP-AAAA-BBBB')
    expect(screen.getByTestId('city-Mumbai')).toHaveTextContent('CTY-AAAA-BBBB')
    expect(screen.getByTestId('city-Mumbai')).toHaveTextContent('1 group · 2 shops')
  })

  it('adds a city under the chosen state', async () => {
    api.post.mockResolvedValue(item({ id: '8', name: 'Pune' }))
    render(<AdminLocationsPage />)
    await userEvent.click(within(await screen.findByTestId('country-India')).getByRole('button', { name: /India/ }))
    await userEvent.click(within(await screen.findByTestId('state-Maharashtra')).getByRole('button', { name: /Maharashtra/ }))

    await userEvent.type(await screen.findByLabelText('New City name'), 'Pune')
    await userEvent.click(screen.getByRole('button', { name: 'Add City' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/locations/cities', { stateId: 5, name: 'Pune' }))
    expect(await screen.findByText('City added')).toBeInTheDocument()
  })

  it('needs a code as well as a name for a country', async () => {
    render(<AdminLocationsPage />)
    await screen.findByTestId('country-India')

    await userEvent.type(screen.getByLabelText('New Country name'), 'Nepal')
    expect(screen.getByRole('button', { name: 'Add Country' })).toBeDisabled()

    await userEvent.type(screen.getByLabelText('Country code'), 'np')
    expect(screen.getByRole('button', { name: 'Add Country' })).toBeEnabled()
  })

  it('shows the reason when a delete is refused, and keeps the item', async () => {
    api.delete.mockRejectedValue({ response: { data: { message: 'Mumbai cannot be deleted because it still has 2 shops.' } } })
    render(<AdminLocationsPage />)
    await userEvent.click(within(await screen.findByTestId('country-India')).getByRole('button', { name: /India/ }))
    await userEvent.click(within(await screen.findByTestId('state-Maharashtra')).getByRole('button', { name: /Maharashtra/ }))

    await userEvent.click(within(await screen.findByTestId('city-Mumbai')).getByRole('button', { name: 'Delete' }))
    const dialog = screen.getByText('Delete this city?').closest('div')!.parentElement!
    await userEvent.click(within(dialog).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByText('Mumbai cannot be deleted because it still has 2 shops.')).toBeInTheDocument()
    expect(screen.getByTestId('city-Mumbai')).toBeInTheDocument()
  })

  it('switches a place off', async () => {
    api.put.mockResolvedValue({})
    render(<AdminLocationsPage />)
    await userEvent.click(within(await screen.findByTestId('country-India')).getByRole('button', { name: /India/ }))
    await userEvent.click(within(await screen.findByTestId('state-Maharashtra')).getByRole('button', { name: /Maharashtra/ }))

    await userEvent.click(within(await screen.findByTestId('city-Mumbai')).getByRole('button', { name: 'Switch off' }))

    await waitFor(() => expect(api.put).toHaveBeenCalledWith('/locations/cities/7', { name: 'Mumbai', isActive: false }))
  })

  it('renames a place', async () => {
    api.put.mockResolvedValue({})
    render(<AdminLocationsPage />)
    await userEvent.click(within(await screen.findByTestId('country-India')).getByRole('button', { name: /India/ }))
    await userEvent.click(within(await screen.findByTestId('state-Maharashtra')).getByRole('button', { name: /Maharashtra/ }))

    await userEvent.click(within(await screen.findByTestId('city-Mumbai')).getByRole('button', { name: 'Rename' }))
    const box = screen.getByLabelText('Rename Mumbai')
    await userEvent.clear(box)
    await userEvent.type(box, 'Bombay')
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(api.put).toHaveBeenCalledWith('/locations/cities/7', { name: 'Bombay', isActive: true }))
  })
})

describe('LocationPicker', () => {
  it('offers each level only after the one above is chosen, and clears the ones below when it changes', async () => {
    const onChange = jest.fn()
    const { rerender } = render(<LocationPicker value={{ countryCode: 'IN' }} onChange={onChange} />)

    await screen.findByRole('option', { name: 'Maharashtra' })
    expect(screen.getByLabelText('City')).toBeDisabled()

    fireEvent.change(screen.getByLabelText('State'), { target: { value: '5' } })
    expect(onChange).toHaveBeenLastCalledWith({ countryCode: 'IN', stateId: 5 })

    rerender(<LocationPicker value={{ countryCode: 'IN', stateId: 5, cityId: 7, groupId: 9 }} onChange={onChange} />)
    await screen.findByRole('option', { name: 'Andheri' })
    fireEvent.change(screen.getByLabelText('City'), { target: { value: '' } })
    expect(onChange).toHaveBeenLastCalledWith({ countryCode: 'IN', stateId: 5, cityId: undefined })
  })

  it('says so when a state has no cities yet', async () => {
    serve({ '/locations/cities': [] })
    render(<LocationPicker value={{ countryCode: 'IN', stateId: 5 }} onChange={jest.fn()} />)

    expect(await screen.findByText(/No cities here yet/)).toBeInTheDocument()
  })
})

describe('ShopForm', () => {
  const shop = {
    id: 'shop-1', name: 'Olive Mart', status: 'Active', countryCode: 'IN', stateId: 5, cityId: 7, groupId: 9, city: 'Mumbai',
    childShopsCount: 0, createdAt: '', updatedAt: '',
  } as ShopDto

  it('saves the shop, then its city and group', async () => {
    updateShop.mockResolvedValue(undefined)
    api.put.mockResolvedValue({})
    const onSaved = jest.fn()
    render(<ShopForm shop={shop} onSaved={onSaved} onCancel={jest.fn()} />)
    await screen.findByRole('option', { name: 'Andheri' })

    await userEvent.clear(screen.getByLabelText('Shop name'))
    await userEvent.type(screen.getByLabelText('Shop name'), 'Olive Market')
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }))

    await waitFor(() => expect(onSaved).toHaveBeenCalledWith('shop-1'))
    expect(updateShop).toHaveBeenCalledWith('shop-1', expect.objectContaining({ name: 'Olive Market', stateId: 5, status: 'Active' }))
    expect(api.put).toHaveBeenCalledWith('/locations/shops/shop-1', { cityId: 7, groupId: 9 })
  })

  it('creates a shop and then places it', async () => {
    createShop.mockResolvedValue({ id: 'new-1' })
    api.put.mockResolvedValue({})
    const onSaved = jest.fn()
    render(<ShopForm onSaved={onSaved} onCancel={jest.fn()} />)
    await screen.findByRole('option', { name: 'Maharashtra' })

    await userEvent.type(screen.getByLabelText('Shop name'), 'New Mart')
    fireEvent.change(screen.getByLabelText('State'), { target: { value: '5' } })
    await screen.findByRole('option', { name: 'Mumbai' })
    fireEvent.change(screen.getByLabelText('City'), { target: { value: '7' } })
    await userEvent.click(screen.getByRole('button', { name: 'Create shop' }))

    await waitFor(() => expect(onSaved).toHaveBeenCalledWith('new-1'))
    expect(createShop).toHaveBeenCalledWith(expect.objectContaining({ name: 'New Mart', countryCode: 'IN', stateId: 5 }))
    expect(api.put).toHaveBeenCalledWith('/locations/shops/new-1', { cityId: 7, groupId: null })
  })

  it('asks for a name', async () => {
    render(<ShopForm onSaved={jest.fn()} onCancel={jest.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: 'Create shop' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Give the shop a name.')
    expect(createShop).not.toHaveBeenCalled()
  })

  it('shows why the location was refused', async () => {
    updateShop.mockResolvedValue(undefined)
    api.put.mockRejectedValue({ response: { data: { message: 'Andheri is not a group of Pune.' } } })
    render(<ShopForm shop={shop} onSaved={jest.fn()} onCancel={jest.fn()} />)
    await screen.findByRole('option', { name: 'Andheri' })

    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Andheri is not a group of Pune.')
  })
})
