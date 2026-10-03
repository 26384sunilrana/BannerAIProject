'use client'

import React, { FormEvent, useCallback, useEffect, useState } from 'react'
import { AppShell } from '@/components/layout/AppShell'
import { Button, Input, Select, Toast } from '@/components/Common'
import { LocationPicker, PickedLocation } from '@/components/location/LocationPicker'
import { useToast } from '@/hooks/useToast'
import { AdKind, AdRate, AdRateInput, RateLevel, rateService } from '@/api/adService'
import { getErrorMessage } from '@/api/client'
import { Roles } from '@/lib/session'

const KIND_OPTIONS = [
  { value: 'Side', label: 'Side strip' },
  { value: 'Mega', label: 'Mega ad' },
  { value: 'Popup', label: 'Popup' },
  { value: 'Minor', label: 'Corner tile' },
]

const LEVEL_LABEL: Record<RateLevel, string> = { All: 'Every shop', Country: 'Country', State: 'State', City: 'City' }

/** The level of a rate is the deepest place that was chosen. */
const levelOf = (place: PickedLocation): RateLevel => (place.cityId ? 'City' : place.stateId ? 'State' : place.countryCode ? 'Country' : 'All')

export default function AdRatesPage() {
  return (
    <AppShell roles={[Roles.Admin]}>
      <Rates />
    </AppShell>
  )
}

function Rates() {
  const toast = useToast()
  const [rates, setRates] = useState<AdRate[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [editing, setEditing] = useState<AdRate | 'new' | null>(null)
  const [place, setPlace] = useState<PickedLocation>({})
  const [kind, setKind] = useState('')
  const [price, setPrice] = useState('')
  const [share, setShare] = useState('100')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    try {
      setRates(await rateService.list())
      setLoadError(null)
    } catch (err) {
      setLoadError(getErrorMessage(err, 'The rates could not be loaded.'))
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const open = (rate: AdRate | 'new') => {
    setEditing(rate)
    setError(null)
    if (rate === 'new') {
      setPlace({})
      setKind('')
      setPrice('')
      setShare('100')
    } else {
      setPlace({ countryCode: rate.countryCode ?? undefined, stateId: rate.stateId ?? undefined, cityId: rate.cityId ?? undefined })
      setKind(rate.kind ?? '')
      setPrice(String(rate.pricePerHour))
      setShare(String(rate.shopSharePercent))
    }
  }

  const save = async (event: FormEvent) => {
    event.preventDefault()
    const pricePerHour = Number(price)
    const shopSharePercent = Number(share)
    if (price.trim() === '' || Number.isNaN(pricePerHour) || pricePerHour < 0) return setError('Enter the price for one hour.')
    if (Number.isNaN(shopSharePercent) || shopSharePercent < 0 || shopSharePercent > 100) return setError('The shop share is between 0 and 100 percent.')

    const level = levelOf(place)
    const input: AdRateInput = {
      level,
      countryCode: level === 'Country' ? place.countryCode ?? null : null,
      stateId: level === 'State' ? place.stateId ?? null : null,
      cityId: level === 'City' ? place.cityId ?? null : null,
      kind: (kind || null) as AdKind | null,
      pricePerHour,
      shopSharePercent,
    }

    setBusy(true)
    setError(null)
    try {
      if (editing && editing !== 'new') await rateService.update(editing.id, input)
      else await rateService.create(input)
      setEditing(null)
      toast.success('The rate was saved.')
      await load()
    } catch (err) {
      setError(getErrorMessage(err, 'The rate could not be saved.'))
    } finally {
      setBusy(false)
    }
  }

  const switchOff = async (rate: AdRate) => {
    try {
      await rateService.deactivate(rate.id)
      toast.success('The rate was switched off. Ads already booked keep their price.')
      await load()
    } catch (err) {
      toast.error(getErrorMessage(err, 'The rate could not be switched off.'))
    }
  }

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">Ad rates</h1>
          <p className="mt-1 max-w-2xl text-gray-600">
            What an hour of an ad you book on a shop screen is worth, by place and kind of ad. The narrowest place wins: a city rate before a state rate, a
            country rate, and a rate for every shop. For side strips and mega ads the price is for the whole screen and is cut down to the share the ad
            takes. The shop is paid its share for the hours the ad ran.
          </p>
        </div>
        {!editing && <Button onClick={() => open('new')}>Add a rate</Button>}
      </header>

      {editing && (
        <form onSubmit={save} noValidate className="space-y-4 rounded-xl border border-gray-200 bg-white p-5">
          {error && (
            <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
              {error}
            </div>
          )}
          <fieldset className="space-y-2">
            <legend className="text-sm font-medium text-gray-700">Where it applies</legend>
            <p className="text-xs text-gray-500">Leave everything empty for every shop, or choose a country, a state or a city. Now: {LEVEL_LABEL[levelOf(place)]}.</p>
            <LocationPicker value={place} onChange={(next) => setPlace({ ...next, groupId: undefined })} />
          </fieldset>
          <div className="grid gap-4 sm:grid-cols-3">
            <Select id="rate-kind" label="Kind of ad" options={KIND_OPTIONS} placeholder="Every kind" value={kind} onChange={(e) => setKind(e.target.value)} />
            <Input id="rate-price" type="number" min={0} step="0.01" label="Price for one hour" value={price} onChange={(e) => setPrice(e.target.value)} />
            <Input id="rate-share" type="number" min={0} max={100} label="Paid to the shop (%)" value={share} onChange={(e) => setShare(e.target.value)} />
          </div>
          <div className="flex gap-3">
            <Button type="submit" isLoading={busy}>
              Save rate
            </Button>
            <Button type="button" variant="secondary" onClick={() => setEditing(null)} disabled={busy}>
              Cancel
            </Button>
          </div>
        </form>
      )}

      {loadError && (
        <p role="alert" className="text-red-700">
          {loadError}
        </p>
      )}
      {!rates && !loadError && <p className="text-gray-500">Loading the rates…</p>}
      {rates?.length === 0 && (
        <p className="rounded-xl border border-dashed border-gray-300 p-8 text-center text-gray-500">
          No rates yet. An ad you book on a shop cannot be sent in until a rate covers that shop.
        </p>
      )}

      {rates && rates.length > 0 && (
        <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
          <table className="w-full min-w-[40rem] text-left text-sm">
            <thead className="bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-4 py-2">Place</th>
                <th className="px-4 py-2">Kind of ad</th>
                <th className="px-4 py-2 text-right">Per hour</th>
                <th className="px-4 py-2 text-right">Shop share</th>
                <th className="px-4 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {rates.map((rate) => (
                <tr key={rate.id} data-testid={`rate-${rate.place}-${rate.kind ?? 'all'}`} className={rate.isActive ? '' : 'text-gray-400'}>
                  <td className="px-4 py-2">
                    {rate.place} <span className="text-xs text-gray-500">({LEVEL_LABEL[rate.level]})</span>
                    {!rate.isActive && <span className="ml-2 text-xs">switched off</span>}
                  </td>
                  <td className="px-4 py-2">{rate.kind ? KIND_OPTIONS.find((k) => k.value === rate.kind)?.label : 'Every kind'}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{rate.pricePerHour.toFixed(2)}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{rate.shopSharePercent}%</td>
                  <td className="space-x-2 px-4 py-2 text-right">
                    {rate.isActive && (
                      <>
                        <Button size="sm" variant="secondary" onClick={() => open(rate)}>
                          Change
                        </Button>
                        <Button size="sm" variant="ghost" onClick={() => switchOff(rate)}>
                          Switch off
                        </Button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
