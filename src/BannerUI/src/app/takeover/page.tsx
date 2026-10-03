'use client'

import React, { FormEvent, useCallback, useEffect, useState } from 'react'
import { AppShell } from '@/components/layout/AppShell'
import { Button, Input, Toast } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { Takeover, takeoverService, TakeoverAssociates } from '@/api/takeoverService'
import { apiClient, getErrorMessage } from '@/api/client'
import { Roles } from '@/lib/session'

const STATUS_LABEL: Record<string, string> = { Requested: 'Waiting for an answer', Declined: 'Declined', Cancelled: 'Cancelled', Completed: 'Done' }
const STATUS_STYLE: Record<string, string> = {
  Requested: 'bg-amber-100 text-amber-800',
  Declined: 'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-600',
  Completed: 'bg-green-100 text-green-800',
}

export default function TakeoverPage() {
  return (
    <AppShell roles={[Roles.Admin, Roles.ShopOwner]}>
      <Takeovers />
    </AppShell>
  )
}

function Takeovers() {
  const { user, hasRole } = useAuth()
  const isAdmin = hasRole(Roles.Admin)
  const toast = useToast()

  const [items, setItems] = useState<Takeover[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      setItems(await takeoverService.list())
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'The requests could not be loaded.'))
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const incoming = (items ?? []).filter((t) => t.direction === 'Incoming')
  const outgoing = (items ?? []).filter((t) => t.direction === 'Outgoing')
  const all = (items ?? []).filter((t) => t.direction === 'Admin')

  return (
    <div className="space-y-8">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Shop takeover</h1>
        <p className="mt-1 max-w-3xl text-gray-600">
          Two shops can have the same name when they are at different addresses. One name at one address is one shop, so when a shop changes hands the new
          owner asks here and the owner of the shop agrees. Then either only the owner changes (the associates stay), or the old shop closes and the new
          owner&apos;s shop takes its place with new associates.
        </p>
      </header>

      {loadError && (
        <p role="alert" className="text-red-700">
          {loadError}
        </p>
      )}
      {!items && !loadError && <p className="text-gray-500">Loading…</p>}

      {isAdmin && items && (
        <section aria-labelledby="all-heading" className="space-y-3">
          <h2 id="all-heading" className="text-lg font-semibold text-gray-900">
            All requests
          </h2>
          {all.length === 0 && <p className="text-gray-500">No takeover has been asked for.</p>}
          {all.map((t) => (
            <TakeoverCard key={t.id} t={t} admin onChanged={load} toast={toast} />
          ))}
        </section>
      )}

      {!isAdmin && items && (
        <>
          <section aria-labelledby="incoming-heading" className="space-y-3">
            <h2 id="incoming-heading" className="text-lg font-semibold text-gray-900">
              Someone asks to take over your shop
            </h2>
            {incoming.length === 0 && <p className="text-gray-500">Nobody has asked.</p>}
            {incoming.map((t) => (
              <TakeoverCard key={t.id} t={t} onChanged={load} toast={toast} />
            ))}
          </section>

          <section aria-labelledby="outgoing-heading" className="space-y-3">
            <h2 id="outgoing-heading" className="text-lg font-semibold text-gray-900">
              Your requests
            </h2>
            {outgoing.length === 0 && <p className="text-gray-500">You have not asked to take a shop over.</p>}
            {outgoing.map((t) => (
              <TakeoverCard key={t.id} t={t} onChanged={load} toast={toast} />
            ))}
          </section>

          <AskForm shopId={user?.shopId ?? null} onAsked={load} toast={toast} />
        </>
      )}
    </div>
  )
}

type ToastApi = ReturnType<typeof useToast>

