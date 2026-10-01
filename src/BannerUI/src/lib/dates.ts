const pad = (n: number) => String(n).padStart(2, '0')

/** Value for an <input type="datetime-local">: the moment in the visitor's own time zone, to the minute. */
export function toLocalInputValue(iso: string | null | undefined): string {
  if (!iso) return ''
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/** The moment a datetime-local value means in the visitor's time zone, as the UTC text the API expects. */
export function fromLocalInput(value: string): string | null {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

export interface WindowCheck {
  valid: boolean
  error?: string
}

/** Checks a schedule before it is sent: both ends present, end after start, end not already past. */
export function checkWindow(startLocal: string, endLocal: string, now = new Date()): WindowCheck {
  const start = fromLocalInput(startLocal)
  const end = fromLocalInput(endLocal)
  if (!start || !end) return { valid: false, error: 'Choose when the banner starts and when it ends.' }
  if (new Date(end) <= new Date(start)) return { valid: false, error: 'The end must be after the start.' }
  if (new Date(end) <= now) return { valid: false, error: 'The end is already in the past.' }
  return { valid: true }
}

const day = new Intl.DateTimeFormat(undefined, { weekday: 'short', day: 'numeric', month: 'short' })
const time = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' })

/** "Mon 3 Nov, 9:00 AM – 12:00 PM", or both dates when it runs across days. */
export function formatWindow(startIso: string | null | undefined, endIso: string | null | undefined): string {
  if (!startIso || !endIso) return 'No schedule'
  const start = new Date(startIso)
  const end = new Date(endIso)
  const sameDay = start.toDateString() === end.toDateString()
  return sameDay
    ? `${day.format(start)}, ${time.format(start)} – ${time.format(end)}`
    : `${day.format(start)}, ${time.format(start)} – ${day.format(end)}, ${time.format(end)}`
}
