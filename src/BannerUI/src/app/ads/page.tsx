'use client'

import React, { useCallback, useEffect, useState } from 'react'
import { AppShell } from '@/components/layout/AppShell'
import Link from 'next/link'
import { Button, Select, Toast } from '@/components/Common'
import { AdForm } from '@/components/ads/AdForm'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { useShopTimeZone } from '@/hooks/useShopTimeZone'
import { adService, AdHistoryEntry, AdInput, ShopAd } from '@/api/adService'
import { apiClient, getErrorMessage } from '@/api/client'
import { describeDaily, formatInZone, BROWSER_ZONE } from '@/lib/timeZones'
import { Roles } from '@/lib/session'

const KIND_LABEL: Record<string, string> = { Side: 'Side strip', Mega: 'Mega ad', Popup: 'Popup', Minor: 'Corner tile' }
const STATUS_LABEL: Record<string, string> = {
  Draft: 'Draft',
  PendingApproval: 'Waiting for approval',
  Approved: 'Approved',
  Rejected: 'Sent back',
  Cancelled: 'Cancelled',
  Overridden: 'Stopped by the shop',
}
const STATUS_STYLE: Record<string, string> = {
  Draft: 'bg-gray-100 text-gray-700',
  PendingApproval: 'bg-amber-100 text-amber-800',
  Approved: 'bg-green-100 text-green-800',
  Rejected: 'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-500',
  Overridden: 'bg-purple-100 text-purple-800',
}
const SOURCE_LABEL: Record<string, string> = { Admin: 'Booked by the administrator', ShopOwner: 'Booked by the owner', SalesExecutive: 'Booked by a sales executive' }

export default function AdsPage() {
  return (
    <AppShell roles={[Roles.Admin, Roles.ShopOwner, Roles.SalesExecutive]}>
      <Ads />
    </AppShell>
  )
}

interface ShopChoice {
  id: string
  name: string
}

