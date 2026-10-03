import {
  ALL_DAYS,
  WEEKDAYS,
  daysLabel,
  describeDaily,
  formatInZone,
  fromMinutes,
  supportedTimeZones,
  toMinutes,
  utcToWallTime,
  wallTimeToUtcIso,
  zoneDate,
  zoneLabel,
  zoneOffsetMs,
} from '@/lib/timeZones'

const hours = (n: number) => n * 3600_000

describe('zoneOffsetMs', () => {
  it('knows how far ahead a zone is', () => {
    const winter = Date.UTC(2030, 0, 15, 12)
    const summer = Date.UTC(2030, 6, 15, 12)

    expect(zoneOffsetMs(winter, 'Asia/Kolkata')).toBe(hours(5.5))
    expect(zoneOffsetMs(winter, 'UTC')).toBe(0)
    expect(zoneOffsetMs(winter, 'Europe/London')).toBe(0)
    expect(zoneOffsetMs(summer, 'Europe/London')).toBe(hours(1))
    expect(zoneOffsetMs(winter, 'America/New_York')).toBe(-hours(5))
    expect(zoneOffsetMs(summer, 'America/New_York')).toBe(-hours(4))
  })
})

describe('wallTimeToUtcIso and utcToWallTime', () => {
  it('turns shop time into the UTC moment it means', () => {
    expect(wallTimeToUtcIso('2030-01-07T09:00', 'Asia/Kolkata')).toBe('2030-01-07T03:30:00.000Z')
    expect(wallTimeToUtcIso('2030-01-07T09:00', 'America/New_York')).toBe('2030-01-07T14:00:00.000Z')
    expect(wallTimeToUtcIso('2030-07-07T09:00', 'America/New_York')).toBe('2030-07-07T13:00:00.000Z')
    expect(wallTimeToUtcIso('2030-01-07T09:00', 'UTC')).toBe('2030-01-07T09:00:00.000Z')
  })

  it('goes back the same way, including across midnight', () => {
    expect(utcToWallTime('2030-01-07T03:30:00.000Z', 'Asia/Kolkata')).toBe('2030-01-07T09:00')
    expect(utcToWallTime('2030-01-07T20:00:00.000Z', 'Asia/Kolkata')).toBe('2030-01-08T01:30')
    expect(utcToWallTime('2030-01-07T03:00:00.000Z', 'America/New_York')).toBe('2030-01-06T22:00')
  })

  it('round-trips', () => {
    for (const zone of ['Asia/Kolkata', 'Europe/London', 'America/New_York', 'Australia/Sydney', 'UTC']) {
      const iso = wallTimeToUtcIso('2030-03-10T08:15', zone)!
      expect(utcToWallTime(iso, zone)).toBe('2030-03-10T08:15')
    }
  })

  it('handles the day the clocks go forward: a time that does not exist moves on rather than failing', () => {
    // London 31 March 2030, 01:30 does not exist (01:00 jumps to 02:00)
    const iso = wallTimeToUtcIso('2030-03-31T01:30', 'Europe/London')!

    expect(new Date(iso).getUTCHours()).toBeGreaterThanOrEqual(0)
    expect(utcToWallTime(iso, 'Europe/London')).toMatch(/^2030-03-31T0[12]:30$/)
  })

  it('is empty or null for what is not a date', () => {
    expect(wallTimeToUtcIso('', 'UTC')).toBeNull()
    expect(wallTimeToUtcIso('soon', 'UTC')).toBeNull()
    expect(utcToWallTime(null, 'UTC')).toBe('')
    expect(utcToWallTime('nonsense', 'UTC')).toBe('')
  })
})

describe('formatting', () => {
  it('shows a moment in the zone, not in the browser zone', () => {
    const text = formatInZone('2030-01-07T20:00:00Z', 'Asia/Kolkata', false)

    expect(text).toMatch(/1:30/)
    expect(text).toMatch(/AM|am/)
  })

  it('has no text for no moment', () => {
    expect(formatInZone(null, 'UTC')).toBe('')
    expect(formatInZone('junk', 'UTC')).toBe('')
  })

  it('names the day in the zone', () => {
    expect(zoneDate('2030-01-07T20:00:00Z', 'Asia/Kolkata')).toBe('2030-01-08')
    expect(zoneDate('2030-01-07T20:00:00Z', 'UTC')).toBe('2030-01-07')
  })

  it('labels a zone with its offset', () => {
    expect(zoneLabel('Asia/Kolkata', Date.UTC(2030, 0, 1))).toBe('Asia/Kolkata (UTC+05:30)')
    expect(zoneLabel('America/New_York', Date.UTC(2030, 0, 1))).toBe('America/New_York (UTC-05:00)')
    expect(zoneLabel('Not/AZone')).toBe('Not/AZone')
  })

  it('offers the real zones, with UTC first-class', () => {
    const zones = supportedTimeZones()

    expect(zones).toContain('Asia/Kolkata')
    expect(zones).toContain('UTC')
  })
})

describe('daily hours', () => {
  it('converts between "HH:mm" and minutes', () => {
    expect(toMinutes('09:30')).toBe(570)
    expect(toMinutes('9:05')).toBe(545)
    expect(toMinutes('23:59')).toBe(1439)
    expect(toMinutes('24:00')).toBeNull()
    expect(toMinutes('12:60')).toBeNull()
    expect(toMinutes('noon')).toBeNull()
    expect(fromMinutes(570)).toBe('09:30')
    expect(fromMinutes(0)).toBe('00:00')
  })

  it('names the days', () => {
    expect(daysLabel(ALL_DAYS)).toBe('every day')
    expect(daysLabel(WEEKDAYS)).toBe('Monday to Friday')
    expect(daysLabel(64 | 1)).toBe('weekends')
    expect(daysLabel(2 | 8 | 32)).toBe('Mon, Wed, Fri')
    expect(daysLabel(0)).toBe('no day')
  })

  it('describes hours in words', () => {
    expect(describeDaily(540, 720, ALL_DAYS)).toBe('9:00 AM – 12:00 PM, every day')
    expect(describeDaily(720, 780, WEEKDAYS)).toBe('12:00 PM – 1:00 PM, Monday to Friday')
    expect(describeDaily(1320, 120, 2)).toBe('10:00 PM – 2:00 AM (until the next morning), Mon')
    expect(describeDaily(0, 60, null)).toBe('12:00 AM – 1:00 AM, every day')
    expect(describeDaily(null, null, null)).toBe('')
  })
})
