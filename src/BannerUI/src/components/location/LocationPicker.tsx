'use client'

import React, { useEffect, useState } from 'react'
import { Select } from '@/components/Common'
import { locationService } from '@/api/locationService'
import { getErrorMessage } from '@/api/client'
import { LocationItem } from '@/types/location'

export interface PickedLocation {
  countryCode?: string
  stateId?: number
  cityId?: number
  groupId?: number
}

interface Props {
  value: PickedLocation
  onChange: (next: PickedLocation) => void
  disabled?: boolean
}

const toOptions = (items: LocationItem[]) => items.map((i) => ({ value: i.id, label: i.name }))

/**
 * Country, state, city and (optionally) the group of that city. Choosing a higher level clears the ones below it,
 * and each list only offers places that are switched on.
 */
export function LocationPicker({ value, onChange, disabled = false }: Props) {
  const [countries, setCountries] = useState<LocationItem[]>([])
  const [states, setStates] = useState<LocationItem[]>([])
  const [cities, setCities] = useState<LocationItem[]>([])
  const [groups, setGroups] = useState<LocationItem[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = async <T,>(task: Promise<T>, set: (v: T) => void) => {
    try {
      set(await task)
      setError(null)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not load the places.'))
    }
  }

  useEffect(() => {
    load(locationService.list('countries'), setCountries)
  }, [])

  useEffect(() => {
    if (!value.countryCode) return setStates([])
    load(locationService.list('states', { parentId: value.countryCode }), setStates)
  }, [value.countryCode])

  useEffect(() => {
    if (!value.stateId) return setCities([])
    load(locationService.list('cities', { parentId: String(value.stateId) }), setCities)
  }, [value.stateId])

  useEffect(() => {
    if (!value.cityId) return setGroups([])
    load(locationService.list('groups', { parentId: String(value.cityId) }), setGroups)
  }, [value.cityId])

  const number = (text: string) => (text ? Number(text) : undefined)

  return (
    <div className="space-y-3">
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
      <div className="grid gap-3 sm:grid-cols-2">
        <Select
          id="pick-country"
          label="Country"
          options={toOptions(countries)}
          value={value.countryCode ?? ''}
          disabled={disabled}
          onChange={(e) => onChange({ countryCode: e.target.value || undefined })}
        />
        <Select
          id="pick-state"
          label="State"
          options={toOptions(states)}
          value={value.stateId ? String(value.stateId) : ''}
          disabled={disabled || !value.countryCode}
          onChange={(e) => onChange({ countryCode: value.countryCode, stateId: number(e.target.value) })}
        />
        <Select
          id="pick-city"
          label="City"
          options={toOptions(cities)}
          value={value.cityId ? String(value.cityId) : ''}
          disabled={disabled || !value.stateId}
          helperText={value.stateId && cities.length === 0 ? 'No cities here yet. Ask the administrator to add yours.' : undefined}
          onChange={(e) => onChange({ countryCode: value.countryCode, stateId: value.stateId, cityId: number(e.target.value) })}
        />
        <Select
          id="pick-group"
          label="Group (optional)"
          options={toOptions(groups)}
          value={value.groupId ? String(value.groupId) : ''}
          disabled={disabled || !value.cityId}
          onChange={(e) => onChange({ ...value, groupId: number(e.target.value) })}
        />
      </div>
    </div>
  )
}
