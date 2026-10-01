/** Billing period values as the API sends and expects them. */
export const BillingPeriod = {
  Monthly: 1,
  Yearly: 2,
  Quarterly: 3,
  HalfYearly: 4,
} as const

export type BillingPeriodValue = (typeof BillingPeriod)[keyof typeof BillingPeriod]

export const PERIOD_OPTIONS: { value: BillingPeriodValue; label: string; months: number }[] = [
  { value: BillingPeriod.Monthly, label: 'Monthly', months: 1 },
  { value: BillingPeriod.Quarterly, label: 'Quarterly (3 months)', months: 3 },
  { value: BillingPeriod.HalfYearly, label: 'Half yearly (6 months)', months: 6 },
  { value: BillingPeriod.Yearly, label: 'Yearly', months: 12 },
]

export function periodLabel(period: number): string {
  return PERIOD_OPTIONS.find((p) => p.value === period)?.label ?? 'Unknown'
}

export function periodMonths(period: number): number {
  return PERIOD_OPTIONS.find((p) => p.value === period)?.months ?? 1
}

/**
 * Same rule as the API: monthly and yearly use the plan prices; quarterly and half yearly are the
 * yearly price spread over the months, so every longer period costs the same per month as yearly.
 */
export function priceFor(plan: { monthlyPrice: number; annualPrice: number }, period: number): number {
  if (period === BillingPeriod.Monthly) return plan.monthlyPrice
  if (period === BillingPeriod.Yearly) return plan.annualPrice
  return Math.round((plan.annualPrice / 12) * periodMonths(period) * 100) / 100
}

export function formatMoney(value: number): string {
  return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 2 }).format(value)
}

export const SubscriptionStatus: Record<number, string> = {
  1: 'Trial',
  2: 'Active',
  3: 'Renewal pending',
  4: 'Payment failed',
  5: 'Suspended',
  6: 'Grace period',
  7: 'Expired',
  8: 'Cancelled',
}

/** Statuses in which the shop counts as subscribed (can add logins). */
export const SUBSCRIBED_STATUSES = [1, 2, 3, 6]
