'use client'

import React, { useCallback, useEffect, useMemo, useState } from 'react'
import Link from 'next/link'
import { AppShell } from '@/components/layout/AppShell'
import { Button } from '@/components/Common'
import { useShopTimeZone } from '@/hooks/useShopTimeZone'
import { bannerListService, ScheduleCalendar } from '@/api/workflowService'
import { getErrorMessage } from '@/api/client'
import { addDays, layoutWeek, mondayOf, weekDates, weekRange } from '@/lib/calendarLayout'
import { utcToWallTime, zoneLabel } from '@/lib/timeZones'
import { Roles } from '@/lib/session'

const HOUR_HEIGHT = 28 // pixels per hour
const COLOURS = ['#2563eb', '#16a34a', '#d97706', '#9333ea', '#dc2626', '#0891b2', '#be185d', '#4d7c0f']

const colourOf = (id: string) => COLOURS[Array.from(id).reduce((sum, c) => sum + c.charCodeAt(0), 0) % COLOURS.length]

export default function CalendarPage() {
  return (
    <AppShell roles={[Roles.ShopOwner, Roles.SalesExecutive]}>
      <Calendar />
    </AppShell>
  )
}

function Calendar() {
  const zone = useShopTimeZone()
  const [weekStart, setWeekStart] = useState<string | null>(null)
  const [data, setData] = useState<ScheduleCalendar | null>(null)
  const [error, setError] = useState<string | null>(null)

  // start on the week that holds today, as the shop's clock sees it
  useEffect(() => {
    if (zone.loaded && weekStart === null) setWeekStart(mondayOf(utcToWallTime(new Date().toISOString(), zone.timeZoneId).slice(0, 10)))
  }, [zone.loaded, zone.timeZoneId, weekStart])

  const load = useCallback(async () => {
    if (!weekStart) return
    try {
      const range = weekRange(weekStart, zone.timeZoneId)
      setData(await bannerListService.getCalendar(range.from, range.to))
      setError(null)
    } catch (err) {
      setError(getErrorMessage(err, 'Could not load the calendar.'))
    }
  }, [weekStart, zone.timeZoneId])

  useEffect(() => {
    setData(null)
    load()
  }, [load])

  const dates = useMemo(() => (weekStart ? weekDates(weekStart) : []), [weekStart])
  const columns = useMemo(
    () => (data && dates.length ? layoutWeek(data.entries, dates, zone.timeZoneId) : {}),
    [data, dates, zone.timeZoneId]
  )
  const names = useMemo(() => Array.from(new Map((data?.entries ?? []).map((e) => [e.bannerId, e.name])).entries()), [data])
  const today = utcToWallTime(new Date().toISOString(), zone.timeZoneId).slice(0, 10)

  const heading = dates.length
    ? `${new Date(`${dates[0]}T12:00:00Z`).toLocaleDateString(undefined, { day: 'numeric', month: 'short', timeZone: 'UTC' })} – ${new Date(`${dates[6]}T12:00:00Z`).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' })}`
    : ''

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">Schedule calendar</h1>
          <p className="mt-1 text-gray-600">
            When each banner is shown, on your shop&apos;s clock ({zoneLabel(zone.timeZoneId)}). Between banners your shop shows its default board.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" disabled={!weekStart} onClick={() => weekStart && setWeekStart(addDays(weekStart, -7))}>
            ← Previous week
          </Button>
          <Button variant="secondary" size="sm" onClick={() => setWeekStart(mondayOf(today))}>
            This week
          </Button>
          <Button variant="secondary" size="sm" disabled={!weekStart} onClick={() => weekStart && setWeekStart(addDays(weekStart, 7))}>
            Next week →
          </Button>
        </div>
      </header>

      <p className="text-lg font-medium text-gray-900" data-testid="calendar-range">
        {heading}
      </p>

      {error && (
        <p role="alert" className="text-red-700">
          {error}
        </p>
      )}
      {!data && !error && <p className="text-gray-500">Loading the calendar…</p>}

      {data && (
        <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white">
          <div className="grid min-w-[44rem]" style={{ gridTemplateColumns: '3.5rem repeat(7, minmax(0, 1fr))' }}>
            <div />
            {dates.map((date) => (
              <div key={date} className={`border-l border-gray-200 px-2 py-2 text-center text-sm ${date === today ? 'bg-blue-50 font-semibold text-blue-800' : 'text-gray-700'}`}>
                {new Date(`${date}T12:00:00Z`).toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', timeZone: 'UTC' })}
              </div>
            ))}

            <div className="relative" style={{ height: HOUR_HEIGHT * 24 }}>
              {Array.from({ length: 24 }, (_, hour) => (
                <span key={hour} className="absolute right-1 -translate-y-1/2 text-[10px] text-gray-500" style={{ top: hour * HOUR_HEIGHT }}>
                  {hour === 0 ? '' : `${hour % 12 === 0 ? 12 : hour % 12}${hour < 12 ? 'a' : 'p'}`}
                </span>
              ))}
            </div>
            {dates.map((date) => (
              <div
                key={date}
                data-testid={`day-${date}`}
                className="relative border-l border-gray-200"
                style={{
                  height: HOUR_HEIGHT * 24,
                  backgroundImage: 'linear-gradient(to bottom, #f3f4f6 1px, transparent 1px)',
                  backgroundSize: `100% ${HOUR_HEIGHT}px`,
                }}
              >
                {(columns[date] ?? []).map((block, index) => (
                  <Link
                    key={`${block.bannerId}-${index}`}
                    href="/banners"
                    data-testid={`block-${block.name}`}
                    title={`${block.name}${block.published ? '' : ' (not approved yet, so it will not be shown)'}`}
                    className="absolute left-0.5 right-0.5 overflow-hidden rounded px-1 text-[11px] leading-tight text-white"
                    style={{
                      top: (block.top / 60) * HOUR_HEIGHT,
                      height: Math.max((block.length / 60) * HOUR_HEIGHT, 14),
                      background: colourOf(block.bannerId),
                      opacity: block.published ? 1 : 0.55,
                      border: block.published ? 'none' : '1px dashed #111827',
                      borderTopLeftRadius: block.continuesBefore ? 0 : undefined,
                      borderBottomLeftRadius: block.continuesAfter ? 0 : undefined,
                    }}
                  >
                    {block.name}
                  </Link>
                ))}
              </div>
            ))}
          </div>
        </div>
      )}

      {data && data.entries.length === 0 && (
        <p className="rounded-xl border border-dashed border-gray-300 p-6 text-center text-gray-500" data-testid="calendar-empty">
          Nothing is scheduled this week. Set a schedule on the{' '}
          <Link href="/banners" className="text-blue-600 hover:text-blue-800">
            banners page
          </Link>
          .
        </p>
      )}

      {names.length > 0 && (
        <ul className="flex flex-wrap gap-4 text-sm text-gray-700" aria-label="Banners this week">
          {names.map(([id, name]) => (
            <li key={id} className="flex items-center gap-2">
              <span className="inline-block h-3 w-3 rounded" style={{ background: colourOf(id) }} />
              {name}
            </li>
          ))}
          <li className="flex items-center gap-2 text-gray-500">
            <span className="inline-block h-3 w-3 rounded border border-dashed border-gray-900 opacity-60" />
            faded: not approved yet
          </li>
        </ul>
      )}
    </div>
  )
}

