import type { CheckoutResult, CheckoutSession } from '@/api/paymentService'
import { CheckoutCancelled, type CheckoutDriver } from './types'

const SCRIPT_URL = 'https://checkout.razorpay.com/v1/checkout.js'

interface RazorpayResponse {
  razorpay_order_id: string
  razorpay_payment_id: string
  razorpay_signature: string
}

interface RazorpayOptions {
  key: string
  amount: number
  currency: string
  order_id: string
  name: string
  handler: (response: RazorpayResponse) => void
  modal: { ondismiss: () => void }
}

type RazorpayConstructor = new (options: RazorpayOptions) => { open: () => void; on: (event: string, callback: () => void) => void }

declare global {
  interface Window {
    Razorpay?: RazorpayConstructor
  }
}

/**
 * The script comes from Razorpay and is loaded only when Razorpay is the provider and someone pays. If a Content-Security-Policy is
 * added later (backlog), it must allow checkout.razorpay.com for scripts and frames.
 */
function loadScript(): Promise<RazorpayConstructor> {
  if (window.Razorpay) return Promise.resolve(window.Razorpay)

  return new Promise((resolve, reject) => {
    const script = document.createElement('script')
    script.src = SCRIPT_URL
    script.async = true
    script.onload = () => (window.Razorpay ? resolve(window.Razorpay) : reject(new Error('The payment form did not load')))
    script.onerror = () => reject(new Error('The payment form could not be loaded. Check your connection and try again.'))
    document.body.appendChild(script)
  })
}

export const razorpayDriver: CheckoutDriver = {
  provider: 'Razorpay',

  async open(session: CheckoutSession): Promise<CheckoutResult> {
    const Razorpay = await loadScript()

    return new Promise<CheckoutResult>((resolve, reject) => {
      // No name, e-mail or phone is passed to the form: the provider asks the payer itself
      const form = new Razorpay({
        key: session.publicKeyId,
        amount: session.amountMinor,
        currency: session.currency,
        order_id: session.orderId,
        name: 'Banner AI',
        handler: (response) =>
          resolve({
            orderId: response.razorpay_order_id,
            paymentId: response.razorpay_payment_id,
            signature: response.razorpay_signature,
          }),
        modal: { ondismiss: () => reject(new CheckoutCancelled()) },
      })
      form.open()
    })
  },
}
