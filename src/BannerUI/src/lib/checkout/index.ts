import { razorpayDriver } from './razorpay'
import { CheckoutCancelled, type CheckoutDriver } from './types'

export { CheckoutCancelled }
export type { CheckoutDriver }

const drivers: Record<string, CheckoutDriver> = {
  [razorpayDriver.provider.toLowerCase()]: razorpayDriver,
}

/** The driver for the provider the API reports, or null when this web app has none for it. */
export function getCheckoutDriver(provider: string | null | undefined): CheckoutDriver | null {
  return provider ? drivers[provider.toLowerCase()] ?? null : null
}
