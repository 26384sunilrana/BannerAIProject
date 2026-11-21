---
bolt: 044a-provider-seams
created: 2026-11-21T09:31:00Z
status: accepted
---

# ADR-005: Auto-renew is ignored and hidden while a real payment provider is configured

## Context

Subscriptions have an auto-renew switch. Today the renewal job charges through `IPaymentGateway`, and with no provider every charge counts as paid. Charging a saved card or an e-mandate needs the provider's subscription or mandate features, extra approvals in India, and storing a payment method reference. Product decision of 2026-10-03: renewal is a "Renew" button until a provider exists; nothing about saved-card charging has been decided.

## Decision

While `Payments:Provider` names a real provider, the auto-renew switch is ignored by the renewal job and hidden on the Subscription screen. Owners get the existing reminders and pay by checkout. With provider `None` nothing changes. Saved-card or mandate charging is a later, separate decision.

## Rationale

Leaving the switch active with a real provider would make the job try to charge without a saved method, fail, count failures and push shops into the grace week for no fault of theirs.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Build saved-card/mandate charging now | Fully automatic renewal | Large, India-specific rules, needs a live account to test | Out of scope for the seam bolt |
| Keep the switch and report "needs owner action" | No hidden control | Confusing: a switch that does nothing | Misleading to owners |
| Remove the switch entirely | Simplest | Breaks the no-provider behaviour and existing data | Loses today's behaviour |

## Consequences

### Positive

- No surprise failed charges when a provider is switched on.
- Behaviour with provider `None` is untouched.

### Negative

- Owners must pay each period themselves once payments are live.
- Existing auto-renew flags stay stored but unused until saved-card charging is built.

### Risks

- Owners expect automatic renewal: reminders (e-mail and SMS) and clear wording on the Subscription screen reduce missed renewals; revisit when mandates are built.

## Related

- **Stories**: checkout-and-currency
- **Standards**: none
- **Previous ADRs**: ADR-004; decisions.md 2026-10-03 (Payment: a Renew click only)
