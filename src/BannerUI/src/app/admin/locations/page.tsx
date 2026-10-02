'use client'

import React, { useCallback, useEffect, useState } from 'react'
import Link from 'next/link'
import { Toast } from '@/components/Common'
import { LocationColumn } from '@/components/location/LocationColumn'
import { useToast } from '@/hooks/useToast'
import { locationService } from '@/api/locationService'
import { getErrorMessage } from '@/api/client'
import { GroupShop, LocationItem, LocationLevel, NewLocation } from '@/types/location'

export default function AdminLocationsPage() {
  const toast = useToast()

  const [countries, setCountries] = useState<LocationItem[] | null>(null)
  const [states, setStates] = useState<LocationItem[] | null>(null)
  const [cities, setCities] = useState<LocationItem[] | null>(null)
  const [groups, setGroups] = useState<LocationItem[] | null>(null)
  const [shops, setShops] = useState<GroupShop[] | null>(null)

  const [country, setCountry] = useState<LocationItem | null>(null)
  const [state, setState] = useState<LocationItem | null>(null)
  const [city, setCity] = useState<LocationItem | null>(null)
  const [group, setGroup] = useState<LocationItem | null>(null)

  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async () => {
    try {
      const [c, s, ci, g, sh] = await Promise.all([
        locationService.list('countries', { includeInactive: true }),
        country ? locationService.list('states', { parentId: country.id, includeInactive: true }) : Promise.resolve(null),
        state ? locationService.list('cities', { parentId: state.id, includeInactive: true }) : Promise.resolve(null),
        city ? locationService.list('groups', { parentId: city.id, includeInactive: true }) : Promise.resolve(null),
        group ? locationService.shopsInGroup(group.id) : Promise.resolve(null),
      ])
      setCountries(c)
      setStates(s)
      setCities(ci)
      setGroups(g)
      setShops(sh)
      setError(null)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not load the places.'))
    }
  }, [country, state, city, group])

  useEffect(() => {
    refresh()
  }, [refresh])

  /** Runs a change, shows what happened, and reloads the lists. Returns whether it worked. */
  const attempt = async (task: () => Promise<unknown>, success: string): Promise<boolean> => {
    setBusy(true)
    try {
      await task()
      toast.success(success)
      await refresh()
      return true
    } catch (err) {
      toast.error(getErrorMessage(err, 'That did not work.'))
      return false
    } finally {
      setBusy(false)
    }
  }

  const parentFor = (level: LocationLevel): string | undefined =>
    ({ countries: undefined, states: country?.id, cities: state?.id, groups: city?.id })[level]

  const handlers = (level: LocationLevel, noun: string) => ({
    busy,
    onAdd: (value: NewLocation) => attempt(() => locationService.create(level, parentFor(level), value), `${noun} added`),
    onRename: (item: LocationItem, name: string) => attempt(() => locationService.update(level, item, { name }), `${noun} renamed`),
    onToggle: async (item: LocationItem) => {
      await attempt(
        () => locationService.update(level, item, { isActive: !item.isActive }),
        `${item.name} is now ${item.isActive ? 'off' : 'on'}`
      )
    },
    onDelete: async (item: LocationItem) => {
      const deleted = await attempt(() => locationService.remove(level, item.id), `${item.name} deleted`)
      if (!deleted) return
      if (level === 'countries' && country?.id === item.id) selectCountry(null)
      if (level === 'states' && state?.id === item.id) selectState(null)
      if (level === 'cities' && city?.id === item.id) selectCity(null)
      if (level === 'groups' && group?.id === item.id) setGroup(null)
    },
  })

  // choosing a place clears everything chosen below it
  const selectCountry = (item: LocationItem | null) => {
    setCountry(item); setState(null); setCity(null); setGroup(null)
    setStates(null); setCities(null); setGroups(null); setShops(null)
  }
  const selectState = (item: LocationItem | null) => {
    setState(item); setCity(null); setGroup(null)
    setCities(null); setGroups(null); setShops(null)
  }
  const selectCity = (item: LocationItem | null) => {
    setCity(item); setGroup(null)
    setGroups(null); setShops(null)
  }

  return (
    <div className="space-y-6">
      <Toast messages={toast.messages} onRemove={toast.remove} />

      <header>
        <h1 className="text-2xl font-semibold text-gray-900">Places</h1>
        <p className="mt-1 text-gray-600">
          Country, state, city and group, each with its own identifier. A group is an area inside a city; shops are placed in a city
          and, if you like, a group. Pick a place to see what is under it.
        </p>
      </header>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <LocationColumn
          title="Countries" noun="Country" addKind="country" childNoun="state" items={countries}
          selectedId={country?.id ?? null} onSelect={selectCountry} {...handlers('countries', 'Country')}
        />
        <LocationColumn
          title="States" noun="State" addKind="state" childNoun="city" items={states}
          blockedReason={country ? undefined : 'Choose a country first.'}
          selectedId={state?.id ?? null} onSelect={selectState} {...handlers('states', 'State')}
        />
        <LocationColumn
          title="Cities" noun="City" addKind="name" childNoun="group" items={cities}
          blockedReason={state ? undefined : 'Choose a state first.'}
          selectedId={city?.id ?? null} onSelect={selectCity} {...handlers('cities', 'City')}
        />
        <LocationColumn
          title="Groups" noun="Group" addKind="name" items={groups}
          blockedReason={city ? undefined : 'Choose a city first.'}
          selectedId={group?.id ?? null} onSelect={setGroup} {...handlers('groups', 'Group')}
        />
      </div>

      {group && (
        <section aria-label="Shops in the group" className="rounded-xl border border-gray-200 bg-white">
          <header className="border-b border-gray-100 px-4 py-3">
            <h2 className="font-semibold text-gray-900">
              Shops in {group.name} <span className="font-mono text-xs font-normal text-gray-500">{group.uniqueId}</span>
            </h2>
          </header>
          {shops === null && <p className="px-4 py-6 text-sm text-gray-500">Loading…</p>}
          {shops?.length === 0 && <p className="px-4 py-6 text-sm text-gray-500">No shop is in this group yet.</p>}
          <ul className="divide-y divide-gray-100">
            {shops?.map((shop) => (
              <li key={shop.id} data-testid={`group-shop-${shop.name}`} className="flex flex-wrap items-center justify-between gap-2 px-4 py-2">
                <div>
                  <Link href={`/shops/${shop.id}`} className="font-medium text-blue-600 hover:text-blue-800">
                    {shop.name}
                  </Link>
                  <span className="ml-2 font-mono text-xs text-gray-500">{shop.uniqueId ?? 'no identifier yet'}</span>
                </div>
                <span className="text-sm text-gray-600">{shop.status}</span>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  )
}
