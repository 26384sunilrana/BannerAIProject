import React from 'react'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

const replace = jest.fn()
let search = ''
jest.mock('next/navigation', () => ({
  useRouter: () => ({ replace, push: jest.fn() }),
  useSearchParams: () => new URLSearchParams(search),
  usePathname: () => '/account',
}))
jest.mock('@/components/layout/AppShell', () => ({ AppShell: ({ children }: { children: React.ReactNode }) => <>{children}</> }))

const logout = jest.fn()
jest.mock('@/context/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'ME-1', email: 'me@example.com', name: 'Me', shopId: 'shop-1', roles: ['ShopOwner'] },
    logout,
  }),
}))

jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
  getErrorMessage: (err: { response?: { data?: { message?: string } } }, fallback: string) => err?.response?.data?.message ?? fallback,
}))
jest.mock('@/api/teamService', () => ({
  teamService: { getTeam: jest.fn(), transferOwnership: jest.fn(), addSalesExecutive: jest.fn(), removeSalesExecutive: jest.fn(), setApprovers: jest.fn() },
}))
jest.mock('@/api/subscriptionService', () => ({ subscriptionService: { getCurrent: jest.fn() } }))
jest.mock('@/api/adminService', () => {
  const actual = jest.requireActual('@/api/adminService')
  return {
    ...actual,
    adminService: {
      listUsers: jest.fn(), deactivateUser: jest.fn(), activateUser: jest.fn(), unlockUser: jest.fn(),
      moveUser: jest.fn(), assignOwner: jest.fn(), listShopOptions: jest.fn(),
    },
  }
})

import { apiClient } from '@/api/client'
import { teamService } from '@/api/teamService'
import { subscriptionService } from '@/api/subscriptionService'
import { adminService } from '@/api/adminService'
import AccountPage from '@/app/account/page'
import TeamPage from '@/app/team/page'
import { UsersPanel } from '@/app/admin/users/UsersPanel'
import { LoginForm } from '@/app/login/LoginForm'
import { clearSession, getAccessToken } from '@/lib/session'

const api = apiClient as jest.Mocked<typeof apiClient>
const team = teamService as jest.Mocked<typeof teamService>
const admin = adminService as jest.Mocked<typeof adminService>

const account = {
  id: 'ME-1', email: 'me@example.com', firstName: 'Olive', lastName: 'Owner', phoneNumber: '+91 98765 43210', shopId: 'shop-1',
  roles: ['ShopOwner'], emailVerified: true, createdAt: '2026-10-01T00:00:00Z',
}

beforeEach(() => {
  jest.clearAllMocks()
  clearSession()
  localStorage.clear()
  search = ''
  api.get.mockResolvedValue(account)
})

