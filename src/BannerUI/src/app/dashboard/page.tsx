'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { AppShell } from '@/components/layout/AppShell'
import { useAuth } from '@/context/AuthContext'
import { Roles } from '@/lib/session'
import { ActiveBanner, bannerListService } from '@/api/workflowService'
import { formatWindow } from '@/lib/dates'

interface Card {
  href: string
  title: string
  text: string
  roles: string[]
}

const CARDS: Card[] = [
  { href: '/banners', title: 'Banners', text: 'Create banners, open the editor and send them for approval.', roles: [Roles.ShopOwner, Roles.SalesExecutive] },
  { href: '/approvals', title: 'Approvals', text: 'See what is waiting for approval, publish approved banners.', roles: [Roles.ShopOwner, Roles.SalesExecutive] },
  { href: '/team', title: 'Team', text: 'Add or remove sales executive logins and choose who approves banners.', roles: [Roles.ShopOwner] },
  { href: '/subscription', title: 'Subscription', text: 'Choose a plan, switch billing period, turn automatic renewal on or off.', roles: [Roles.ShopOwner] },
  { href: '/shops', title: 'Shops', text: 'Manage every shop on the platform.', roles: [Roles.Admin] },
  { href: '/admin/subscription-plans', title: 'Subscription plans', text: 'Edit plans and prices.', roles: [Roles.Admin] },
]

export default function DashboardPage() {
  return (
    <AppShell>
      <Welcome />
    </AppShell>
  )
}

function Welcome() {
  const { user, hasRole } = useAuth()
  const cards = CARDS.filter((card) => card.roles.some((role) => hasRole(role)))
  const [active, setActive] = useState<ActiveBanner | null>(null)
  const inShop = hasRole(Roles.ShopOwner) || hasRole(Roles.SalesExecutive)

  useEffect(() => {
    if (!inShop) return
    bannerListService.getActive().then(setActive).catch(() => setActive(null))
  }, [inShop])

  return (
    <div>
      <h1 className="text-2xl font-semibold text-gray-900">Welcome{user?.name ? `, ${user.name}` : ''}</h1>
      <p className="mt-1 text-gray-600">What would you like to do?</p>

      {active && (
        <p className="mt-4 rounded-xl border border-gray-200 bg-white px-4 py-3 text-sm" data-testid="now-showing">
          {active.banner ? (
            <>
              <span className="font-medium text-gray-900">Now showing: {active.banner.name}</span>
              <span className="text-gray-600"> · {formatWindow(active.banner.publishStartAt, active.banner.publishEndAt)}</span>
            </>
          ) : (
            <span className="text-gray-700">
              No banner is scheduled right now, so your shop shows its own default banner.
            </span>
          )}
        </p>
      )}

      <ul className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {cards.map((card) => (
          <li key={card.href}>
            <Link
              href={card.href}
              className="block h-full rounded-xl border border-gray-200 bg-white p-5 shadow-sm hover:border-blue-400 hover:shadow transition"
            >
              <h2 className="font-semibold text-gray-900">{card.title}</h2>
              <p className="mt-1 text-sm text-gray-600">{card.text}</p>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  )
}
