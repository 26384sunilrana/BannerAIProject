'use client'

import React, { useCallback, useEffect, useState } from 'react'
import Link from 'next/link'
import { Button, ConfirmDialog, Input, Select, Toast } from '@/components/Common'
import { Pagination } from '@/components/admin/Pagination'
import { useDebounce } from '@/hooks/useDebounce'
import { useToast } from '@/hooks/useToast'
import { adminService } from '@/api/adminService'
import { subscriptionService } from '@/api/subscriptionService'
import { getErrorMessage } from '@/api/client'
import { SubscriptionStatus, formatMoney, periodLabel } from '@/lib/pricing'
import { AdminSubscription, LifecycleReport, Page } from '@/types/admin'

const PAGE_SIZE = 25

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  ...Object.entries(SubscriptionStatus).map(([value, label]) => ({ value, label })),
]

/** Statuses where the shop is cut off or about to be, so the action reads "Reactivate". */
const NEEDS_REACTIVATION = [4, 5, 6, 7]

const TONES: Record<number, string> = {
  1: 'bg-blue-100 text-blue-800',
  2: 'bg-green-100 text-green-800',
  3: 'bg-amber-100 text-amber-800',
  4: 'bg-red-100 text-red-800',
  5: 'bg-red-100 text-red-800',
  6: 'bg-amber-100 text-amber-800',
  7: 'bg-red-100 text-red-800',
  8: 'bg-gray-100 text-gray-700',
}

export default function AdminSubscriptionsPage() {
  const toast = useToast()

  const [status, setStatus] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [result, setResult] = useState<Page<AdminSubscription> | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)
  const [toRenew, setToRenew] = useState<AdminSubscription | null>(null)
  const [running, setRunning] = useState(false)
  const [report, setReport] = useState<LifecycleReport | null>(null)

  const term = useDebounce(search, 300)

  const load = useCallback(async () => {
    try {
      setResult(
        await adminService.listSubscriptions({
          status: status ? Number(status) : undefined,
          search: term,
          page,
          pageSize: PAGE_SIZE,
        })
      )
      setError(null)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not load subscriptions.'))
    }
  }, [status, term, page])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    setPage(1)
  }, [status, term])

  const renew = async (subscription: AdminSubscription) => {
    setBusyId(subscription.id)
    try {
      await subscriptionService.renewNow(subscription.id)
      toast.success(`${subscription.shopName} was renewed.`)
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'Could not renew.'))
    } finally {
      setBusyId(null)
    }
  }

  const runJob = async () => {
    setRunning(true)
    try {
      setReport(await adminService.runLifecycle())
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'The job could not run.'))
    } finally {
      setRunning(false)
    }
  }

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">Subscriptions</h1>
          <p className="mt-1 text-gray-600">Every shop&apos;s plan, soonest renewal first. Reactivate shops that were switched off.</p>
        </div>
        <Button variant="secondary" isLoading={running} onClick={runJob}>
          Run renewal job now
        </Button>
      </header>

      {report && (
        <div role="status" data-testid="lifecycle-report" className="rounded-xl border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-900">
          Job finished: {report.autoRenewed} renewed, {report.renewalsFailed} payments failed, {report.enteredGrace} started grace,{' '}
          {report.expired} expired, {report.messagesSent} messages sent.
        </div>
      )}

      <div className="flex flex-wrap items-end gap-4">
        <div className="w-56">
          <Select id="status-filter" label="Status" options={STATUS_OPTIONS} value={status} onChange={(e) => setStatus(e.target.value)} />
        </div>
        <div className="w-full max-w-sm">
          <Input id="shop-search" label="Shop" placeholder="Shop name" value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
      </div>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}
      {!result && !error && <p className="text-gray-500">Loading subscriptions…</p>}

      {result && (
        <>
          <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
            <table className="min-w-full divide-y divide-gray-200 text-sm">
              <thead className="bg-gray-50 text-left text-gray-600">
                <tr>
                  <th className="px-4 py-3 font-medium">Shop</th>
                  <th className="px-4 py-3 font-medium">Plan</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Renews / ended</th>
                  <th className="px-4 py-3 font-medium text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {result.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-gray-500">
                      No subscriptions match.
                    </td>
                  </tr>
                )}
                {result.items.map((subscription) => {
                  const reactivate = NEEDS_REACTIVATION.includes(subscription.status)
                  const cancelled = subscription.status === 8
                  return (
                    <tr key={subscription.id} data-testid={`subscription-${subscription.shopName}`}>
                      <td className="px-4 py-3">
                        <p className="font-medium text-gray-900">{subscription.shopName}</p>
                        <Link href={`/admin/users?shopId=${subscription.shopId}`} className="text-blue-600 hover:text-blue-800">
                          Users
                        </Link>
                      </td>
                      <td className="px-4 py-3 text-gray-700">
                        {subscription.planName}
                        <br />
                        <span className="text-gray-500">
                          {periodLabel(subscription.billingPeriod)} · {formatMoney(subscription.currentPrice)}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${TONES[subscription.status] ?? TONES[8]}`}>
                          {SubscriptionStatus[subscription.status] ?? subscription.status}
                        </span>
                        {!subscription.autoRenew && !cancelled && <p className="mt-1 text-xs text-gray-500">No auto renewal</p>}
                      </td>
                      <td className="px-4 py-3 text-gray-700">
                        {new Date(subscription.renewalDate).toLocaleDateString()}
                        {subscription.graceEndsAt && (
                          <p className="text-xs text-gray-500">Logins off {new Date(subscription.graceEndsAt).toLocaleDateString()}</p>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right">
                        {!cancelled && (
                          <Button
                            size="sm"
                            variant={reactivate ? 'primary' : 'secondary'}
                            isLoading={busyId === subscription.id}
                            onClick={() => setToRenew(subscription)}
                          >
                            {reactivate ? 'Reactivate' : 'Renew early'}
                          </Button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <Pagination page={result.page} pageSize={result.pageSize} total={result.total} onChange={setPage} />
        </>
      )}

      <ConfirmDialog
        isOpen={toRenew !== null}
        title={toRenew && NEEDS_REACTIVATION.includes(toRenew.status) ? 'Reactivate this shop?' : 'Renew for another period?'}
        message={`${toRenew?.shopName ?? ''} gets another ${toRenew ? periodLabel(toRenew.billingPeriod).toLowerCase() : 'period'} from today, logins are switched back on, and ${toRenew ? formatMoney(toRenew.currentPrice) : 'the plan price'} is charged through the payment provider (none is connected yet).`}
        confirmText={toRenew && NEEDS_REACTIVATION.includes(toRenew.status) ? 'Reactivate' : 'Renew'}
        onCancel={() => setToRenew(null)}
        onConfirm={async () => {
          const target = toRenew
          setToRenew(null)
          if (target) await renew(target)
        }}
      />
    </div>
  )
}
