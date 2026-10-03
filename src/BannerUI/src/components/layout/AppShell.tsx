'use client'

import React from 'react'
import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'
import { useAuth } from '@/context/AuthContext'
import { RequireAuth } from '@/components/auth/RequireAuth'
import { Roles } from '@/lib/session'
import { NotificationBell } from './NotificationBell'

interface NavItem {
  href: string
  label: string
  roles?: string[]
}

const NAV: NavItem[] = [
  { href: '/dashboard', label: 'Home' },
  { href: '/banners', label: 'Banners', roles: [Roles.ShopOwner, Roles.SalesExecutive] },
  { href: '/banners/calendar', label: 'Calendar', roles: [Roles.ShopOwner, Roles.SalesExecutive] },
  { href: '/ads', label: 'Ads', roles: [Roles.Admin, Roles.ShopOwner, Roles.SalesExecutive] },
  { href: '/media', label: 'My files', roles: [Roles.ShopOwner, Roles.SalesExecutive] },
  { href: '/approvals', label: 'Approvals', roles: [Roles.ShopOwner, Roles.SalesExecutive] },
  { href: '/team', label: 'Team', roles: [Roles.ShopOwner] },
  { href: '/subscription', label: 'Subscription', roles: [Roles.ShopOwner] },
  { href: '/shops', label: 'My shop', roles: [Roles.ShopOwner] },
  { href: '/default-board', label: 'Default board', roles: [Roles.ShopOwner] },
  { href: '/display', label: 'Shop screen', roles: [Roles.ShopOwner, Roles.SalesExecutive] },
  { href: '/shops', label: 'Shops', roles: [Roles.Admin] },
  { href: '/admin/locations', label: 'Places', roles: [Roles.Admin] },
  { href: '/admin/ad-rates', label: 'Ad rates', roles: [Roles.Admin] },
  { href: '/admin/ad-reports', label: 'Ad report', roles: [Roles.Admin] },
  { href: '/admin/subscription-plans', label: 'Plans', roles: [Roles.Admin] },
  { href: '/admin/subscriptions', label: 'Subscriptions', roles: [Roles.Admin] },
  { href: '/admin/users', label: 'Users', roles: [Roles.Admin] },
  { href: '/admin/audit-log', label: 'Activity', roles: [Roles.Admin] },
]

interface AppShellProps {
  children: React.ReactNode
  /** Restrict the page itself to these roles. */
  roles?: string[]
}

/** Signed-in page frame: top navigation with the links the user's role allows, and sign out. */
export function AppShell({ children, roles }: AppShellProps) {
  return (
    <RequireAuth roles={roles}>
      <Frame>{children}</Frame>
    </RequireAuth>
  )
}

function Frame({ children }: { children: React.ReactNode }) {
  const { user, logout, hasRole } = useAuth()
  const pathname = usePathname()
  const router = useRouter()

  const links = NAV.filter((item) => !item.roles || item.roles.some((role) => hasRole(role)))

  const signOut = async () => {
    await logout()
    router.replace('/login')
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm border-b border-gray-200" aria-label="Main">
        <div className="max-w-6xl mx-auto px-6 py-3 flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-6">
            <Link href="/dashboard" className="text-lg font-bold text-gray-900">
              Banner AI
            </Link>
            <ul className="flex flex-wrap gap-4">
              {links.map((item) => {
                // the most specific link is the active one (Calendar, not also Banners, on /banners/calendar)
                const matches = (href: string) => pathname === href || (href !== '/dashboard' && !!pathname?.startsWith(href + '/'))
                const best = links.filter((l) => matches(l.href)).sort((a, b) => b.href.length - a.href.length)[0]
                const active = best?.href === item.href && best?.label === item.label
                return (
                  <li key={item.href}>
                    <Link
                      href={item.href}
                      aria-current={active ? 'page' : undefined}
                      className={active ? 'font-semibold text-blue-700' : 'text-gray-600 hover:text-gray-900'}
                    >
                      {item.label}
                    </Link>
                  </li>
                )
              })}
            </ul>
          </div>
          <div className="flex items-center gap-4 text-sm">
            <NotificationBell />
            <Link href="/account" className="text-gray-700 hover:text-gray-900" data-testid="signed-in-as" title="My account">
              {user?.name || user?.email}
            </Link>
            <button type="button" onClick={signOut} className="text-blue-600 hover:text-blue-800 font-medium">
              Sign out
            </button>
          </div>
        </div>
      </nav>
      <main className="max-w-6xl mx-auto px-6 py-8">{children}</main>
    </div>
  )
}
