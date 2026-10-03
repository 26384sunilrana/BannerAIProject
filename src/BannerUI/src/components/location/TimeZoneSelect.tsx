'use client'

import React, { useMemo, useState } from 'react'
import { Button, Select } from '@/components/Common'
import { supportedTimeZones, zoneLabel } from '@/lib/timeZones'

interface Props {
  id: string
  label: string
  /** The chosen IANA name; empty means "follow the level above". */
  value: string
  onChange: (value: string) => void
  /** Text for the empty choice, for example "Same as my city (Asia/Kolkata)". */
  inheritLabel: string
  disabled?: boolean
  helperText?: string
  /** Show the list straight away instead of the chosen zone with a Change button. */
  alwaysOpen?: boolean
}

/**
 * A time zone chosen from a list of several hundred. It shows the chosen zone (or what it falls back to) and only opens the
 * long list when asked, which keeps forms light.
 */
export function TimeZoneSelect({ id, label, value, onChange, inheritLabel, disabled, helperText, alwaysOpen = false }: Props) {
  const [open, setOpen] = useState(false)

  const options = useMemo(() => {
    if (!alwaysOpen && !open) return []
    const names = supportedTimeZones()
    // a value stored from another machine may be missing from this browser's list: keep it selectable
    const all = value && !names.includes(value) ? [value, ...names] : names
    // plain names: working out the offset of several hundred zones would be slow, so only the chosen one gets it (below)
    return all.map((zone) => ({ value: zone, label: zone }))
  }, [value, open, alwaysOpen])

  if (!alwaysOpen && !open) {
    return (
      <div className="flex flex-col gap-1" data-testid={`${id}-summary`}>
        <span className="text-sm font-medium text-gray-700">{label}</span>
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-gray-900">{value ? zoneLabel(value) : inheritLabel}</span>
          <Button type="button" size="sm" variant="ghost" disabled={disabled} onClick={() => setOpen(true)} aria-label={`Change ${label.toLowerCase()}`}>
            Change
          </Button>
          {value && (
            <Button type="button" size="sm" variant="ghost" disabled={disabled} onClick={() => onChange('')}>
              Use the default
            </Button>
          )}
        </div>
        {helperText && <span className="text-sm text-gray-500">{helperText}</span>}
      </div>
    )
  }

  return (
    <Select
      id={id}
      label={label}
      options={options}
      placeholder={inheritLabel}
      value={value}
      disabled={disabled}
      helperText={value ? [zoneLabel(value), helperText].filter(Boolean).join('. ') : helperText}
      onChange={(e) => {
        onChange(e.target.value)
        if (!alwaysOpen) setOpen(false)
      }}
    />
  )
}
