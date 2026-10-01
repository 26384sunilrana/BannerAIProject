import { checkWindow, formatWindow, fromLocalInput, toLocalInputValue } from '@/lib/dates'

describe('local date helpers', () => {
  it('round-trips a moment through the datetime-local format', () => {
    const iso = new Date(2026, 10, 3, 9, 30).toISOString()   // 3 Nov 2026, 09:30 local

    expect(toLocalInputValue(iso)).toBe('2026-11-03T09:30')
    expect(fromLocalInput('2026-11-03T09:30')).toBe(iso)
  })

  it('handles empty and invalid values', () => {
    expect(toLocalInputValue(null)).toBe('')
    expect(toLocalInputValue('nonsense')).toBe('')
    expect(fromLocalInput('')).toBeNull()
    expect(fromLocalInput('not a date')).toBeNull()
  })
})

describe('checkWindow', () => {
  const now = new Date(2026, 10, 1, 8, 0)

  it('accepts a window in the future, including one that is only a few hours long', () => {
    expect(checkWindow('2026-11-03T09:00', '2026-11-03T12:00', now).valid).toBe(true)
  })

  it('needs both ends', () => {
    expect(checkWindow('', '2026-11-03T12:00', now)).toMatchObject({ valid: false, error: expect.stringMatching(/starts and when it ends/) })
  })

  it('rejects an end that is not after the start', () => {
    expect(checkWindow('2026-11-03T12:00', '2026-11-03T12:00', now).error).toMatch(/after the start/)
    expect(checkWindow('2026-11-03T12:00', '2026-11-03T09:00', now).error).toMatch(/after the start/)
  })

  it('rejects a window that is already over', () => {
    expect(checkWindow('2026-10-01T09:00', '2026-10-01T12:00', now).error).toMatch(/past/)
  })
})

describe('formatWindow', () => {
  it('shows hours only for a same-day window and both days otherwise', () => {
    const sameDay = formatWindow(new Date(2026, 10, 3, 9, 0).toISOString(), new Date(2026, 10, 3, 12, 0).toISOString())
    const spans = formatWindow(new Date(2026, 10, 3, 9, 0).toISOString(), new Date(2026, 10, 5, 12, 0).toISOString())

    expect(sameDay.match(/Nov/g)).toHaveLength(1)
    expect(spans.match(/Nov/g)).toHaveLength(2)
  })

  it('says so when there is no schedule', () => {
    expect(formatWindow(null, null)).toBe('No schedule')
  })
})
