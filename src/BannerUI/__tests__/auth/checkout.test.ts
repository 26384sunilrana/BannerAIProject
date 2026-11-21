jest.mock('@/api/client', () => ({
  apiClient: { get: jest.fn(), post: jest.fn(), put: jest.fn(), delete: jest.fn() },
}))

import { apiClient } from '@/api/client'
import { paymentService } from '@/api/paymentService'
import { providerService } from '@/api/providerService'
import { CheckoutCancelled, getCheckoutDriver } from '@/lib/checkout'
import { razorpayDriver } from '@/lib/checkout/razorpay'

const api = apiClient as jest.Mocked<typeof apiClient>

beforeEach(() => {
  jest.clearAllMocks()
  document.head.innerHTML = ''
  document.body.innerHTML = ''
  delete (window as { Razorpay?: unknown }).Razorpay
})

describe('Checkout drivers', () => {
  it('finds the driver by the provider name the API reports, in any letter case', () => {
    expect(getCheckoutDriver('Razorpay')).toBe(razorpayDriver)
    expect(getCheckoutDriver('razorpay')).toBe(razorpayDriver)
  })

  it('has none for an unknown or missing provider', () => {
    expect(getCheckoutDriver('Stripe')).toBeNull()
    expect(getCheckoutDriver(null)).toBeNull()
    expect(getCheckoutDriver(undefined)).toBeNull()
  })
})

describe('Razorpay driver', () => {
  const session = { provider: 'Razorpay', orderId: 'order_1', amount: 499, amountMinor: 49900, currency: 'INR', publicKeyId: 'rzp_test_key' }

  function installForm(behaviour: (options: Record<string, unknown>) => void) {
    const constructed: Record<string, unknown>[] = []
    ;(window as { Razorpay?: unknown }).Razorpay = class {
      constructor(options: Record<string, unknown>) {
        constructed.push(options)
        this.options = options
      }
      options: Record<string, unknown>
      on(): void {}
      open(): void {
        behaviour(this.options)
      }
    }
    return constructed
  }

  it('opens the provider form with the order and returns what the form gives back', async () => {
    const constructed = installForm((options) =>
      (options.handler as (r: unknown) => void)({ razorpay_order_id: 'order_1', razorpay_payment_id: 'pay_1', razorpay_signature: 'sig' })
    )

    const result = await razorpayDriver.open(session)

    expect(result).toEqual({ orderId: 'order_1', paymentId: 'pay_1', signature: 'sig' })
    expect(constructed[0]).toMatchObject({ key: 'rzp_test_key', amount: 49900, currency: 'INR', order_id: 'order_1' })
  })

  it('passes no name, e-mail or phone of the payer to the form', async () => {
    const constructed = installForm((options) =>
      (options.handler as (r: unknown) => void)({ razorpay_order_id: 'o', razorpay_payment_id: 'p', razorpay_signature: 's' })
    )
    await razorpayDriver.open(session)
    expect(constructed[0]).not.toHaveProperty('prefill')
    expect(constructed[0]).not.toHaveProperty('customer_id')
  })

  it('rejects with CheckoutCancelled when the form is closed without paying', async () => {
    installForm((options) => (options.modal as { ondismiss: () => void }).ondismiss())
    await expect(razorpayDriver.open(session)).rejects.toBeInstanceOf(CheckoutCancelled)
  })

  it('loads the provider script only when needed, and only once', async () => {
    const promise = razorpayDriver.open(session)
    const script = document.querySelector('script[src="https://checkout.razorpay.com/v1/checkout.js"]') as HTMLScriptElement
    expect(script).not.toBeNull()

    installForm((options) =>
      (options.handler as (r: unknown) => void)({ razorpay_order_id: 'o', razorpay_payment_id: 'p', razorpay_signature: 's' })
    )
    script.onload?.(new Event('load'))
    await promise

    await razorpayDriver.open(session)
    expect(document.querySelectorAll('script[src*="checkout.razorpay.com"]').length).toBe(1)
  })

  it('reports a script that cannot be loaded', async () => {
    const promise = razorpayDriver.open(session)
    const script = document.querySelector('script') as HTMLScriptElement
    script.onerror?.(new Event('error'))
    await expect(promise).rejects.toThrow(/could not be loaded/i)
  })
})

describe('Payment and provider calls', () => {
  it('asks the server for the payment settings, starts a checkout and confirms it', async () => {
    api.get.mockResolvedValue({ enabled: false })
    api.post.mockResolvedValue({})

    await paymentService.getConfig()
    await paymentService.startCheckout('sub-1')
    await paymentService.confirm('sub-1', { orderId: 'o', paymentId: 'p', signature: 's' })

    expect(api.get).toHaveBeenCalledWith('/payments/config')
    expect(api.post).toHaveBeenCalledWith('/subscriptions/sub-1/checkout', {})
    expect(api.post).toHaveBeenCalledWith('/subscriptions/sub-1/checkout/confirm', { orderId: 'o', paymentId: 'p', signature: 's' })
  })

  it('uses the admin provider endpoints, with the test address left empty when none is given', async () => {
    api.get.mockResolvedValue([])
    api.post.mockResolvedValue({ sent: true })

    await providerService.getStatus()
    await providerService.sendTestEmail()
    await providerService.sendTestEmail(' me@example.com ')
    await providerService.sendTestSms(' 9876543210 ')

    expect(api.get).toHaveBeenCalledWith('/admin/providers')
    expect(api.post).toHaveBeenCalledWith('/admin/providers/test-email', { to: null })
    expect(api.post).toHaveBeenCalledWith('/admin/providers/test-email', { to: 'me@example.com' })
    expect(api.post).toHaveBeenCalledWith('/admin/providers/test-sms', { to: '9876543210' })
  })
})
