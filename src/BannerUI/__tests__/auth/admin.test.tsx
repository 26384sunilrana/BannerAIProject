import React from 'react'
import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

let search = ''
jest.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(search),
  useRouter: () => ({ replace: jest.fn(), push: jest.fn() }),
  usePathname: () => '/admin/users',
}))

jest.mock('@/context/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'ME-1', email: 'me@example.com', name: 'Me', shopId: null, roles: ['Admin'] } }),
}))

jest.mock('@/api/adminService', () => {
  const actual = jest.requireActual('@/api/adminService')
  return {
    ...actual,
    adminService: {
      listUsers: jest.fn(),
      deactivateUser: jest.fn(),
      activateUser: jest.fn(),
      unlockUser: jest.fn(),
      listSubscriptions: jest.fn(),
      runLifecycle: jest.fn(),
      listAuditLogs: jest.fn(),
    },
  }
})
jest.mock('@/api/subscriptionService', () => ({ subscriptionService: { renewNow: jest.fn() } }))

import { adminService, toQuery } from '@/api/adminService'
import { subscriptionService } from '@/api/subscriptionService'
import { UsersPanel } from '@/app/admin/users/UsersPanel'
import AdminSubscriptionsPage from '@/app/admin/subscriptions/page'
import { AuditPanel, statusTone } from '@/app/admin/audit-log/AuditPanel'
import { Pagination } from '@/components/admin/Pagination'

const admin = adminService as jest.Mocked<typeof adminService>
const subs = subscriptionService as jest.Mocked<typeof subscriptionService>

const user = (overrides: Record<string, unknown> = {}) => ({
  id: 'u1', email: 'olive@example.com', fullName: 'Olive Owner', shopId: 's1', roles: ['ShopOwner'], isActive: true,
  isLockedOut: false, emailVerified: true, createdAt: '2026-10-01T00:00:00Z', lastLoginAttempt: null, ...overrides,
})

const subscription = (overrides: Record<string, unknown> = {}) => ({
  id: 'sub1', shopId: 's1', shopName: 'Olive Mart', planName: 'Silver', status: 2, billingPeriod: 2, currentPrice: 800,
  renewalDate: '2026-12-01T00:00:00Z', autoRenew: true, graceEndsAt: null, ...overrides,
})

beforeEach(() => {
  jest.clearAllMocks()
  search = ''
})

describe('toQuery', () => {
  it('keeps only the values that are set', () => {
    expect(toQuery({ search: 'olive', page: 2, includeInactive: false, shopId: '', status: undefined, flag: true })).toBe('?search=olive&page=2&flag=true')
    expect(toQuery({})).toBe('')
  })
})

describe('Pagination', () => {
  it('shows the range and moves between pages', async () => {
    const onChange = jest.fn()
    render(<Pagination page={2} pageSize={25} total={60} onChange={onChange} />)

    expect(screen.getByTestId('pagination-summary')).toHaveTextContent('Showing 26-50 of 60')
    await userEvent.click(screen.getByRole('button', { name: 'Next' }))
    await userEvent.click(screen.getByRole('button', { name: 'Previous' }))
    expect(onChange).toHaveBeenNthCalledWith(1, 3)
    expect(onChange).toHaveBeenNthCalledWith(2, 1)
  })

  it('has no controls when everything fits, and disables the ends', () => {
    const { rerender } = render(<Pagination page={1} pageSize={25} total={10} onChange={jest.fn()} />)
    expect(screen.queryByRole('button')).toBeNull()

    rerender(<Pagination page={1} pageSize={25} total={60} onChange={jest.fn()} />)
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled()
  })
})

