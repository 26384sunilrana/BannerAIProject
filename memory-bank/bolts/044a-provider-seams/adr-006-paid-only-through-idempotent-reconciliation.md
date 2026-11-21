---
bolt: 044a-provider-seams
created: 2026-11-21T09:32:00Z
status: accepted
---

# ADR-006: "Paid" comes only from one idempotent reconciliation; the webhook is the source of truth

## Context

A payment can be reported twice: by the browser after checkout and by the provider's webhook, which may itself be delivered several times or out of order. The owner wants the screen to update at once, but money must never be applied twice, and a payment for the wrong amount or currency must never renew a plan. The application never sees card data.

## Decision

Both the browser confirm and the webhook feed one reconciliation step. It verifies the signature, records a webhook receipt (unique per provider and event id), checks that amount and currency equal the invoice's, and then in one database transaction marks the attempt Paid, the invoice Paid and renews the subscription. The webhook is the source of truth; the confirm call only speeds up the screen. The database enforces one Paid attempt per invoice (unique filtered index). A mismatch is not applied: it is recorded, administrators get an in-app notice, and the provider still gets HTTP 200. No outside call is made inside the transaction.

## Rationale

One code path means one set of rules to test. Database constraints protect against bugs and races that application checks can miss.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Trust the browser confirm only | Simple, instant | Browser can be closed or tampered with; lost payments | Unsafe |
| Webhook only | Single source | Screen waits for the webhook (seconds to minutes) | Poor experience |
| Two separate code paths | Each is simple | Rules can drift apart; double-apply risk | Hard to keep correct |

## Consequences

### Positive

- Repeated or reordered events are harmless.
- Wrong-amount payments are caught and visible to administrators.
- Easy to test with a fake provider and fixed signature vectors.

### Negative

- A mismatched payment stays unapplied until an administrator acts (refund or manual fix).
- Slightly more tables (receipts, attempts).

### Risks

- Webhook endpoint is public: protected by constant-time HMAC check, size limit and rate limits; secret kept only in the secret store.

## Related

- **Stories**: razorpay-adapter-webhook, checkout-and-currency
- **Standards**: none
- **Previous ADRs**: ADR-004, ADR-001 (payments are per shop)
