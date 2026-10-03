'use client'

import React, { FormEvent, useState } from 'react'
import Link from 'next/link'
import { Button, Input } from '@/components/Common'
import { useShopTimeZone } from '@/hooks/useShopTimeZone'
import { bannerListService, DailyHours } from '@/api/workflowService'
import { getErrorMessage } from '@/api/client'
import { formatWindow } from '@/lib/dates'
import { ALL_DAYS, DAY_BITS, WEEKDAYS, describeDaily, fromMinutes, toMinutes, utcToWallTime, wallTimeToUtcIso, zoneLabel } from '@/lib/timeZones'
import { BannerSummary } from '@/types/workflow'

interface SchedulePickerProps {
  banner: BannerSummary
  /** True when changing the schedule will send an approved or live banner back for approval. */
  needsReapprovalOnChange: boolean
  onSaved: (message: string) => void | Promise<void>
}

/**
 * Shows when a banner runs and lets the owner change it. Times are the shop's own clock (its time zone), not the clock of
 * the computer being used. A banner can run only at certain hours of each day, so several banners can share dates; two
 * banners still cannot be shown at the same moment.
 */
export function SchedulePicker({ banner, needsReapprovalOnChange, onSaved }: SchedulePickerProps) {
  const zone = useShopTimeZone()
  const [open, setOpen] = useState(false)
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [limited, setLimited] = useState(false)
  const [from, setFrom] = useState('09:00')
  const [to, setTo] = useState('17:00')
  const [days, setDays] = useState(ALL_DAYS)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const hasSchedule = !!banner.publishStartAt && !!banner.publishEndAt
  const hasDaily = banner.dailyStartMinutes != null && banner.dailyEndMinutes != null

  const load = () => {
    setStart(utcToWallTime(banner.publishStartAt, zone.timeZoneId))
    setEnd(utcToWallTime(banner.publishEndAt, zone.timeZoneId))
    setLimited(hasDaily)
    if (hasDaily) {
      setFrom(fromMinutes(banner.dailyStartMinutes!))
      setTo(fromMinutes(banner.dailyEndMinutes!))
      setDays(banner.activeDays ?? ALL_DAYS)
    } else {
      setFrom('09:00')
      setTo('17:00')
      setDays(ALL_DAYS)
    }
    setError(null)
  }

  const toggleDay = (bit: number) => setDays((current) => (current & bit ? current & ~bit : current | bit))

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const startIso = wallTimeToUtcIso(start, zone.timeZoneId)
    const endIso = wallTimeToUtcIso(end, zone.timeZoneId)
    if (!startIso || !endIso) return setError('Choose when the banner starts and when it ends.')
    if (new Date(endIso) <= new Date(startIso)) return setError('The end must be after the start.')
    if (new Date(endIso) <= new Date()) return setError('The end is already in the past.')

    let daily: DailyHours | null = null
    if (limited) {
      const startMinutes = toMinutes(from)
      const endMinutes = toMinutes(to)
      if (startMinutes === null || endMinutes === null) return setError('Choose the hours of the day.')
      if (startMinutes === endMinutes) return setError('The hours cannot start and end at the same time.')
      if ((days & ALL_DAYS) === 0) return setError('Choose at least one day of the week.')
      daily = { startMinutes, endMinutes, days }
    }

    setBusy(true)
    setError(null)
    try {
      const result = await bannerListService.setSchedule(banner.id, startIso, endIso, daily)
      setOpen(false)
      await onSaved(result.requiresReapproval ? 'Schedule saved. The banner needs to be approved again.' : 'Schedule saved.')
    } catch (err) {
      setError(getErrorMessage(err, 'Could not save the schedule.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="w-full">
      <div className="flex flex-wrap items-center gap-3">
        <span className={hasSchedule ? 'text-sm text-gray-700' : 'text-sm text-amber-700'} data-testid="schedule-summary">
          {hasSchedule ? formatWindow(banner.publishStartAt, banner.publishEndAt, zone.timeZoneId) : 'Not scheduled'}
          {hasSchedule && hasDaily && (
            <span className="block text-xs text-gray-500" data-testid="schedule-daily">
              {describeDaily(banner.dailyStartMinutes, banner.dailyEndMinutes, banner.activeDays)}
            </span>
          )}
        </span>
        <button
          type="button"
          className="text-sm text-blue-600 hover:text-blue-800"
          aria-expanded={open}
          onClick={() => {
            if (!open) load()
            setOpen(!open)
          }}
        >
          {hasSchedule ? 'Change schedule' : 'Set schedule'}
        </button>
      </div>

      {open && (
        <form onSubmit={submit} noValidate className="mt-3 max-w-xl space-y-3 rounded-lg border border-gray-200 bg-gray-50 p-4">
          {error && (
            <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
              {error}
            </div>
          )}
          <div className="grid gap-3 sm:grid-cols-2">
            <Input id={`start-${banner.id}`} type="datetime-local" label="Starts" value={start} onChange={(e) => setStart(e.target.value)} />
            <Input id={`end-${banner.id}`} type="datetime-local" label="Ends" value={end} onChange={(e) => setEnd(e.target.value)} />
          </div>

          <label className="flex items-center gap-2 text-sm text-gray-800">
            <input type="checkbox" checked={limited} onChange={(e) => setLimited(e.target.checked)} />
            Only at certain hours each day
          </label>
          {limited && (
            <div className="space-y-3 rounded-lg border border-gray-200 bg-white p-3">
              <div className="grid gap-3 sm:grid-cols-2">
                <Input id={`from-${banner.id}`} type="time" label="From" value={from} onChange={(e) => setFrom(e.target.value)} />
                <Input id={`to-${banner.id}`} type="time" label="Until" value={to} onChange={(e) => setTo(e.target.value)} />
              </div>
              {toMinutes(to) !== null && toMinutes(from) !== null && toMinutes(to)! <= toMinutes(from)! && (
                <p className="text-xs text-gray-600">The hours run past midnight, into the next morning.</p>
              )}
              <fieldset>
                <legend className="text-sm font-medium text-gray-700">On these days</legend>
                <div className="mt-1 flex flex-wrap gap-3">
                  {DAY_BITS.map((day) => (
                    <label key={day.bit} className="flex items-center gap-1 text-sm text-gray-800">
                      <input type="checkbox" checked={(days & day.bit) !== 0} onChange={() => toggleDay(day.bit)} aria-label={day.long} />
                      {day.short}
                    </label>
                  ))}
                </div>
                <div className="mt-1 flex gap-3 text-xs">
                  <button type="button" className="text-blue-600 hover:text-blue-800" onClick={() => setDays(ALL_DAYS)}>
                    Every day
                  </button>
                  <button type="button" className="text-blue-600 hover:text-blue-800" onClick={() => setDays(WEEKDAYS)}>
                    Monday to Friday
                  </button>
                </div>
              </fieldset>
            </div>
          )}

          <p className="text-xs text-gray-600" data-testid="schedule-zone-note">
            Times are your shop&apos;s clock: {zoneLabel(zone.timeZoneId)}.{' '}
            {zone.source === 'default' || zone.source === 'browser' ? (
              <>
                No time zone is set for the shop yet, so this is {zone.source === 'browser' ? 'your computer’s' : 'UTC'}.{' '}
                <Link href="/shops" className="text-blue-600 hover:text-blue-800">
                  Set it on the shop page
                </Link>
                .
              </>
            ) : null}{' '}
            Two banners cannot be shown at the same moment, but they can share dates when their hours differ. Outside any banner&apos;s hours your shop
            shows its default board.
          </p>
          {needsReapprovalOnChange && hasSchedule && (
            <p className="text-xs text-amber-800">Changing the schedule sends this banner back for approval.</p>
          )}
          <div className="flex gap-2">
            <Button type="submit" size="sm" isLoading={busy}>
              Save schedule
            </Button>
            <Button type="button" size="sm" variant="secondary" onClick={() => setOpen(false)}>
              Cancel
            </Button>
          </div>
        </form>
      )}
    </div>
  )
}
