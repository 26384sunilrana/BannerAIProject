import { daysBetween, subscriptionNotice } from '@/lib/subscriptionNotice'
import { ShopSubscription } from '@/api/subscriptionService'
import { loadDisplayCache, saveDisplayCache } from '@/lib/displayCache'

const now = new Date(2026, 10, 10, 15, 0)
const daysFromNow = (n: number) => new Date(2026, 10, 10 + n, 9, 0).toISOString()

const sub = (overrides: Partial<ShopSubscription>): ShopSubscription => ({
  id: 's1', shopId: 'shop', planId: 'p', planName: 'Silver', status: 2, billingPeriod: 2, currentPrice: 800,
  startDate: daysFromNow(-300), renewalDate: daysFromNow(60), autoRenew: true,
  ...overrides,
})

describe('daysBetween', () => {
  it('counts calendar days whatever the time of day', () => {
    expect(daysBetween(new Date(2026, 10, 10, 23, 59), new Date(2026, 10, 11, 0, 1))).toBe(1)
    expect(daysBetween(new Date(2026, 10, 10, 0, 1), new Date(2026, 10, 10, 23, 59))).toBe(0)
    expect(daysBetween(new Date(2026, 10, 10), new Date(2026, 10, 3))).toBe(-7)
  })
})

describe('subscriptionNotice', () => {
  it('says nothing when there is no subscription or nothing to worry about', () => {
    expect(subscriptionNotice(null, now)).toBeNull()
    expect(subscriptionNotice(sub({}), now)).toBeNull()
    expect(subscriptionNotice(sub({ autoRenew: true, renewalDate: daysFromNow(2) }), now)).toBeNull() // renews itself
    expect(subscriptionNotice(sub({ autoRenew: false, renewalDate: daysFromNow(20) }), now)).toBeNull() // far away
  })

  it('warns from seven days before an ending that will not renew itself', () => {
    const week = subscriptionNotice(sub({ autoRenew: false, renewalDate: daysFromNow(7) }), now)
    const tomorrow = subscriptionNotice(sub({ autoRenew: false, renewalDate: daysFromNow(1) }), now)
    const today = subscriptionNotice(sub({ autoRenew: false, renewalDate: daysFromNow(0) }), now)

    expect(week).toMatchObject({ tone: 'warning', title: 'Your plan ends in 7 days', canRenew: true })
    expect(tomorrow?.title).toBe('Your plan ends in 1 day')
    expect(today?.title).toBe('Your plan ends today')
  })

  it('explains the grace week: default banner only, and when logins stop', () => {
    const notice = subscriptionNotice(sub({ status: 6, renewalDate: daysFromNow(-2), graceEndsAt: daysFromNow(5) }), now)

    expect(notice).toMatchObject({ tone: 'danger', title: 'Your plan has ended', canRenew: true })
    expect(notice?.text).toMatch(/default banner/)
    expect(notice?.text).toMatch(/in 5 days/)
  })

  it('says logins are off once expired, without offering a renewal the owner cannot make', () => {
    for (const status of [5, 7]) {
      expect(subscriptionNotice(sub({ status }), now)).toMatchObject({ tone: 'danger', title: 'Logins are switched off', canRenew: false })
    }
  })

  it('asks the owner to renew after a failed payment', () => {
    expect(subscriptionNotice(sub({ status: 4 }), now)).toMatchObject({ tone: 'danger', canRenew: true })
  })
})

describe('displayCache', () => {
  beforeEach(() => localStorage.clear())

  it('keeps the shop name for the default board', () => {
    expect(loadDisplayCache()).toBeNull()

    saveDisplayCache({ shopId: 's1', shopName: 'Olive Mart' })

    expect(loadDisplayCache()).toMatchObject({ shopId: 's1', shopName: 'Olive Mart' })
  })

  it('ignores damaged data', () => {
    localStorage.setItem('display_cache', '{not json')
    expect(loadDisplayCache()).toBeNull()
    localStorage.setItem('display_cache', JSON.stringify({ shopName: 5 }))
    expect(loadDisplayCache()).toBeNull()
  })
})
