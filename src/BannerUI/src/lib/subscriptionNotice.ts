import { ShopSubscription } from '@/api/subscriptionService'

export type NoticeTone = 'info' | 'warning' | 'danger'

export interface SubscriptionNotice {
  tone: NoticeTone
  title: string
  text: string
  /** Whether to offer a Renew now button. */
  canRenew: boolean
}

const MS_PER_DAY = 24 * 60 * 60 * 1000
const dateOnly = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()

/** Whole calendar days from `from` to `to`, in the visitor's time zone (negative when `to` is earlier). */
export function daysBetween(from: Date, to: Date): number {
  return Math.round((dateOnly(to) - dateOnly(from)) / MS_PER_DAY)
}

const fmt = (iso: string) => new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
const plural = (n: number, word: string) => `${n} ${word}${n === 1 ? '' : 's'}`

/**
 * What the owner should be told about their subscription right now, or null when nothing needs saying.
 * Mirrors the server's rules: reminders from 7 days before an ending that will not renew itself,
 * a grace week after an unpaid renewal date, then logins are switched off.
 */
export function subscriptionNotice(subscription: ShopSubscription | null, now = new Date()): SubscriptionNotice | null {
  if (!subscription) return null

  const status = subscription.status

  if (status === 6) {
    const endsAt = subscription.graceEndsAt ?? subscription.renewalDate
    const left = Math.max(0, daysBetween(now, new Date(endsAt)))
    return {
      tone: 'danger',
      title: 'Your plan has ended',
      text: `Your shop is showing only its default banner until you renew. Logins are switched off in ${plural(left, 'day')} (on ${fmt(endsAt)}).`,
      canRenew: true,
    }
  }

  if (status === 7 || status === 5) {
    return {
      tone: 'danger',
      title: 'Logins are switched off',
      text: 'Your plan was not renewed in time. Contact support to reactivate your account.',
      canRenew: false,
    }
  }

  if (status === 4) {
    return {
      tone: 'danger',
      title: 'Your last payment failed',
      text: 'Renew now to keep your banners playing.',
      canRenew: true,
    }
  }

  const daysLeft = daysBetween(now, new Date(subscription.renewalDate))
  if (!subscription.autoRenew && daysLeft <= 7 && daysLeft >= 0) {
    return {
      tone: 'warning',
      title: daysLeft === 0 ? 'Your plan ends today' : `Your plan ends in ${plural(daysLeft, 'day')}`,
      text: `It will not renew automatically (${fmt(subscription.renewalDate)}). Renew now so your banners keep playing.`,
      canRenew: true,
    }
  }

  return null
}
