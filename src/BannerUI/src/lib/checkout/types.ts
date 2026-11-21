import type { CheckoutResult, CheckoutSession } from '@/api/paymentService'

/**
 * One checkout driver per payment provider (ADR-004): it opens the provider's own payment form for an order the server made and
 * resolves with what the form returns. A new provider is one more driver; nothing else in the pages changes.
 */
export interface CheckoutDriver {
  /** The provider's name as the API reports it (Payments:Provider). */
  provider: string
  /** Opens the form. Rejects with a CheckoutCancelled when the person closes it without paying. */
  open(session: CheckoutSession): Promise<CheckoutResult>
}

export class CheckoutCancelled extends Error {
  constructor() {
    super('Payment was cancelled')
    this.name = 'CheckoutCancelled'
  }
}