function AskForm({ shopId, onAsked, toast }: { shopId: string | null; onAsked: () => void; toast: ToastApi }) {
  const [name, setName] = useState('')
  const [address, setAddress] = useState('')
  const [postal, setPostal] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  // start from what this shop has already been given: a new owner usually asks for the shop they have just been told to run
  useEffect(() => {
    if (!shopId) return
    apiClient
      .get<{ name: string; address?: string | null; postalCode?: string | null }>(`/shops/${shopId}`)
      .then((shop) => {
        setName(shop.name ?? '')
        setAddress(shop.address ?? '')
        setPostal(shop.postalCode ?? '')
      })
      .catch(() => undefined)
  }, [shopId])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!name.trim()) return setError('Give the name of the shop.')
    if (!address.trim()) return setError('Give the street address of the shop.')
    setBusy(true)
    setError(null)
    try {
      await takeoverService.ask(name.trim(), address.trim(), postal.trim())
      toast.success('The request was sent. The owner of the shop has been told.')
      onAsked()
    } catch (err) {
      setError(getErrorMessage(err, 'The request could not be sent.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section aria-labelledby="ask-heading">
      <h2 id="ask-heading" className="text-lg font-semibold text-gray-900">
        Ask to take a shop over
      </h2>
      <p className="mt-1 max-w-3xl text-sm text-gray-600">
        Type the name and address of the shop exactly as it is known. The owner of that shop is asked to agree; nothing changes until they do.
      </p>
      <form onSubmit={submit} noValidate className="mt-3 max-w-xl space-y-3 rounded-xl border border-gray-200 bg-white p-5">
        {error && (
          <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
            {error}
          </div>
        )}
        <Input id="takeover-name" label="Shop name" value={name} onChange={(e) => setName(e.target.value)} />
        <Input id="takeover-address" label="Street address" value={address} onChange={(e) => setAddress(e.target.value)} />
        <div className="max-w-xs">
          <Input id="takeover-postal" label="Postal code" value={postal} onChange={(e) => setPostal(e.target.value)} />
        </div>
        <Button type="submit" isLoading={busy}>
          Ask the owner
        </Button>
      </form>
    </section>
  )
}

function TakeoverCard({ t, admin = false, onChanged, toast }: { t: Takeover; admin?: boolean; onChanged: () => void; toast: ToastApi }) {
  const [mode, setMode] = useState<'confirm' | 'decline' | null>(null)
  const [associates, setAssociates] = useState<TakeoverAssociates | ''>('')
  const [password, setPassword] = useState('')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const run = async (task: () => Promise<unknown>, done: string) => {
    setBusy(true)
    setError(null)
    try {
      await task()
      toast.success(done)
      setMode(null)
      setPassword('')
      onChanged()
    } catch (err) {
      setError(getErrorMessage(err, 'That did not work.'))
    } finally {
      setBusy(false)
    }
  }

  const canConfirm = t.can.includes('confirm') || t.can.includes('confirm-as-admin')

  return (
    <div data-testid={`takeover-${t.id}`} className="rounded-xl border border-gray-200 bg-white p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="font-semibold text-gray-900">{t.existingShopName}</p>
          <p className="text-sm text-gray-600">
            Asked by {t.requesterName} ({t.requesterEmail}) from the shop &ldquo;{t.newShopName}&rdquo;
            {admin && t.existingOwnerName && <> · current owner {t.existingOwnerName}</>}
          </p>
          {t.status === 'Completed' && (
            <p className="text-sm text-gray-700">
              {t.associates === 'Keep' ? 'Only the owner changed; the associates stayed.' : 'The old shop was closed and the new owner’s shop took its place.'}
              {t.decidedByName && <> Answered by {t.decidedByName}{t.decidedByAdmin ? ' (administrator, for the owner)' : ''}.</>}
            </p>
          )}
          {t.note && <p className="mt-1 text-sm text-gray-700">“{t.note}”</p>}
        </div>
        <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_STYLE[t.status]}`}>{STATUS_LABEL[t.status]}</span>
      </div>

      {t.status === 'Requested' && (
        <div className="mt-3 flex flex-wrap gap-2">
          {canConfirm && (
            <Button size="sm" onClick={() => setMode(mode === 'confirm' ? null : 'confirm')}>
              {t.can.includes('confirm') ? 'Hand the shop over' : 'Confirm for the owner'}
            </Button>
          )}
          {t.can.includes('decline') && (
            <Button size="sm" variant="secondary" onClick={() => setMode(mode === 'decline' ? null : 'decline')}>
              Decline
            </Button>
          )}
          {t.can.includes('cancel') && (
            <Button size="sm" variant="ghost" disabled={busy} onClick={() => run(() => takeoverService.cancel(t.id), 'The request was cancelled.')}>
              Cancel the request
            </Button>
          )}
        </div>
      )}

      {error && (
        <p role="alert" className="mt-3 text-sm text-red-700">
          {error}
        </p>
      )}

      {mode === 'confirm' && (
        <form
          className="mt-3 space-y-3 rounded-lg bg-gray-50 p-4"
          onSubmit={(e) => {
            e.preventDefault()
            if (!associates) return setError('Choose what happens to the associates.')
            if (t.can.includes('confirm')) {
              if (!password) return setError('Enter your password.')
              run(() => takeoverService.confirm(t.id, associates, password), 'The shop was handed over.')
            } else {
              if (!note.trim()) return setError('Say why you are answering for the owner.')
              run(() => takeoverService.confirmAsAdmin(t.id, associates, note), 'The takeover was done on the owner’s behalf.')
            }
          }}
        >
          <fieldset className="space-y-2">
            <legend className="text-sm font-medium text-gray-800">What happens to the sales associates?</legend>
            <label className="flex items-start gap-2 text-sm text-gray-800">
              <input type="radio" name={`assoc-${t.id}`} checked={associates === 'Keep'} onChange={() => setAssociates('Keep')} className="mt-1" />
              <span>
                <span className="font-medium">They stay.</span> Only the owner changes. The shop keeps its banners, files and plan; the new owner must confirm who approves
                banners; the old owner&apos;s login is switched off.
              </span>
            </label>
            <label className="flex items-start gap-2 text-sm text-gray-800">
              <input type="radio" name={`assoc-${t.id}`} checked={associates === 'Replace'} onChange={() => setAssociates('Replace')} className="mt-1" />
              <span>
                <span className="font-medium">They change too.</span> The old shop closes, all its logins are switched off and its plan stops renewing. The new owner&apos;s
                shop takes the address and adds new associates.
              </span>
            </label>
          </fieldset>

          {t.can.includes('confirm') ? (
            <Input id={`pw-${t.id}`} type="password" label="Your password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
          ) : (
            <div>
              <label htmlFor={`note-${t.id}`} className="text-sm font-medium text-gray-700">
                Why are you answering for the owner?
              </label>
              <textarea
                id={`note-${t.id}`}
                rows={2}
                maxLength={500}
                value={note}
                onChange={(e) => setNote(e.target.value)}
                className="mt-1 w-full rounded-lg border-2 border-gray-300 px-3 py-2 text-gray-900 focus-visible:outline-none focus:border-blue-500"
              />
            </div>
          )}
          <Button type="submit" isLoading={busy}>
            {t.can.includes('confirm') ? 'Yes, hand it over' : 'Confirm the takeover'}
          </Button>
        </form>
      )}

      {mode === 'decline' && (
        <div className="mt-3 space-y-2 rounded-lg bg-gray-50 p-4">
          <label htmlFor={`decline-${t.id}`} className="text-sm font-medium text-gray-700">
            Reason (optional)
          </label>
          <textarea
            id={`decline-${t.id}`}
            rows={2}
            maxLength={500}
            value={note}
            onChange={(e) => setNote(e.target.value)}
            className="w-full rounded-lg border-2 border-gray-300 px-3 py-2 text-gray-900 focus-visible:outline-none focus:border-blue-500"
          />
          <Button size="sm" isLoading={busy} onClick={() => run(() => takeoverService.decline(t.id, note), 'The request was declined.')}>
            Decline the request
          </Button>
        </div>
      )}
    </div>
  )
}
