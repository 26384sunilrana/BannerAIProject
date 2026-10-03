/**
 * Times in the shop's time zone. A banner schedule is about the shop's clock, not the clock of whoever is typing
 * (an owner travelling, or an administrator in another country, still means "9 am in the shop"). The browser only
 * knows its own zone for form fields, so these functions convert with Intl, no library needed.
 */

export const BROWSER_ZONE = (() => {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'
  } catch {
    return 'UTC'
  }
})()

const pad = (n: number) => String(n).padStart(2, '0')

/** How far ahead of UTC the zone is at this moment, in milliseconds (India: +5.5 hours). */
export function zoneOffsetMs(utcMs: number, zone: string): number {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: zone,
    hourCycle: 'h23',
    year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit',
  }).formatToParts(new Date(utcMs))
  const get = (type: string) => Number(parts.find((p) => p.type === type)?.value)
  const asIfUtc = Date.UTC(get('year'), get('month') - 1, get('day'), get('hour'), get('minute'), get('second'))
  return asIfUtc - Math.floor(utcMs / 1000) * 1000
}

/** "2030-01-07T09:00" typed as wall-clock time in the zone, as the UTC moment it means (ISO text), or null when it is not a date. */
export function wallTimeToUtcIso(local: string, zone: string): string | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(local)
  if (!match) return null
  const [, y, mo, d, h, mi] = match.map(Number) as unknown as number[]
  const guess = Date.UTC(y, mo - 1, d, h, mi)
  if (Number.isNaN(guess)) return null

  // the offset depends on the moment, so settle it in two passes (matters around daylight saving changes)
  const first = guess - zoneOffsetMs(guess, zone)
  const second = guess - zoneOffsetMs(first, zone)
  return new Date(second).toISOString()
}

/** A UTC moment as wall-clock time in the zone, in the form of a datetime-local field ("2030-01-07T09:00"). */
export function utcToWallTime(iso: string | null | undefined, zone: string): string {
  if (!iso) return ''
  const ms = new Date(iso).getTime()
  if (Number.isNaN(ms)) return ''
  const shifted = new Date(ms + zoneOffsetMs(ms, zone))
  return `${shifted.getUTCFullYear()}-${pad(shifted.getUTCMonth() + 1)}-${pad(shifted.getUTCDate())}T${pad(shifted.getUTCHours())}:${pad(shifted.getUTCMinutes())}`
}

/** "Mon 7 Jan, 9:00 AM" in the zone. */
export function formatInZone(iso: string | null | undefined, zone: string, withDate = true): string {
  if (!iso) return ''
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  return new Intl.DateTimeFormat(undefined, {
    timeZone: zone,
    ...(withDate ? { weekday: 'short', day: 'numeric', month: 'short' } : {}),
    hour: 'numeric',
    minute: '2-digit',
  }).format(date)
}

/** The date part ("2030-01-07") of a UTC moment in the zone, for grouping by day. */
export function zoneDate(iso: string, zone: string): string {
  return utcToWallTime(iso, zone).slice(0, 10)
}

/** Browsers list some zones under their old names (Asia/Calcutta); people look for the current one (Asia/Kolkata). */
const CURRENT_NAMES: Record<string, string> = {
  'Asia/Calcutta': 'Asia/Kolkata',
  'Asia/Katmandu': 'Asia/Kathmandu',
  'Asia/Saigon': 'Asia/Ho_Chi_Minh',
  'Asia/Rangoon': 'Asia/Yangon',
  'Europe/Kiev': 'Europe/Kyiv',
  'America/Buenos_Aires': 'America/Argentina/Buenos_Aires',
}

/** The zones this browser knows, by their current IANA names. */
export function supportedTimeZones(): string[] {
  try {
    const list = (Intl as unknown as { supportedValuesOf?: (key: string) => string[] }).supportedValuesOf?.('timeZone')
    if (list && list.length) {
      const names = new Set(list.map((zone) => CURRENT_NAMES[zone] ?? zone))
      names.add('UTC')
      return ['UTC', ...Array.from(names).filter((zone) => zone !== 'UTC').sort()]
    }
  } catch {
    // an older browser: fall through to the short list
  }
  return ['UTC', 'Asia/Kolkata', 'Asia/Dubai', 'Asia/Singapore', 'Europe/London', 'Europe/Paris', 'America/New_York', 'America/Chicago', 'America/Los_Angeles', 'Australia/Sydney']
}

/** A readable label: "Asia/Kolkata (UTC+05:30)". */
export function zoneLabel(zone: string, at = Date.now()): string {
  try {
    const offset = zoneOffsetMs(at, zone) / 60000
    const sign = offset < 0 ? '-' : '+'
    const abs = Math.abs(offset)
    return `${zone} (UTC${sign}${pad(Math.floor(abs / 60))}:${pad(abs % 60)})`
  } catch {
    return zone
  }
}

// ----- daily hours

/** "09:30" to minutes after midnight; null when it is not a time. */
export function toMinutes(time: string): number | null {
  const match = /^(\d{1,2}):(\d{2})$/.exec(time)
  if (!match) return null
  const h = Number(match[1])
  const m = Number(match[2])
  return h > 23 || m > 59 ? null : h * 60 + m
}

/** Minutes after midnight to "09:30" (the value of an <input type="time">). */
export function fromMinutes(minutes: number): string {
  return `${pad(Math.floor(minutes / 60))}:${pad(minutes % 60)}`
}

/** One bit per weekday, as the API counts them: Sunday 1, Monday 2 ... Saturday 64. */
export const DAY_BITS = [
  { bit: 2, short: 'Mon', long: 'Monday' },
  { bit: 4, short: 'Tue', long: 'Tuesday' },
  { bit: 8, short: 'Wed', long: 'Wednesday' },
  { bit: 16, short: 'Thu', long: 'Thursday' },
  { bit: 32, short: 'Fri', long: 'Friday' },
  { bit: 64, short: 'Sat', long: 'Saturday' },
  { bit: 1, short: 'Sun', long: 'Sunday' },
] as const

export const ALL_DAYS = 127
export const WEEKDAYS = 2 | 4 | 8 | 16 | 32

export function daysLabel(mask: number): string {
  if ((mask & ALL_DAYS) === ALL_DAYS) return 'every day'
  if ((mask & ALL_DAYS) === WEEKDAYS) return 'Monday to Friday'
  if ((mask & ALL_DAYS) === (64 | 1)) return 'weekends'
  return DAY_BITS.filter((d) => mask & d.bit).map((d) => d.short).join(', ') || 'no day'
}

function clock(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  const suffix = h < 12 ? 'AM' : 'PM'
  return `${h % 12 === 0 ? 12 : h % 12}:${pad(m)} ${suffix}`
}

/** "9:00 AM – 12:00 PM, Monday to Friday" */
export function describeDaily(start: number | null | undefined, end: number | null | undefined, days: number | null | undefined): string {
  if (start == null || end == null) return ''
  const overnight = end <= start ? ' (until the next morning)' : ''
  return `${clock(start)} – ${clock(end)}${overnight}, ${daysLabel(days ?? ALL_DAYS)}`
}
