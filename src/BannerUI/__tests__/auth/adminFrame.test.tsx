import React from 'react'
import { render, screen } from '@testing-library/react'
import fs from 'node:fs'
import path from 'node:path'

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  usePathname: () => '/admin/ad-rates',
}))
jest.mock('@/hooks/useShopTimeZone', () => ({
  useShopTimeZone: () => ({ timeZoneId: 'UTC', source: 'shop', ownTimeZoneId: null, loaded: true }),
  forgetShopTimeZone: jest.fn(),
}))
jest.mock('@/components/layout/NotificationBell', () => ({ NotificationBell: () => null }))
jest.mock('@/components/layout/VerifyEmailBanner', () => ({ VerifyEmailBanner: () => null }))
jest.mock('@/components/location/LocationPicker', () => ({ LocationPicker: () => null }))
jest.mock('@/context/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'a1', email: 'admin@example.com', name: 'Admin User', shopId: null, roles: ['Admin'] },
    ready: true,
    signedOutRedirect: null,
    hasRole: (role: string) => role === 'Admin',
    logout: jest.fn(),
  }),
}))
jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn().mockResolvedValue([]), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
  API_ORIGIN: 'http://api.test',
  getErrorMessage: (_err: unknown, fallback: string) => fallback,
}))

import AdminLayout from '@/app/admin/layout'
import AdRatesPage from '@/app/admin/ad-rates/page'
import AdReportsPage from '@/app/admin/ad-reports/page'

/** Every page.tsx under app/admin, however deep. */
const adminPages = (dir: string): string[] =>
  fs.readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name)
    if (entry.isDirectory()) return adminPages(full)
    return entry.name === 'page.tsx' ? [full] : []
  })

describe('Pages under /admin share one page frame', () => {
  it('the admin layout is the only place under app/admin that uses the page frame', () => {
    const root = path.join(__dirname, '..', '..', 'src', 'app', 'admin')
    const pages = adminPages(root)
    expect(pages.length).toBeGreaterThan(5)
    const wrapped = pages.filter((file) => /AppShell/.test(fs.readFileSync(file, 'utf8'))).map((file) => path.relative(root, file))
    // a page that wraps itself is drawn inside the frame the layout already gives it: the menu bar shows twice
    expect(wrapped).toEqual([])
  })

  it.each([
    ['Ad rates', AdRatesPage],
    ['Ad report', AdReportsPage],
  ])('%s shows exactly one main menu under the admin layout', async (_name, Page) => {
    render(
      <AdminLayout>
        <Page />
      </AdminLayout>
    )
    expect(await screen.findAllByRole('navigation', { name: 'Main' })).toHaveLength(1)
    expect(screen.getAllByText('Banner AI')).toHaveLength(1)
  })
})