describe('Account page', () => {
  it('shows who you are and fills in your details', async () => {
    render(<AccountPage />)

    expect(await screen.findByDisplayValue('Olive')).toBeInTheDocument()
    expect(screen.getByText(/me@example.com · Shop owner/)).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toBeDisabled()
    expect(screen.getByLabelText('Phone')).toHaveValue('+91 98765 43210')
  })

  it('saves changed details, and sends a blank phone as none', async () => {
    api.put.mockResolvedValue({ ...account, firstName: 'Olivia', phoneNumber: null })
    render(<AccountPage />)
    await screen.findByDisplayValue('Olive')

    await userEvent.clear(screen.getByLabelText('First name'))
    await userEvent.type(screen.getByLabelText('First name'), 'Olivia')
    await userEvent.clear(screen.getByLabelText('Phone'))
    await userEvent.click(screen.getByRole('button', { name: 'Save details' }))

    await waitFor(() => expect(api.put).toHaveBeenCalledWith('/account', { firstName: 'Olivia', lastName: 'Owner', phoneNumber: null }))
    expect(await screen.findByText('Your details were saved.')).toBeInTheDocument()
  })

  it('asks for a first name, and shows what the server says when it refuses', async () => {
    render(<AccountPage />)
    await screen.findByDisplayValue('Olive')

    await userEvent.clear(screen.getByLabelText('First name'))
    await userEvent.click(screen.getByRole('button', { name: 'Save details' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('Enter your first name.')
    expect(api.put).not.toHaveBeenCalled()

    api.put.mockRejectedValue({ response: { data: { message: 'The phone number can have digits.' } } })
    await userEvent.type(screen.getByLabelText('First name'), 'Olive')
    await userEvent.click(screen.getByRole('button', { name: 'Save details' }))
    expect(await screen.findByText('The phone number can have digits.')).toBeInTheDocument()
  })

  it('checks the new password before asking the server', async () => {
    render(<AccountPage />)
    await screen.findByDisplayValue('Olive')
    const submit = () => userEvent.click(screen.getByRole('button', { name: 'Change password' }))

    await submit()
    expect(await screen.findByText('Enter your current password.')).toBeInTheDocument()

    await userEvent.type(screen.getByLabelText('Current password'), 'Password123!')
    await userEvent.type(screen.getByLabelText('New password'), 'short')
    await submit()
    expect(await screen.findByText('The new password needs at least 8 characters.')).toBeInTheDocument()

    await userEvent.clear(screen.getByLabelText('New password'))
    await userEvent.type(screen.getByLabelText('New password'), 'BetterPass456!')
    await userEvent.type(screen.getByLabelText('New password again'), 'Different789!')
    await submit()
    expect(await screen.findByText('The two new passwords are not the same.')).toBeInTheDocument()
    expect(api.post).not.toHaveBeenCalled()
  })

  it('changes the password, keeps this device signed in with the new token, and clears the form', async () => {
    api.post.mockResolvedValue({ message: 'ok', tokens: { accessToken: 'header.payload.sig', refreshToken: '' } })
    render(<AccountPage />)
    await screen.findByDisplayValue('Olive')

    await userEvent.type(screen.getByLabelText('Current password'), 'Password123!')
    await userEvent.type(screen.getByLabelText('New password'), 'BetterPass456!')
    await userEvent.type(screen.getByLabelText('New password again'), 'BetterPass456!')
    await userEvent.click(screen.getByRole('button', { name: 'Change password' }))

    await waitFor(() =>
      expect(api.post).toHaveBeenCalledWith('/account/change-password', {
        currentPassword: 'Password123!', newPassword: 'BetterPass456!', confirmPassword: 'BetterPass456!',
      })
    )
    expect(await screen.findByText(/Your other devices were signed out/)).toBeInTheDocument()
    expect(getAccessToken()).toBe('header.payload.sig')
    expect(screen.getByLabelText('Current password')).toHaveValue('')
  })

  it('shows why the password change was refused', async () => {
    api.post.mockRejectedValue({ response: { data: { message: 'Your current password is not right.' } } })
    render(<AccountPage />)
    await screen.findByDisplayValue('Olive')

    await userEvent.type(screen.getByLabelText('Current password'), 'wrong')
    await userEvent.type(screen.getByLabelText('New password'), 'BetterPass456!')
    await userEvent.type(screen.getByLabelText('New password again'), 'BetterPass456!')
    await userEvent.click(screen.getByRole('button', { name: 'Change password' }))

    expect(await screen.findByText('Your current password is not right.')).toBeInTheDocument()
  })

  it('signs out everywhere after a confirmation, then goes to the sign-in page', async () => {
    api.post.mockResolvedValue({ sessionsEnded: 3 })
    render(<AccountPage />)
    await screen.findByDisplayValue('Olive')

    await userEvent.click(screen.getByRole('button', { name: 'Sign out everywhere' }))
    expect(api.post).not.toHaveBeenCalled()
    const dialog = screen.getByText('Sign out everywhere?').closest('div')!.parentElement!
    await userEvent.click(within(dialog).getByRole('button', { name: 'Sign out everywhere' }))

    await waitFor(() => expect(api.post).toHaveBeenCalledWith('/account/sign-out-everywhere', {}))
    await waitFor(() => expect(logout).toHaveBeenCalledWith('/login'))
  })

  it('says so when the account cannot be loaded', async () => {
    api.get.mockRejectedValue({ response: { data: { message: 'Not allowed' } } })
    render(<AccountPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Not allowed')
  })
})

describe('Team page: handing the shop over', () => {
  const shopTeam = {
    shopId: 'shop-1', ownerUserId: 'ME-1', ownerIsApprover: true, maxSalesExecutives: 2,
    salesExecutives: [{ userId: 'sam-1', email: 'sam@example.com', fullName: 'Sam Seller', isApprover: false }],
  }

  beforeEach(() => {
    team.getTeam.mockResolvedValue(shopTeam as never)
    ;(subscriptionService.getCurrent as jest.Mock).mockResolvedValue({ status: 2 })
  })

  it('needs the password, hands over, signs out and explains on the sign-in page', async () => {
    team.transferOwnership.mockResolvedValue({ newOwnerEmail: 'sam@example.com' })
    render(<TeamPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Hand the shop over' }))
    const dialog = screen.getByRole('dialog', { name: 'Hand the shop over' })
    expect(dialog).toHaveTextContent('Sam Seller becomes the owner')

    await userEvent.click(within(dialog).getByRole('button', { name: 'Hand the shop over' }))
    expect(await within(dialog).findByText('Enter your password to confirm.')).toBeInTheDocument()
    expect(team.transferOwnership).not.toHaveBeenCalled()

    await userEvent.type(within(dialog).getByLabelText('Your password'), 'Password123!')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Hand the shop over' }))

    await waitFor(() => expect(team.transferOwnership).toHaveBeenCalledWith('shop-1', 'sam-1', 'Password123!'))
    await waitFor(() => expect(logout).toHaveBeenCalledWith('/login?handover=1'))
  })

  it('stays open and shows the reason when the password is wrong', async () => {
    team.transferOwnership.mockRejectedValue({ response: { data: { message: 'Your password is not right.' } } })
    render(<TeamPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Hand the shop over' }))
    const dialog = screen.getByRole('dialog', { name: 'Hand the shop over' })
    await userEvent.type(within(dialog).getByLabelText('Your password'), 'nope')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Hand the shop over' }))

    expect(await within(dialog).findByText('Your password is not right.')).toBeInTheDocument()
    expect(replace).not.toHaveBeenCalled()
    await userEvent.click(within(dialog).getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('dialog', { name: 'Hand the shop over' })).toBeNull()
  })
})

describe('Admin users: moving and making an owner', () => {
  const page = (items: unknown[]) => ({ items, total: items.length, page: 1, pageSize: 25 })
  const user = (overrides: Record<string, unknown> = {}) => ({
    id: 'u1', email: 'sam@example.com', fullName: 'Sam Seller', shopId: 'shop-1', roles: ['SalesExecutive'], isActive: true,
    isLockedOut: false, emailVerified: true, createdAt: '2026-10-01T00:00:00Z', lastLoginAttempt: null, ...overrides,
  })

  it('offers Move and Make owner for sales executives only', async () => {
    admin.listUsers.mockResolvedValue(page([user(), user({ id: 'u2', email: 'olive@example.com', roles: ['ShopOwner'] })]) as never)
    render(<UsersPanel />)

    const sam = await screen.findByTestId('user-sam@example.com')
    expect(within(sam).getByRole('button', { name: 'Move' })).toBeInTheDocument()
    expect(within(sam).getByRole('button', { name: 'Make owner' })).toBeInTheDocument()
    const olive = screen.getByTestId('user-olive@example.com')
    expect(within(olive).queryByRole('button', { name: 'Move' })).toBeNull()
    expect(within(olive).queryByRole('button', { name: 'Make owner' })).toBeNull()
  })

  it('moves a sales executive to the chosen shop, leaving their own shop out of the choices', async () => {
    admin.listUsers.mockResolvedValue(page([user()]) as never)
    admin.listShopOptions.mockResolvedValue([{ id: 'shop-1', name: 'Olive Mart' }, { id: 'shop-2', name: 'Beta Bakery' }])
    admin.moveUser.mockResolvedValue({ toShopId: 'shop-2' })
    render(<UsersPanel />)

    await userEvent.click(within(await screen.findByTestId('user-sam@example.com')).getByRole('button', { name: 'Move' }))
    const dialog = await screen.findByRole('dialog', { name: 'Move to another shop' })
    await within(dialog).findByRole('option', { name: 'Beta Bakery' })
    expect(within(dialog).queryByRole('option', { name: 'Olive Mart' })).toBeNull()

    await userEvent.click(within(dialog).getByRole('button', { name: 'Move' }))
    expect(await within(dialog).findByText('Choose the shop to move them to.')).toBeInTheDocument()

    await userEvent.selectOptions(within(dialog).getByLabelText('Move to'), 'shop-2')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Move' }))

    await waitFor(() => expect(admin.moveUser).toHaveBeenCalledWith('u1', 'shop-2'))
    expect(await screen.findByText('sam@example.com was moved.')).toBeInTheDocument()
  })

  it('shows why a move was refused and keeps the dialog open', async () => {
    admin.listUsers.mockResolvedValue(page([user()]) as never)
    admin.listShopOptions.mockResolvedValue([{ id: 'shop-2', name: 'Beta Bakery' }])
    admin.moveUser.mockRejectedValue({ response: { data: { message: 'The shop to move to already has 2 sales executives' } } })
    render(<UsersPanel />)

    await userEvent.click(within(await screen.findByTestId('user-sam@example.com')).getByRole('button', { name: 'Move' }))
    const dialog = await screen.findByRole('dialog', { name: 'Move to another shop' })
    await within(dialog).findByRole('option', { name: 'Beta Bakery' })
    await userEvent.selectOptions(within(dialog).getByLabelText('Move to'), 'shop-2')
    await userEvent.click(within(dialog).getByRole('button', { name: 'Move' }))

    expect(await within(dialog).findByText('The shop to move to already has 2 sales executives')).toBeInTheDocument()
  })

  it('asks first, then makes the executive the owner of their shop', async () => {
    admin.listUsers.mockResolvedValue(page([user()]) as never)
    admin.assignOwner.mockResolvedValue({ newOwnerEmail: 'sam@example.com' })
    render(<UsersPanel />)

    await userEvent.click(within(await screen.findByTestId('user-sam@example.com')).getByRole('button', { name: 'Make owner' }))
    expect(admin.assignOwner).not.toHaveBeenCalled()
    const dialog = screen.getByText('Make this person the owner?').closest('div')!.parentElement!
    await userEvent.click(within(dialog).getByRole('button', { name: 'Make owner' }))

    await waitFor(() => expect(admin.assignOwner).toHaveBeenCalledWith('shop-1', 'u1'))
  })
})

describe('Sign-in page after a hand-over', () => {
  it('explains why the person was signed out', () => {
    search = 'handover=1'
    render(<LoginForm />)

    expect(screen.getByRole('status')).toHaveTextContent('The shop was handed over. Sign in again with your own login.')
  })

  it('shows no such note normally', () => {
    render(<LoginForm />)

    expect(screen.queryByRole('status')).toBeNull()
  })
})
