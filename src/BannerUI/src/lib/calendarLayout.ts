import { utcToWallTime, wallTimeToUtcIso } from './timeZones'

/** One banner stretch drawn in one day column: minutes after midnight on the shop's clock. */
export interface DayBlock {
  bannerId: string
  name: string
  published: boolean
  /** Minutes after the day starts, and how many minutes it lasts (the day is 1440). */
  top: number
  length: number
  /** The stretch carries on from the day before, or on into the next day. */
  continuesBefore: boolean
  continuesAfter: boolean
}

export interface CalendarInput {
  bannerId: string
  name: string
  startUtc: string
  endUtc: string
  published: boolean
}

const MINUTES_PER_DAY = 1440

/** "2030-01-07" plus a number of days. */
export function addDays(date: string, days: number): string {
  const [y, m, d] = date.split('-').map(Number)
  const moved = new Date(Date.UTC(y, m - 1, d + days))
  return `${moved.getUTCFullYear()}-${String(moved.getUTCMonth() + 1).padStart(2, '0')}-${String(moved.getUTCDate()).padStart(2, '0')}`
}

/** The Monday of the week that holds this date ("2030-01-09" gives "2030-01-07"). */
export function mondayOf(date: string): string {
  const [y, m, d] = date.split('-').map(Number)
  const weekday = new Date(Date.UTC(y, m - 1, d)).getUTCDay() // 0 = Sunday
  return addDays(date, -((weekday + 6) % 7))
}

/** Seven consecutive dates starting at the Monday. */
export function weekDates(monday: string): string[] {
  return Array.from({ length: 7 }, (_, i) => addDays(monday, i))
}

/** The moments (UTC, ISO text) a week of the shop's calendar starts and ends, for asking the API. */
export function weekRange(monday: string, zone: string): { from: string; to: string } {
  return {
    from: wallTimeToUtcIso(`${monday}T00:00`, zone)!,
    to: wallTimeToUtcIso(`${addDays(monday, 7)}T00:00`, zone)!,
  }
}

const minutesOf = (wall: string) => Number(wall.slice(11, 13)) * 60 + Number(wall.slice(14, 16))

/** Cuts each stretch into the day columns it touches. Days are the dates of the week, in order. */
export function layoutWeek(entries: CalendarInput[], dates: string[], zone: string): Record<string, DayBlock[]> {
  const columns: Record<string, DayBlock[]> = Object.fromEntries(dates.map((d) => [d, [] as DayBlock[]]))

  for (const entry of entries) {
    const start = utcToWallTime(entry.startUtc, zone)
    const end = utcToWallTime(entry.endUtc, zone)
    if (!start || !end) continue

    for (const date of dates) {
      const dayStart = `${date}T00:00`
      const dayEnd = `${addDays(date, 1)}T00:00`
      const from = start > dayStart ? start : dayStart
      const to = end < dayEnd ? end : dayEnd
      if (to <= from) continue

      const top = from === dayStart ? 0 : minutesOf(from)
      const bottom = to === dayEnd ? MINUTES_PER_DAY : minutesOf(to)
      columns[date].push({
        bannerId: entry.bannerId,
        name: entry.name,
        published: entry.published,
        top,
        length: Math.max(bottom - top, 1),
        continuesBefore: start < dayStart,
        continuesAfter: end > dayEnd,
      })
    }
  }

  for (const date of dates) columns[date].sort((a, b) => a.top - b.top)
  return columns
}
