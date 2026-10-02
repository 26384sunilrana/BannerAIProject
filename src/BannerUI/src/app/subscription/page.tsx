'use client'

import React, { useCallback, useEffect, useState } from 'react'
import { AppShell } from '@/components/layout/AppShell'
import { Button, Toast } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { PlanSummary, ShopSubscription, subscriptionService } from '@/api/subscriptionService'
import { getErrorMessage } from '@/api/client'
import { subscriptionNotice } from '@/lib/subscriptionNotice'
import { SubscriptionNoticeCard } from '@/components/SubscriptionNoticeCard'
import { Roles } from '@/lib/session'
import {
  BillingPeriod,
  PERIOD_OPTIONS,
  SubscriptionStatus,
  SUBSCRIBED_STATUSES,
  formatMoney,
  periodLabel,
  priceFor,
} from '@/lib/pricing'

export default function SubscriptionPage() {
  return (
    <AppShell roles={[Roles.ShopOwner]}>
      <Subscription />
    </AppShell>
  )
}

function Subscription() {
  const { user } = useAuth()
  const shopId = user?.shopId ?? null
  const toast = useToast()

  const [plans, setPlans] = useState<PlanSummary[] | null>(null)
  const [current, setCurrent] = useState<ShopSubscription | null | undefined>(undefined)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [planId, setPlanId] = useState<string>('')
  const [period, setPeriod] = useState<number>(BillingPeriod.Yearly)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    if (!shopId) return
    try {
      const [planList, subscription] = await Promise.all([subscriptionService.getPlans(), subscriptionService.getCurrent(shopId)])
      const active = planList.filter((p) => p.isActive)
      setPlans(active)
      setCurrent(subscription)
      setPlanId((prev) => prev || subscription?.planId || active[0]?.id || '')
      if (subscription) setPeriod(subscription.billingPeriod)
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'Could not load subscription details.'))
    }
  }, [shopId])

  useEffect(() => {
    load()
  }, [load])

  if (!shopId) return <p className="text-gray-600">Your account is not linked to a shop.</p>
  if (loadError) {
    return (
      <p role="alert" className="text-red-700">
        {loadError}
      </p>
    )
  }
  if (!plans || current === undefined) return <p className="text-gray-500">Loading subscription…</p>

  const selected = plans.find((p) => p.id === planId)
  const subscribed = current !== null && SUBSCRIBED_STATUSES.includes(current.status)

  const act = async (work: () => Promise<unknown>, done: string) => {
    setBusy(true)
    try {
      await work()
      toast.success(done)
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'That did not work.'))
    } finally {
      setBusy(false)
    }
  }

  const currentPlan = current ? plans.find((p) => p.id === current.planId) : undefined
  const pendingPlan = current?.pendingPlanId ? plans.find((p) => p.id === current.pendingPlanId) : undefined

  return (
    <div className="space-y-8">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Subscription</h1>
        <p className="mt-1 text-gray-600">
          Every period costs the same per month, so you can switch plan or period at any time. A plan change takes effect from the next day.
        </p>
      </header>

      {(() => {
        const notice = subscriptionNotice(current ?? null)
        return notice && current ? (
          <SubscriptionNoticeCard
            notice={notice}
            renewing={busy}
            onRenew={() => act(() => subscriptionService.renewNow(current.id), 'Your plan was renewed.')}
          />
        ) : null
      })()}

      {current && (
        <section aria-labelledby="current-heading" className="rounded-xl border border-gray-200 bg-white p-5">
          <h2 id="current-heading" className="text-lg font-semibold text-gray-900">
            Your plan
          </h2>
          <dl className="mt-3 grid gap-x-8 gap-y-2 sm:grid-cols-2 text-sm">
            <div>
              <dt className="text-gray-500">Plan</dt>
              <dd className="font-medium text-gray-900">{current.planName}</dd>
            </div>
            <div>
              <dt className="text-gray-500">Status</dt>
              <dd className="font-medium text-gray-900">{SubscriptionStatus[current.status] ?? current.status}</dd>
            </div>
            <div>
              <dt className="text-gray-500">Billing</dt>
              <dd className="font-medium text-gray-900">
                {periodLabel(current.billingPeriod)} · {formatMoney(current.currentPrice)}
              </dd>
            </div>
            <div>
              <dt className="text-gray-500">Next renewal</dt>
              <dd className="font-medium text-gray-900">{new Date(current.renewalDate).toLocaleDateString()}</dd>
            </div>
          </dl>

          {pendingPlan && current.pendingPlanEffectiveAt && (
            <p className="mt-3 rounded-lg bg-blue-50 px-3 py-2 text-sm text-blue-800">
              Changing to {pendingPlan.name} on {new Date(current.pendingPlanEffectiveAt).toLocaleDateString()}.
            </p>
          )}

          {current.status !== 8 && current.status !== 7 && current.status !== 5 && !subscriptionNotice(current)?.canRenew && (
            <div className="mt-4">
              <Button size="sm" variant="secondary" isLoading={busy} onClick={() => act(() => subscriptionService.renewNow(current.id), 'Your plan was renewed for another period.')}>
                Renew early
              </Button>
            </div>
          )}

          <label className="mt-4 flex items-center gap-3">
            <input
              type="checkbox"
              checked={current.autoRenew}
              disabled={busy}
              onChange={(e) =>
                act(() => subscriptionService.setAutoRenew(current.id, e.target.checked), e.target.checked ? 'Auto renewal is on.' : 'Auto renewal is off. We will remind you before it renews.')
              }
            />
            <span className="text-gray-900">Renew automatically</span>
          </label>
        </section>
      )}

      <section aria-labelledby="choose-heading">
        <h2 id="choose-heading" className="text-lg font-semibold text-gray-900">
          {subscribed ? 'Change plan or billing period' : 'Choose a plan'}
        </h2>

        <fieldset className="mt-3">
          <legend className="sr-only">Billing period</legend>
          <div className="flex flex-wrap gap-2">
            {PERIOD_OPTIONS.map((option) => (
              <label
                key={option.value}
                className={`cursor-pointer rounded-lg border px-3 py-1.5 text-sm ${period === option.value ? 'border-blue-600 bg-blue-50 text-blue-800' : 'border-gray-300 bg-white text-gray-700'}`}
              >
                <input type="radio" name="period" className="sr-only" checked={period === option.value} onChange={() => setPeriod(option.value)} />
                {option.label}
              </label>
            ))}
          </div>
        </fieldset>

        <ul className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4" role="radiogroup" aria-label="Plans">
          {plans.map((plan) => {
            const isSelected = plan.id === planId
            return (
              <li key={plan.id}>
                <button
                  type="button"
                  role="radio"
                  aria-checked={isSelected}
                  onClick={() => setPlanId(plan.id)}
                  className={`h-full w-full rounded-xl border-2 bg-white p-4 text-left transition ${isSelected ? 'border-blue-600 shadow' : 'border-gray-200 hover:border-gray-400'}`}
                >
                  <span className="block font-semibold text-gray-900">{plan.name}</span>
                  <span className="mt-1 block text-sm text-gray-600">{plan.description}</span>
                  <span className="mt-3 block text-lg font-semibold text-gray-900">{formatMoney(priceFor(plan, period))}</span>
                  <span className="block text-xs text-gray-500">per {periodLabel(period).toLowerCase().replace(/ \(.*\)/, '')}</span>
                </button>
              </li>
            )
          })}
        </ul>

        <div className="mt-5 flex flex-wrap items-center gap-3">
          {!subscribed && (
            <>
              <Button
                disabled={!selected}
                isLoading={busy}
                onClick={() => selected && act(() => subscriptionService.subscribe(shopId, selected.id, period), `Subscribed to ${selected.name}.`)}
              >
                Subscribe{selected ? ` to ${selected.name}` : ''}
              </Button>
              <p className="text-sm text-gray-500">Online payment is not connected yet; subscribing activates the plan straight away.</p>
            </>
          )}

          {subscribed && current && selected && selected.id !== current.planId && (
            <Button
              isLoading={busy}
              onClick={() =>
                act(
                  () => subscriptionService.changePlan(current.id, selected.id, priceFor(selected, current.billingPeriod) > current.currentPrice),
                  `Your plan changes to ${selected.name} from tomorrow.`
                )
              }
            >
              Change to {selected.name}
            </Button>
          )}

          {subscribed && current && period !== current.billingPeriod && (
            <Button
              variant="secondary"
              isLoading={busy}
              onClick={() => act(() => subscriptionService.changeBillingPeriod(current.id, period), `Billing changed to ${periodLabel(period)}.`)}
            >
              Switch to {periodLabel(period)}
            </Button>
          )}

          {subscribed && currentPlan && selected?.id === current?.planId && period === current?.billingPeriod && (
            <p className="text-sm text-gray-500">This is your current plan.</p>
          )}
        </div>
      </section>
    </div>
  )
}
