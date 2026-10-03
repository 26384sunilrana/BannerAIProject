'use client'

import React, { useEffect, useState } from 'react'
import Link from 'next/link'
import { AppShell } from '@/components/layout/AppShell'
import { Input } from '@/components/Common'
import { reportService, UserReportRow } from '@/api/adService'
import { getErrorMessage } from '@/api/client'
import { Roles } from '@/lib/session'

const currentMonth = () => {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
}

export default function AdReportsPage() {
  return (
    <AppShell roles={[Roles.Admin]}>
      <Report />
    </AppShell>
  )
}

function Report() {
  const [month, setMonth] = useState(currentMonth())
  const [rows, setRows] = useState<UserReportRow[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!/^\d{4}-\d{2}$/.test(month)) return
    let current = true
    setRows(null)
    reportService
      .users(month)
      .then((r) => {
        if (current) {
          setRows(r.rows)
          setError(null)
        }
      })
      .catch((err) => current && setError(getErrorMessage(err, 'The report could not be loaded.')))
    return () => {
      current = false
    }
  }, [month])

  const total = (key: keyof UserReportRow) => (rows ?? []).reduce((sum, r) => sum + (r[key] as number), 0)

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Ad report: who booked what</h1>
        <p className="mt-1 max-w-2xl text-gray-600">
          For the ads booked in the month: how many each person booked, how many went live, were sent back or cancelled, are still waiting, and how many
          were held for a compliance review because they mention health wording. The money side is in the{' '}
          <Link href="/ads/statement" className="text-blue-700 hover:underline">
            monthly statement
          </Link>
          .
        </p>
      </header>

      <div className="w-48">
        <Input id="report-month" type="month" label="Month" value={month} onChange={(e) => setMonth(e.target.value)} />
      </div>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}
      {!rows && !error && <p className="text-gray-500">Loading the report…</p>}
      {rows?.length === 0 && (
        <p className="rounded-xl border border-dashed border-gray-300 p-8 text-center text-gray-500" data-testid="report-empty">
          No ad was booked in this month.
        </p>
      )}

      {rows && rows.length > 0 && (
        <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
          <table className="w-full min-w-[44rem] text-left text-sm">
            <thead className="bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-4 py-2">Person</th>
                <th className="px-4 py-2">Shop</th>
                <th className="px-4 py-2 text-right">Booked</th>
                <th className="px-4 py-2 text-right">Went live</th>
                <th className="px-4 py-2 text-right">Sent back</th>
                <th className="px-4 py-2 text-right">Cancelled</th>
                <th className="px-4 py-2 text-right">Waiting</th>
                <th className="px-4 py-2 text-right">Held for review</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {rows.map((row) => (
                <tr key={row.userId} data-testid={`report-${row.name}`}>
                  <td className="px-4 py-2">
                    <span className="font-medium text-gray-900">{row.name}</span>
                    <span className="block text-xs text-gray-500">{row.role}</span>
                  </td>
                  <td className="px-4 py-2">{row.shopName || '—'}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.booked}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.approved}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.sentBack}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.cancelled}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.waiting}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{row.flagged}</td>
                </tr>
              ))}
            </tbody>
            <tfoot className="bg-gray-50 font-semibold" data-testid="report-total">
              <tr>
                <td className="px-4 py-2" colSpan={2}>
                  Everyone
                </td>
                <td className="px-4 py-2 text-right tabular-nums">{total('booked')}</td>
                <td className="px-4 py-2 text-right tabular-nums">{total('approved')}</td>
                <td className="px-4 py-2 text-right tabular-nums">{total('sentBack')}</td>
                <td className="px-4 py-2 text-right tabular-nums">{total('cancelled')}</td>
                <td className="px-4 py-2 text-right tabular-nums">{total('waiting')}</td>
                <td className="px-4 py-2 text-right tabular-nums">{total('flagged')}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </div>
  )
}
