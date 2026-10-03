import React from 'react'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  usePathname: () => '/takeover',
}))
jest.mock('@/components/layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }))

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
import { Takeover } from '@/api/takeoverService'
import TakeoverPage from '@/app/takeover/page'

const api = apiClient as jest.Mocked<typeof apiClient>

const takeover = (overrides: Partial<Takeover> = {}): Takeover => ({
  id: 't1', direction: 'Incoming', existingShopName: 'Olive Mart', newShopName: 'Olive Mart (new)', requesterName: 'Nina New', requesterEmail: 'n***@example.com',
  existingOwnerName: 'Olive Owner', status: 'Requested', associates: null, decidedByName: null, decidedByAdmin: false, note: null, createdAt: '2030-01-07T10:00:00Z',
  decidedAt: null, can: ['confirm', 'decline'], ...overrides,
})

function serve(items: Takeover[]) {
  api.get.mockImplementation(async (path: string) => {
    if (path === '/shop-takeovers') return items
    if (path.startsWith('/shops/')) return { name: 'Olive Mart', address: '5 Market Street', postalCode: '400001' }
    throw new Error(path)
  })
}

beforeEach(() => {
  jest.clearAllMocks()
  mockRoles = ['ShopOwner']
})

describe('Takeover page: the old owner', () => {
  it('shows who asks, with a masked address, and hands the shop over with the password', async () => {
    serve([takeover()])
    api.post.mockResolvedValue(takeover({ status: 'Completed', associates: 'Keep', can: [] }))
    render(<TakeoverPage />)

    const card = await screen.findByTestId('takeover-t1')
    expect(card).toHaveTextContent('Asked by Nina New (n***@example.com)')
    expect(card).toHaveTextContent('Waiting for an answer')

    await userEvent.click(within(card).getByRole('button', { name: 'Hand the shop over' }))
    await userEvent.click(within(card).getByRole('button', { name: 'Yes, hand it over' }))
    expect(await within(card).findByRole('alert')).toHaveTextContent('Choose what happens to the associates')

    await userEvent.click(within(card).getByLabelText(/They stay/))
    await userEvent.click(within(card).getByRole('button', { name: 'Yes, hand it over' }))
    expect(await within(card).findByRole('alert')).toHaveTextContent('Enter your password')

    await userEvent.type(within(card).getByLabelText('Your password'), 'Password123!')
    await userEvent.click(within(card).getByRole('button', { name: 'Yes, hand it over' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-takeovers/t1/confirm', { associates: 'Keep', password: 'Password123!' }))
    expect(await screen.findByText('The shop was handed over.')).toBeInTheDocument()
  })

  it('can choose that the associates change too', async () => {
    serve([takeover()])
    api.post.mockResolvedValue(takeover({ status: 'Completed', associates: 'Replace', can: [] }))
    render(<TakeoverPage />)
    const card = await screen.findByTestId('takeover-t1')

    await userEvent.click(within(card).getByRole('button', { name: 'Hand the shop over' }))
    await userEvent.click(within(card).getByLabelText(/They change too/))
    await userEvent.type(within(card).getByLabelText('Your password'), 'Password123!')
    await userEvent.click(within(card).getByRole('button', { name: 'Yes, hand it over' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-takeovers/t1/confirm', { associates: 'Replace', password: 'Password123!' }))
  })

  it('shows the server reason when the password is wrong', async () => {
    serve([takeover()])
    api.post.mockRejectedValue({ response: { data: { message: 'Your password is not right.' } } })
    render(<TakeoverPage />)
    const card = await screen.findByTestId('takeover-t1')

    await userEvent.click(within(card).getByRole('button', { name: 'Hand the shop over' }))
    await userEvent.click(within(card).getByLabelText(/They stay/))
    await userEvent.type(within(card).getByLabelText('Your password'), 'nope')
    await userEvent.click(within(card).getByRole('button', { name: 'Yes, hand it over' }))

    expect(await within(card).findByRole('alert')).toHaveTextContent('Your password is not right.')
  })

  it('declines with an optional reason', async () => {
    serve([takeover()])
    api.post.mockResolvedValue(takeover({ status: 'Declined', can: [] }))
    render(<TakeoverPage />)
    const card = await screen.findByTestId('takeover-t1')

    await userEvent.click(within(card).getByRole('button', { name: 'Decline' }))
    await userEvent.type(within(card).getByLabelText('Reason (optional)'), 'I have not sold the shop')
    await userEvent.click(within(card).getByRole('button', { name: 'Decline the request' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-takeovers/t1/decline', { note: 'I have not sold the shop' }))
  })
})

describe('Takeover page: the new owner', () => {
  it('starts the form from the shop details, and sends the request', async () => {
    serve([])
    api.post.mockResolvedValue(takeover({ direction: 'Outgoing', can: ['cancel'] }))
    render(<TakeoverPage />)

    await waitFor(() => expect(screen.getByLabelText('Street address')).toHaveValue('5 Market Street'))
    expect(screen.getByLabelText('Shop name')).toHaveValue('Olive Mart')
    expect(screen.getByLabelText('Postal code')).toHaveValue('400001')

    await userEvent.click(screen.getByRole('button', { name: 'Ask the owner' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-takeovers', { name: 'Olive Mart', address: '5 Market Street', postalCode: '400001' }))
    expect(await screen.findByText(/The owner of the shop has been told/)).toBeInTheDocument()
  })

  it('asks for what is missing and shows why a request was refused', async () => {
    serve([])
    api.get.mockImplementation(async (path: string) => (path === '/shop-takeovers' ? [] : { name: '', address: '', postalCode: '' }))
    api.post.mockRejectedValue({ response: { data: { message: 'No shop with that name and address was found.' } } })
    render(<TakeoverPage />)
    await screen.findByText('Nobody has asked.')

    await userEvent.click(screen.getByRole('button', { name: 'Ask the owner' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Give the name of the shop')

    await userEvent.type(screen.getByLabelText('Shop name'), 'Ghost Shop')
    await userEvent.type(screen.getByLabelText('Street address'), '1 Nowhere')
    await userEvent.click(screen.getByRole('button', { name: 'Ask the owner' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('No shop with that name and address was found.')
  })

  it('shows the asked request and cancels it', async () => {
    serve([takeover({ id: 't2', direction: 'Outgoing', can: ['cancel'] })])
    api.post.mockResolvedValue(takeover({ id: 't2', direction: 'Outgoing', status: 'Cancelled', can: [] }))
    render(<TakeoverPage />)
    const card = await screen.findByTestId('takeover-t2')

    await userEvent.click(within(card).getByRole('button', { name: 'Cancel the request' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-takeovers/t2/cancel'))
    expect(within(card).queryByRole('button', { name: 'Hand the shop over' })).toBeNull()
  })

  it('tells how a finished takeover went', async () => {
    serve([takeover({ id: 't3', direction: 'Outgoing', status: 'Completed', associates: 'Replace', decidedByName: 'Olive Owner', can: [] })])
    render(<TakeoverPage />)

    expect(await screen.findByTestId('takeover-t3')).toHaveTextContent('The old shop was closed and the new owner’s shop took its place. Answered by Olive Owner.')
  })
})

describe('Takeover page: the administrator', () => {
  beforeEach(() => {
    mockRoles = ['Admin']
  })

  it('lists every request and answers for an owner who cannot be reached, with a reason', async () => {
    serve([takeover({ direction: 'Admin', can: ['confirm-as-admin', 'decline'] })])
    api.post.mockResolvedValue(takeover({ direction: 'Admin', status: 'Completed', associates: 'Keep', decidedByAdmin: true, decidedByName: 'Ops', can: [] }))
    render(<TakeoverPage />)
    const card = await screen.findByTestId('takeover-t1')
    expect(card).toHaveTextContent('current owner Olive Owner')
    expect(screen.queryByText('Ask to take a shop over')).toBeNull()

    await userEvent.click(within(card).getByRole('button', { name: 'Confirm for the owner' }))
    await userEvent.click(within(card).getByLabelText(/They stay/))
    await userEvent.click(within(card).getByRole('button', { name: 'Confirm the takeover' }))
    expect(await within(card).findByRole('alert')).toHaveTextContent('Say why you are answering for the owner')

    await userEvent.type(within(card).getByLabelText('Why are you answering for the owner?'), 'Sale papers sent by post')
    await userEvent.click(within(card).getByRole('button', { name: 'Confirm the takeover' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/shop-takeovers/t1/confirm-as-admin', { associates: 'Keep', note: 'Sale papers sent by post' }))
  })
})
