'use client'

import React, { useCallback, useEffect, useState } from 'react'
import Link from 'next/link'
import { useSearchParams } from 'next/navigation'
import { Button, Input } from '@/components/Common'
import { Pagination } from '@/components/admin/Pagination'
import { adminService } from '@/api/adminService'
import { getErrorMessage } from '@/api/client'
import { fromLocalInput } from '@/lib/dates'
import { AuditEntry, Page } from '@/types/admin'

const PAGE_SIZE = 50

/** Colour for an HTTP status: green success, amber client problem, red server problem. */
export function statusTone(code: number): string {
  if (code >= 500) return 'bg-red-100 text-red-800'
  if (code >= 400) return 'bg-amber-100 text-amber-800'
  return 'bg-green-100 text-green-800'
}

export function AuditPanel() {
  const params = useSearchParams()
  const userId = params?.get('userId') ?? ''
  const shopId = params?.get('shopId') ?? ''

  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [failuresOnly, setFailuresOnly] = useState(false)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<Page<AuditEntry> | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      setResult(
        await adminService.listAuditLogs({
          fromIso: fromLocalInput(from) ?? undefined,
          toIso: fromLocalInput(to) ?? undefined,
          userId,
          shopId,
          failuresOnly,
          page,
          pageSize: PAGE_SIZE,
        })
      )
      setError(null)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not load the activity log.'))
    }
  }, [from, to, userId, shopId, failuresOnly, page])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    setPage(1)
  }, [from, to, userId, shopId, failuresOnly])

  const [exporting, setExporting] = useState(false)

  const exportCsv = async () => {
    setExporting(true)
    try {
      const file = await adminService.exportAuditLogs({
        fromIso: fromLocalInput(from) ?? undefined,
        toIso: fromLocalInput(to) ?? undefined,
        userId,
        shopId,
        failuresOnly,
        page: 1,
        pageSize: PAGE_SIZE,
      })
      const url = URL.createObjectURL(file)
      const link = document.createElement('a')
      link.href = url
      link.download = `activity-log-${new Date().toISOString().slice(0, 10)}.csv`
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
      setError(null)
    } catch (err) {
      setError(getErrorMessage(err, 'The activity log could not be exported.'))
    } finally {
      setExporting(false)
    }
  }

  const filtered = !!userId || !!shopId

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Activity log</h1>
        <p className="mt-1 text-gray-600">
          Who called what, newest first. Sign-ins and every call by a signed-in user are recorded; what was sent is not.
          {filtered && (
            <>
              {' '}
              Filtered to one {userId ? 'user' : 'shop'}.{' '}
              <Link href="/admin/audit-log" className="text-blue-600 hover:text-blue-800">
                Show everything
              </Link>
            </>
          )}
        </p>
      </header>

      <div className="flex flex-wrap items-end gap-4">
        <div className="w-56">
          <Input id="audit-from" type="datetime-local" label="From" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="w-56">
          <Input id="audit-to" type="datetime-local" label="To" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        <label className="flex items-center gap-2 pb-2 text-sm text-gray-700">
          <input type="checkbox" checked={failuresOnly} onChange={(e) => setFailuresOnly(e.target.checked)} />
          Failures only (status 400 and up)
        </label>
        <Button variant="secondary" onClick={exportCsv} isLoading={exporting}>
          Export as CSV
        </Button>
      </div>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}
      {!result && !error && <p className="text-gray-500">Loading activity…</p>}

      {result && (
        <>
          <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50 text-left text-gray-600">
                <tr>
                  <th className="px-4 py-3 font-medium">When</th>
                  <th className="px-4 py-3 font-medium">User</th>
                  <th className="px-4 py-3 font-medium">Request</th>
                  <th className="px-4 py-3 font-medium">Result</th>
                  <th className="px-4 py-3 font-medium">From</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {result.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-gray-500">
                      Nothing recorded for these filters.
                    </td>
                  </tr>
                )}
                {result.items.map((entry) => (
                  <tr key={entry.id}>
                    <td className="whitespace-nowrap px-4 py-2 text-gray-700">{new Date(entry.occurredAt).toLocaleString()}</td>
                    <td className="px-4 py-2 text-gray-700">
                      {entry.userEmail ? (
                        entry.userId ? (
                          <Link href={`/admin/audit-log?userId=${encodeURIComponent(entry.userId)}`} className="text-blue-600 hover:text-blue-800">
                            {entry.userEmail}
                          </Link>
                        ) : (
                          entry.userEmail
                        )
                      ) : (
                        <span className="text-gray-400">not signed in</span>
                      )}
                    </td>
                    <td className="px-4 py-2 font-mono text-xs text-gray-800">
                      <span className="mr-2 font-semibold">{entry.method}</span>
                      {entry.path}
                    </td>
                    <td className="whitespace-nowrap px-4 py-2">
                      <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${statusTone(entry.statusCode)}`}>{entry.statusCode}</span>
                      <span className="ml-2 text-xs text-gray-500">{entry.durationMs} ms</span>
                    </td>
                    <td className="px-4 py-2 text-gray-600">{entry.ipAddress ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <Pagination page={result.page} pageSize={result.pageSize} total={result.total} onChange={setPage} />
        </>
      )}
    </div>
  )
}
