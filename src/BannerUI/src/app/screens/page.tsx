'use client'

import React, { FormEvent, useCallback, useEffect, useState } from 'react'
import { AppShell } from '@/components/layout/AppShell'
import { Button, ConfirmDialog, Input, Select, Toast } from '@/components/Common'
import { useAuth } from '@/context/AuthContext'
import { useToast } from '@/hooks/useToast'
import { PlayReport, ScreenInfo, screenService } from '@/api/screenService'
import { getErrorMessage } from '@/api/client'
import { formatInZone } from '@/lib/timeZones'
import { useShopTimeZone } from '@/hooks/useShopTimeZone'
import { Roles } from '@/lib/session'

const KIND_LABEL: Record<string, string> = { Banner: 'Banner', Ad: 'Ad', DefaultBoard: 'Default board' }

export default function ScreensPage() {
  return (
    <AppShell roles={[Roles.ShopOwner, Roles.SalesExecutive]}>
      <Screens />
    </AppShell>
  )
}

function Screens() {
  const { user, hasRole } = useAuth()
  const isOwner = hasRole(Roles.ShopOwner)
  const shopId = user?.shopId ?? null
  const zone = useShopTimeZone().timeZoneId
  const toast = useToast()

  const [screens, setScreens] = useState<ScreenInfo[] | null>(null)
  const [report, setReport] = useState<PlayReport | null>(null)
  const [days, setDays] = useState('7')
  const [loadError, setLoadError] = useState<string | null>(null)
  const [code, setCode] = useState('')
  const [name, setName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [renaming, setRenaming] = useState<{ id: string; name: string } | null>(null)
  const [removing, setRemoving] = useState<ScreenInfo | null>(null)

  const load = useCallback(async () => {
    if (!shopId) return
    try {
      setScreens(await screenService.list(shopId))
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'The screens could not be loaded.'))
    }
  }, [shopId])

  useEffect(() => {
    load()
    // a screen that has just come online shows up without a reload
    const timer = setInterval(load, 20_000)
    return () => clearInterval(timer)
  }, [load])

  useEffect(() => {
    if (!shopId) return
    screenService.report(shopId, Number(days)).then(setReport).catch(() => setReport(null))
  }, [shopId, days, screens?.length])

  const add = async (event: FormEvent) => {
    event.preventDefault()
    if (!shopId) return
    if (code.replace(/[^a-z0-9]/gi, '').length !== 6) return setError('Type the six characters the screen shows.')
    setBusy(true)
    setError(null)
    try {
      await screenService.pair(shopId, code, name)
      setCode('')
      setName('')
      toast.success('The screen was added. It starts by itself within a few seconds.')
      await load()
    } catch (err) {
      setError(getErrorMessage(err, 'The screen could not be added.'))
    } finally {
      setBusy(false)
    }
  }

  const rename = async () => {
    if (!shopId || !renaming) return
    try {
      await screenService.rename(shopId, renaming.id, renaming.name)
      setRenaming(null)
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'The screen could not be renamed.'))
    }
  }

  const remove = async () => {
    if (!shopId || !removing) return
    try {
      await screenService.remove(shopId, removing.id)
      toast.success('The screen was removed. It shows a new code and can be added again.')
      setRemoving(null)
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'The screen could not be removed.'))
      setRemoving(null)
    }
  }

  if (!shopId) return <p className="text-gray-600">Your account is not linked to a shop.</p>
  const hasScreen = (screens?.length ?? 0) > 0

  return (
    <div className="space-y-8">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Screens</h1>
        <p className="mt-1 max-w-3xl text-gray-600">
          The television or tablet in your shop. Nobody signs in on it: it shows a code once, you type the code here, and from then on it plays your banners and ads by
          itself. You are told here when it goes offline.
        </p>
      </header>

      {loadError && (
        <p role="alert" className="text-red-700">
          {loadError}
        </p>
      )}
      {!screens && !loadError && <p className="text-gray-500">Loading…</p>}

      {screens?.map((screen) => (
        <section key={screen.id} data-testid={`screen-${screen.name}`} className="rounded-xl border border-gray-200 bg-white p-4">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="text-lg font-semibold text-gray-900">{screen.name}</p>
              <p className="text-sm text-gray-600">
                {screen.lastSeenAt ? `Last heard from ${formatInZone(screen.lastSeenAt, zone)}` : 'Not heard from yet'}
                {screen.appVersion && ` · ${screen.appVersion}`}
              </p>
            </div>
            <span
              data-testid="screen-state"
              className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${screen.online ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}
            >
              {screen.online ? 'Online' : 'Offline'}
            </span>
          </div>
          {isOwner && (
            <div className="mt-3 flex flex-wrap gap-2">
              <Button size="sm" variant="secondary" onClick={() => setRenaming({ id: screen.id, name: screen.name })}>
                Rename
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setRemoving(screen)}>
                Remove this screen
              </Button>
            </div>
          )}
          {renaming?.id === screen.id && (
            <div className="mt-3 flex max-w-md items-end gap-2">
              <div className="flex-1">
                <Input id="screen-rename" label="Name" value={renaming.name} onChange={(e) => setRenaming({ id: screen.id, name: e.target.value })} />
              </div>
              <Button size="sm" onClick={rename}>
                Save
              </Button>
              <Button size="sm" variant="secondary" onClick={() => setRenaming(null)}>
                Cancel
              </Button>
            </div>
          )}
        </section>
      ))}

      {screens && !hasScreen && (
        <p className="rounded-xl border border-dashed border-gray-300 p-6 text-center text-gray-500" data-testid="no-screen">
          No screen is added yet.
        </p>
      )}

      {isOwner && screens && !hasScreen && (
        <section aria-labelledby="add-heading">
          <h2 id="add-heading" className="text-lg font-semibold text-gray-900">
            Add a screen
          </h2>
          <ol className="mt-2 list-decimal space-y-1 pl-5 text-sm text-gray-700">
            <li>
              On the television, open the web browser and go to <b>{typeof window !== 'undefined' ? `${window.location.origin}/player` : '/player'}</b>
            </li>
            <li>The television shows a code of six characters.</li>
            <li>Type that code here.</li>
          </ol>
          <form onSubmit={add} noValidate className="mt-3 max-w-md space-y-3 rounded-xl border border-gray-200 bg-white p-5">
            {error && (
              <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
                {error}
              </div>
            )}
            <Input id="screen-code" label="Code on the screen" value={code} onChange={(e) => setCode(e.target.value.toUpperCase())} autoComplete="off" maxLength={8} />
            <Input id="screen-name" label="Name (optional)" value={name} onChange={(e) => setName(e.target.value)} placeholder="Shop window" maxLength={60} />
            <Button type="submit" isLoading={busy}>
              Add the screen
            </Button>
          </form>
        </section>
      )}

      <section aria-labelledby="played-heading" className="space-y-3">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h2 id="played-heading" className="text-lg font-semibold text-gray-900">
              What was shown
            </h2>
            <p className="text-sm text-gray-600">Hours each banner, ad and the default board were on your screen, counted from what the screen reports every minute.</p>
          </div>
          <div className="w-44">
            <Select
              id="played-days"
              label="Period"
              options={[
                { value: '7', label: 'Last 7 days' },
                { value: '30', label: 'Last 30 days' },
                { value: '90', label: 'Last 90 days' },
              ]}
              value={days}
              onChange={(e) => setDays(e.target.value)}
            />
          </div>
        </div>
        {report && report.rows.length === 0 && (
          <p className="rounded-xl border border-dashed border-gray-300 p-6 text-center text-gray-500" data-testid="no-play">
            Nothing was reported in this period.
          </p>
        )}
        {report && report.rows.length > 0 && (
          <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
            <table className="w-full min-w-[28rem] text-left text-sm">
              <thead className="bg-gray-50 text-xs uppercase text-gray-500">
                <tr>
                  <th className="px-4 py-2">What</th>
                  <th className="px-4 py-2">Kind</th>
                  <th className="px-4 py-2 text-right">Hours on screen</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {report.rows.map((row) => (
                  <tr key={`${row.kind}-${row.refId}`} data-testid={`play-${row.label}`}>
                    <td className="px-4 py-2 font-medium text-gray-900">{row.label}</td>
                    <td className="px-4 py-2 text-gray-700">{KIND_LABEL[row.kind]}</td>
                    <td className="px-4 py-2 text-right tabular-nums">{row.hours.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <ConfirmDialog
        isOpen={!!removing}
        title="Remove this screen?"
        message="It stops showing your banners at once and goes back to showing a pairing code. You can add it again with the new code."
        confirmText="Remove the screen"
        isDangerous
        onConfirm={remove}
        onCancel={() => setRemoving(null)}
      />
    </div>
  )
}