describe('UsersPanel', () => {
  const page = (items: unknown[]) => ({ items, total: items.length, page: 1, pageSize: 25 })

  it('lists users with their role and status, and offers the right actions', async () => {
    admin.listUsers.mockResolvedValue(page([
      user(),
      user({ id: 'u2', email: 'locked@example.com', isLockedOut: true }),
      user({ id: 'u3', email: 'gone@example.com', isActive: false }),
    ]) as never)
    render(<UsersPanel />)

    const row = await screen.findByTestId('user-locked@example.com')
    expect(within(row).getByText('Locked out')).toBeInTheDocument()
    expect(within(row).getByRole('button', { name: 'Unlock' })).toBeInTheDocument()
    expect(within(screen.getByTestId('user-gone@example.com')).getByRole('button', { name: 'Reactivate' })).toBeInTheDocument()
    expect(within(screen.getByTestId('user-olive@example.com')).queryByRole('button', { name: 'Unlock' })).toBeNull()
  })

  it('cannot deactivate yourself', async () => {
    admin.listUsers.mockResolvedValue(page([user({ id: 'me-1', email: 'me@example.com' })]) as never)
    render(<UsersPanel />)

    expect(await screen.findByRole('button', { name: 'Deactivate' })).toBeDisabled()
  })

  it('asks first, then deactivates and reloads', async () => {
    admin.listUsers.mockResolvedValue(page([user()]) as never)
    admin.deactivateUser.mockResolvedValue(user({ isActive: false }) as never)
    render(<UsersPanel />)

    await userEvent.click(await screen.findByRole('button', { name: 'Deactivate' }))
    expect(admin.deactivateUser).not.toHaveBeenCalled()
    await userEvent.click(within(screen.getByText('Deactivate this user?').closest('div')!.parentElement!).getByRole('button', { name: 'Deactivate' }))

    await waitFor(() => expect(admin.deactivateUser).toHaveBeenCalledWith('u1'))
    await waitFor(() => expect(admin.listUsers).toHaveBeenCalledTimes(2))
  })

  it('searches after a short pause, from the first page, and can include deactivated users', async () => {
    admin.listUsers.mockResolvedValue(page([user()]) as never)
    render(<UsersPanel />)
    await screen.findByTestId('user-olive@example.com')

    await userEvent.type(screen.getByLabelText('Search'), 'olive')
    await waitFor(() => expect(admin.listUsers).toHaveBeenLastCalledWith(expect.objectContaining({ search: 'olive', page: 1 })))

    await userEvent.click(screen.getByLabelText('Include deactivated'))
    await waitFor(() => expect(admin.listUsers).toHaveBeenLastCalledWith(expect.objectContaining({ includeInactive: true })))
  })

  it('narrows to one shop from the address', async () => {
    search = 'shopId=s9'
    admin.listUsers.mockResolvedValue(page([]) as never)
    render(<UsersPanel />)

    await waitFor(() => expect(admin.listUsers).toHaveBeenCalledWith(expect.objectContaining({ shopId: 's9' })))
    expect(await screen.findByText('No users match.')).toBeInTheDocument()
    expect(screen.getByText('Show all')).toBeInTheDocument()
  })

  it('shows why the list could not load', async () => {
    admin.listUsers.mockRejectedValue({ response: { status: 403 } })
    render(<UsersPanel />)

    expect(await screen.findByRole('alert')).toHaveTextContent(/permission/i)
  })
})