function Ads() {
  const { user, hasRole } = useAuth()
  const isAdmin = hasRole(Roles.Admin)
  const toast = useToast()
  const ownZone = useShopTimeZone()

  const [shops, setShops] = useState<ShopChoice[]>([])
  const [shopId, setShopId] = useState<string>(isAdmin ? '' : user?.shopId ?? '')
  const [shopZone, setShopZone] = useState<string>(BROWSER_ZONE)
  const [ads, setAds] = useState<ShopAd[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [editing, setEditing] = useState<ShopAd | 'new' | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [rejecting, setRejecting] = useState<string | null>(null)
  const [overriding, setOverriding] = useState<string | null>(null)
  const [reason, setReason] = useState('')
  const [history, setHistory] = useState<{ id: string; entries: AdHistoryEntry[] } | null>(null)

  const zone = isAdmin ? shopZone : ownZone.timeZoneId

  useEffect(() => {
    if (!isAdmin) return
    apiClient
      .get<{ items: ShopChoice[] }>('/shops?pageNumber=1&pageSize=100')
      .then((page) => setShops(page.items))
      .catch(() => setLoadError('The list of shops could not be loaded.'))
  }, [isAdmin])

  useEffect(() => {
    if (!isAdmin || !shopId) return
    apiClient
      .get<{ timeZoneId: string }>(`/shops/${shopId}/time-zone`)
      .then((z) => setShopZone(z.timeZoneId))
      .catch(() => setShopZone(BROWSER_ZONE))
  }, [isAdmin, shopId])

  const load = useCallback(async () => {
    if (isAdmin && !shopId) {
      setAds([])
      return
    }
    try {
      setAds(await adService.list(isAdmin ? shopId : undefined))
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'The ads could not be loaded.'))
    }
  }, [isAdmin, shopId])

  useEffect(() => {
    load()
  }, [load])

  const save = async (input: AdInput) => {
    setBusy(true)
    setFormError(null)
    try {
      if (editing && editing !== 'new') await adService.update(editing.id, input)
      else await adService.create(isAdmin ? { ...input, shopId } : input)
      setEditing(null)
      toast.success('The ad was saved as a draft. Send it in when it is ready.')
      await load()
    } catch (err) {
      setFormError(getErrorMessage(err, 'The ad could not be saved.'))
    } finally {
      setBusy(false)
    }
  }

  const act = async (ad: ShopAd, action: 'submit' | 'approve' | 'reject' | 'cancel' | 'override', body: { reason?: string } = {}) => {
    setBusy(true)
    try {
      const result = await adService.act(ad.id, action, body)
      const text: Record<string, string> = {
        submit: result.status === 'Approved' ? 'The ad is booked.' : 'The ad was sent to the owner for approval.',
        approve: 'The ad was approved.',
        reject: 'The ad was sent back.',
        cancel: 'The ad was cancelled.',
        override: 'The ad was stopped. The administrator has been told.',
      }
      toast.success(text[action])
      setRejecting(null)
      setOverriding(null)
      setReason('')
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'That did not work.'))
    } finally {
      setBusy(false)
    }
  }

  const showHistory = async (ad: ShopAd) => {
    if (history?.id === ad.id) return setHistory(null)
    try {
      setHistory({ id: ad.id, entries: await adService.history(ad.id) })
    } catch (err) {
      toast.error(getErrorMessage(err, 'The history could not be loaded.'))
    }
  }

  const canBook = !isAdmin || !!shopId

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">Ads</h1>
          <p className="mt-1 max-w-2xl text-gray-600">
            {isAdmin
              ? 'Book ads on a shop screen and see what the shop has booked itself, so you can plan around it.'
              : 'Book an ad on your shop screen for an advertiser who has come to you. It sits beside your banner, over it, or in a corner, for the dates you choose.'}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          {(isAdmin || hasRole(Roles.ShopOwner)) && (
            <Link href="/ads/statement" className="text-sm font-medium text-blue-700 hover:underline">
              Monthly statement
            </Link>
          )}
          {canBook && !editing && (
            <Button
              onClick={() => {
                setFormError(null)
                setEditing('new')
              }}
            >
              Book an ad
            </Button>
          )}
        </div>
      </header>

      {isAdmin && (
        <div className="max-w-sm">
          <Select
            id="ads-shop"
            label="Shop"
            options={shops.map((s) => ({ value: s.id, label: s.name }))}
            placeholder="Choose a shop"
            value={shopId}
            onChange={(e) => {
              setShopId(e.target.value)
              setEditing(null)
              setHistory(null)
              setAds(null)
            }}
          />
        </div>
      )}

      {editing && (
        <AdForm
          key={editing === 'new' ? 'new' : editing.id}
          ad={editing === 'new' ? undefined : editing}
          zone={zone}
          allowPicture={!isAdmin}
          busy={busy}
          error={formError}
          onSubmit={save}
          onCancel={() => setEditing(null)}
        />
      )}

      {loadError && (
        <p role="alert" className="text-red-700">
          {loadError}
        </p>
      )}
      {isAdmin && !shopId && <p className="text-gray-500">Choose a shop to see its ads.</p>}
      {ads === null && !loadError && (!isAdmin || shopId) && <p className="text-gray-500">Loading the ads…</p>}
      {ads?.length === 0 && (!isAdmin || shopId) && !loadError && (
        <p className="rounded-xl border border-dashed border-gray-300 p-8 text-center text-gray-500">No ads yet. Use “Book an ad”.</p>
      )}

      <ul className="space-y-3">
        {ads?.map((ad) => (
          <li key={ad.id} data-testid={`ad-row-${ad.headline}`} className="rounded-xl border border-gray-200 bg-white p-4">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="font-semibold text-gray-900">{ad.headline}</p>
                <p className="text-sm text-gray-600">
                  {ad.advertiserName} · {KIND_LABEL[ad.kind]}
                  {ad.kind === 'Side' || ad.kind === 'Mega' ? `, ${ad.spacePercent}% on the ${ad.placement.toLowerCase()}` : ad.kind === 'Minor' ? `, ${ad.placement.replace(/([A-Z])/g, ' $1').trim().toLowerCase()}` : `, ${ad.popupSeconds} s every ${ad.popupEveryMinutes} min`}
                </p>
                <p className="text-sm text-gray-600">
                  {formatInZone(ad.startAt, zone)} to {formatInZone(ad.endAt, zone)}
                  {ad.dailyStartMinutes != null && <span> · {describeDaily(ad.dailyStartMinutes, ad.dailyEndMinutes, ad.activeDays)}</span>}
                </p>
                <p className="text-xs text-gray-500">{SOURCE_LABEL[ad.source] ?? ad.source}</p>
                {ad.pricePerHour != null && (
                  <p className="text-xs text-gray-500" data-testid="ad-price">
                    {ad.pricePerHour.toFixed(2)} an hour, the shop is paid {ad.shopSharePercent ?? 100}% of it
                  </p>
                )}
                {ad.stoppedAt && ad.status !== 'Draft' && <p className="text-xs text-gray-500">Stopped {formatInZone(ad.stoppedAt, zone)}</p>}
                {ad.decisionNote && ad.status !== 'Approved' && <p className="mt-1 text-sm text-gray-700">“{ad.decisionNote}”</p>}
              </div>
              <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_STYLE[ad.status]}`}>{STATUS_LABEL[ad.status]}</span>
            </div>

            <div className="mt-3 flex flex-wrap gap-2">
              {ad.can.includes('submit') && (
                <Button size="sm" disabled={busy} onClick={() => act(ad, 'submit')}>
                  Send in
                </Button>
              )}
              {ad.can.includes('edit') && (
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() => {
                    setFormError(null)
                    setEditing(ad)
                  }}
                >
                  Edit
                </Button>
              )}
              {ad.can.includes('approve') && (
                <Button size="sm" disabled={busy} onClick={() => act(ad, 'approve')}>
                  Approve
                </Button>
              )}
              {ad.can.includes('reject') && (
                <Button size="sm" variant="secondary" disabled={busy} onClick={() => setRejecting(rejecting === ad.id ? null : ad.id)}>
                  Send back
                </Button>
              )}
              {ad.can.includes('override') && (
                <Button size="sm" variant="secondary" disabled={busy} onClick={() => setOverriding(overriding === ad.id ? null : ad.id)}>
                  Override this ad
                </Button>
              )}
              {ad.can.includes('cancel') && (
                <Button size="sm" variant="ghost" disabled={busy} onClick={() => act(ad, 'cancel')}>
                  Cancel ad
                </Button>
              )}
              <Button size="sm" variant="ghost" onClick={() => showHistory(ad)}>
                {history?.id === ad.id ? 'Hide history' : 'History'}
              </Button>
            </div>

            {overriding === ad.id && (
              <div className="mt-3 space-y-2 rounded-lg bg-amber-50 p-3">
                <p className="text-sm text-amber-900">
                  The administrator booked this ad. If you have a better offer for this time you can stop it now: the space is free at once, the shop is
                  paid for the hours it ran, and the administrator is told.
                </p>
                <label htmlFor={`override-${ad.id}`} className="text-sm font-medium text-gray-700">
                  Reason (optional)
                </label>
                <textarea
                  id={`override-${ad.id}`}
                  rows={2}
                  maxLength={500}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  className="w-full rounded-lg border-2 border-gray-300 px-3 py-2 text-gray-900 focus-visible:outline-none focus:border-blue-500"
                />
                <Button size="sm" disabled={busy} onClick={() => act(ad, 'override', { reason })}>
                  Yes, stop this ad
                </Button>
              </div>
            )}

            {rejecting === ad.id && (
              <div className="mt-3 space-y-2 rounded-lg bg-gray-50 p-3">
                <label htmlFor={`reason-${ad.id}`} className="text-sm font-medium text-gray-700">
                  Why is it sent back?
                </label>
                <textarea
                  id={`reason-${ad.id}`}
                  rows={2}
                  maxLength={500}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  className="w-full rounded-lg border-2 border-gray-300 px-3 py-2 text-gray-900 focus-visible:outline-none focus:border-blue-500"
                />
                <Button size="sm" disabled={busy || !reason.trim()} onClick={() => act(ad, 'reject', { reason })}>
                  Send it back
                </Button>
              </div>
            )}

            {history?.id === ad.id && (
              <ol className="mt-3 space-y-1 border-t border-gray-100 pt-3 text-sm text-gray-700" aria-label="History">
                {history.entries.map((entry, i) => (
                  <li key={i}>
                    <span className="font-medium">{entry.action}</span> by {entry.userName}, {formatInZone(entry.at, zone)}
                    {entry.note && <span className="text-gray-500"> — {entry.note}</span>}
                  </li>
                ))}
              </ol>
            )}
          </li>
        ))}
      </ul>
    </div>
  )
}
