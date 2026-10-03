'use client'

import React, { useEffect, useState } from 'react'
import Link from 'next/link'
import { AppShell } from '@/components/layout/AppShell'
import { Input, Select } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { AdStatement, statementService } from '@/api/adService'
import { apiClient, getErrorMessage } from '@/api/client'
import { Roles } from '@/lib/session'

const KIND_LABEL: Record<string, string> = { Side: 'Side strip', Mega: 'Mega ad', Popup: 'Popup', Minor: 'Corner tile' }
const STATUS_LABEL: Record<string, string> = { Approved: 'Ran', Cancelled: 'Cancelled', Overridden: 'Stopped by the shop' }

const money = (value: number) => value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })

const currentMonth = () => {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
}

export default function StatementPage() {
  return (
    <AppShell roles={[Roles.Admin, Roles.ShopOwner]}>
      <Statement />
    </AppShell>
  )
}

function Statement() {
  const { hasRole } = useAuth()
  const isAdmin = hasRole(Roles.Admin)
  const [month, setMonth] = useState(currentMonth())
  const [shops, setShops] = useState<{ id: string; name: string }[]>([])
  const [shopId, setShopId] = useState('')
  const [statement, setStatement] = useState<AdStatement | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!isAdmin) return
    apiClient
      .get<{ items: { id: string; name: string }[] }>('/shops?pageNumber=1&pageSize=100')
      .then((page) => setShops(page.items))
      .catch(() => undefined)
  }, [isAdmin])

  useEffect(() => {
    if (!/^\d{4}-\d{2}$/.test(month)) return
    let current = true
    setStatement(null)
    statementService
      .get(month, isAdmin && shopId ? shopId : undefined)
      .then((s) => {
        if (current) {
          setStatement(s)
          setError(null)
        }
      })
      .catch((err) => current && setError(getErrorMessage(err, 'The statement could not be loaded.')))
    return () => {
      current = false
    }
  }, [month, shopId, isAdmin])

  // the administrator can read the statement by city
  const cities = Object.values(
    (statement?.shops ?? []).reduce<Record<string, { name: string; shops: number; payout: number }>>((all, shop) => {
      const name = shop.cityName || 'No city'
      all[name] = { name, shops: (all[name]?.shops ?? 0) + 1, payout: (all[name]?.payout ?? 0) + shop.payout }
      return all
    }, {})
  ).sort((x, y) => x.name.localeCompare(y.name))

  return (
    <div className="space-y-6">
      <header>
        <Link href="/ads" className="text-sm text-blue-700 hover:underline">
          ← Ads
        </Link>
        <h1 className="mt-1 text-2xl font-semibold text-gray-900">Monthly statement of ads</h1>
        <p className="mt-1 max-w-2xl text-gray-600">
          How many hours each ad was on the screen in the month, on the shop clock. Ads booked by the administrator are paid to the shop by the hours they
          ran. Ads the shop booked itself are listed for the hours only: their price is the shop&apos;s own business.
        </p>
      </header>

      <div className="flex flex-wrap items-end gap-4">
        <div className="w-48">
          <Input id="statement-month" type="month" label="Month" value={month} onChange={(e) => setMonth(e.target.value)} />
        </div>
        {isAdmin && (
          <div className="w-64">
            <Select id="statement-shop" label="Shop" options={shops.map((s) => ({ value: s.id, label: s.name }))} placeholder="All shops" value={shopId} onChange={(e) => setShopId(e.target.value)} />
          </div>
        )}
      </div>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}
      {!statement && !error && <p className="text-gray-500">Loading the statement…</p>}
      {statement && statement.shops.length === 0 && (
        <p className="rounded-xl border border-dashed border-gray-300 p-8 text-center text-gray-500" data-testid="statement-empty">
          No ad ran in this month.
        </p>
      )}

      {isAdmin && cities.length > 1 && (
        <ul className="flex flex-wrap gap-3 text-sm text-gray-700" aria-label="By city" data-testid="statement-cities">
          {cities.map((c) => (
            <li key={c.name} className="rounded-full bg-gray-100 px-3 py-1">
              {c.name}: {c.shops} shop{c.shops === 1 ? '' : 's'}, paid {money(c.payout)}
            </li>
          ))}
        </ul>
      )}

      {statement?.shops.map((shop) => (
        <section key={shop.shopId} data-testid={`statement-${shop.shopName}`} className="rounded-xl border border-gray-200 bg-white p-4">
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <h2 className="text-lg font-semibold text-gray-900">
              {shop.shopName}
              {shop.cityName && <span className="ml-2 text-sm font-normal text-gray-500">{shop.cityName}</span>}
            </h2>
            <p className="text-sm text-gray-600">
              Administrator ads {shop.adminAdHours} h · own ads {shop.ownAdHours} h · <span className="font-semibold text-gray-900">paid {money(shop.payout)}</span>
            </p>
          </div>
          <div className="mt-3 overflow-x-auto">
            <table className="w-full min-w-[40rem] text-left text-sm">
              <thead className="text-xs uppercase text-gray-500">
                <tr>
                  <th className="py-1 pr-3">Ad</th>
                  <th className="py-1 pr-3">Booked by</th>
                  <th className="py-1 pr-3 text-right">Hours</th>
                  <th className="py-1 pr-3 text-right">Per hour</th>
                  <th className="py-1 pr-3 text-right">Shop share</th>
                  <th className="py-1 text-right">Paid to the shop</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {shop.lines.map((line) => (
                  <tr key={line.adId} data-testid={`line-${line.headline}`}>
                    <td className="py-2 pr-3">
                      <span className="font-medium text-gray-900">{line.headline}</span>
                      <span className="block text-xs text-gray-500">
                        {line.advertiserName} · {KIND_LABEL[line.kind]} · {STATUS_LABEL[line.status] ?? line.status}
                      </span>
                    </td>
                    <td className="py-2 pr-3">{line.source === 'Admin' ? 'Administrator' : line.source === 'ShopOwner' ? 'Owner' : 'Sales executive'}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{line.hours}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{line.pricePerHour != null ? money(line.pricePerHour) : '—'}</td>
                    <td className="py-2 pr-3 text-right tabular-nums">{line.shopSharePercent != null ? `${line.shopSharePercent}%` : '—'}</td>
                    <td className="py-2 text-right tabular-nums">{line.payout != null ? money(line.payout) : 'not tracked'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ))}

      {statement && statement.shops.length > 0 && (
        <p className="text-right text-lg font-semibold text-gray-900" data-testid="statement-total">
          Total paid to shops: {money(statement.totalPayout)}
        </p>
      )}
    </div>
  )
}