describe('AdminSubscriptionsPage', () => {
  const page = (items: unknown[]) => ({ items, total: items.length, page: 1, pageSize: 25 })

  it('says Reactivate for shops that are cut off and Renew early for running ones', async () => {
    admin.listSubscriptions.mockResolvedValue(page([
      subscription(),
      subscription({ id: 'sub2', shopName: 'Gone Shop', status: 7, renewalDate: '2026-09-01T00:00:00Z', graceEndsAt: '2026-09-08T00:00:00Z' }),
      subscription({ id: 'sub3', shopName: 'Old Shop', status: 8 }),
    ]) as never)
    render(<AdminSubscriptionsPage />)

    expect(within(await screen.findByTestId('subscription-Olive Mart')).getByRole('button', { name: 'Renew early' })).toBeInTheDocument()
    const gone = screen.getByTestId('subscription-Gone Shop')
    expect(within(gone).getByRole('button', { name: 'Reactivate' })).toBeInTheDocument()
    expect(within(gone).getByText('Expired')).toBeInTheDocument()
    expect(within(screen.getByTestId('subscription-Old Shop')).queryByRole('button')).toBeNull()
  })

  it('confirms, renews and reloads', async () => {
    admin.listSubscriptions.mockResolvedValue(page([subscription({ status: 7 })]) as never)
    subs.renewNow.mockResolvedValue(undefined)
    render(<AdminSubscriptionsPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Reactivate' }))
    expect(subs.renewNow).not.toHaveBeenCalled()
    const dialog = screen.getByText('Reactivate this shop?').closest('div')!.parentElement!
    await userEvent.click(within(dialog).getByRole('button', { name: 'Reactivate' }))

    await waitFor(() => expect(subs.renewNow).toHaveBeenCalledWith('sub1'))
    await waitFor(() => expect(admin.listSubscriptions).toHaveBeenCalledTimes(2))
  })

  it('filters by status', async () => {
    admin.listSubscriptions.mockResolvedValue(page([]) as never)
    render(<AdminSubscriptionsPage />)
    await screen.findByText('No subscriptions match.')

    await userEvent.selectOptions(screen.getByLabelText('Status'), '6')

    await waitFor(() => expect(admin.listSubscriptions).toHaveBeenLastCalledWith(expect.objectContaining({ status: 6, page: 1 })))
  })

  it('runs the renewal job on demand and reports what it did', async () => {
    admin.listSubscriptions.mockResolvedValue(page([]) as never)
    admin.runLifecycle.mockResolvedValue({ planChangesApplied: 0, autoRenewed: 2, renewalsFailed: 1, enteredGrace: 1, expired: 3, messagesSent: 8 })
    render(<AdminSubscriptionsPage />)

    await userEvent.click(await screen.findByRole('button', { name: 'Run renewal job now' }))

    expect(await screen.findByTestId('lifecycle-report')).toHaveTextContent('2 renewed, 1 payments failed, 1 started grace, 3 expired, 8 messages sent')
  })
})

describe('AuditPanel', () => {
  const entry = (overrides: Record<string, unknown> = {}) => ({
    id: 'a1', occurredAt: '2026-10-02T10:00:00Z', userId: 'u1', userEmail: 'olive@example.com', shopId: 's1',
    method: 'POST', path: '/api/banners', statusCode: 201, ipAddress: '10.0.0.5', durationMs: 42, ...overrides,
  })

  it('colours results by severity', () => {
    expect(statusTone(200)).toMatch(/green/)
    expect(statusTone(403)).toMatch(/amber/)
    expect(statusTone(500)).toMatch(/red/)
  })

  it('lists entries and links a user to their own activity', async () => {
    admin.listAuditLogs.mockResolvedValue({ items: [entry(), entry({ id: 'a2', userEmail: null, userId: null, path: '/api/authentication/login', statusCode: 401 })], total: 2, page: 1, pageSize: 50 } as never)
    render(<AuditPanel />)

    expect(await screen.findByText('/api/banners')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'olive@example.com' })).toHaveAttribute('href', '/admin/audit-log?userId=u1')
    expect(screen.getByText('not signed in')).toBeInTheDocument()
    expect(screen.getByText('401')).toBeInTheDocument()
  })

  it('filters to failures and to one user from the address', async () => {
    search = 'userId=u1'
    admin.listAuditLogs.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 50 } as never)
    render(<AuditPanel />)
    await screen.findByText('Nothing recorded for these filters.')
    expect(admin.listAuditLogs).toHaveBeenCalledWith(expect.objectContaining({ userId: 'u1' }))

    await userEvent.click(screen.getByLabelText(/Failures only/))

    await waitFor(() => expect(admin.listAuditLogs).toHaveBeenLastCalledWith(expect.objectContaining({ failuresOnly: true, userId: 'u1' })))
  })
})
