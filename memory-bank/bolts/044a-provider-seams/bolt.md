---
id: 044a-provider-seams
intent: 002-requirements-gap-closure
type: ddd-construction-bolt
status: complete
started: 2026-11-21T08:45:30.000Z
current_stage: null
stages_completed:
  - name: domain-model
    completed: 2026-11-21T08:52:00.000Z
    artifact: ddd-01-domain-model.md
  - name: technical-design
    completed: 2026-11-21T09:20:00.000Z
    artifact: ddd-02-technical-design.md
  - name: adr-analysis
    completed: 2026-11-21T09:40:00.000Z
    artifact: adr-004..008
  - name: implement
    completed: 2026-11-21T10:40:00.000Z
    artifact: source code
  - name: test
    completed: 2026-11-21T09:57:15Z
    artifact: ddd-03-test-report.md
stories:
  - payment-seam
  - razorpay-adapter-webhook
  - checkout-and-currency
  - sms-seam-msg91
  - provider-status-and-test-buttons
  - account-recovery-screens
  - setup-guide-update
created: 2026-11-21T08:39:37.000Z
requires_bolts: []
enables_bolts:
  - 044-real-providers
blocks: false
completed: "2026-11-21T09:56:58Z"
---

# Bolt 044a - Provider seams (payments, SMS, email, account recovery)

**Goal**: everything that needs a payment, SMS or email provider is built and tested without real accounts. Going live later means filling in empty config values, nothing else.
India is the default (INR, +91, Razorpay, MSG91/DLT), but country, currency and provider are settings, so another country or provider is an adapter, not a rewrite.
Decisions: see decisions.md (2026-11-21).

## What exists today (checked in code)
- `IPaymentGateway.ChargeAsync` with a stand-in (`NoPaymentGateway`, always "paid"); "Renew" button calls `renewNow`.
- `ISmsSender` with `LoggingSmsSender` (writes to log, returns false).
- `EmailService` already sends through SMTP when `Email:Smtp:Host` is set.
- `forgot-password` never sends the mail (TODO in `AuthenticationService.cs`); `reset-password` needs a signed-in user so cannot work; no forgot/reset/verify screens.
- Currency is not modelled anywhere.

## Stories

1. **payment-seam** - Provider-neutral payment contract (checkout, webhook, refund) beside the existing `ChargeAsync`. Config: `Payments:Provider` (None | Razorpay), `Payments:DefaultCurrency` (INR), `Payments:DefaultCountry` (IN), one empty section per provider. Provider chosen at start-up; `None` keeps today's behaviour (Renew click, nobody charged).
2. **razorpay-adapter-webhook** - Razorpay adapter (orders API, HMAC signature check, refunds) and `POST /api/payments/webhook` (anonymous, signature verified, idempotent, 404 when provider is None). Tested offline with a fake HTTP handler and known signature vectors. Keys: `Payments:Razorpay:KeyId/KeySecret/WebhookSecret`, empty.
3. **checkout-and-currency** - ISO currency on plans and invoices (default INR, migration), `GET /api/payments/config` (enabled, provider, public key id, currency), checkout call on the Subscription screen when a provider is configured; plain Renew button when not. Payment-failed path reuses the existing grace week. No card data stored.
4. **sms-seam-msg91** - `ISmsSender` gets templated messages (template key, variables, DLT template id) and E.164 phone normalisation with a default country code. Config `Sms:Provider` (None | Msg91), `Sms:SenderId`, `Sms:DefaultCountryCode`, templates by key; MSG91 adapter. Phone numbers masked in logs. The three renewal texts from providers-setup.md are the first templates.
5. **provider-status-and-test-buttons** - Admin "Providers" screen: for payments, SMS and email shows configured / not configured (never the values) and has "send me a test email / SMS" buttons; every use is audited.
6. **account-recovery-screens** - Backend: forgot-password sends the mail; reset works without a session (token looked up, stored hashed, single use, 1 hour); verification mail sent at sign-up. Screens: `/forgot-password`, `/reset-password`, `/verify-email`, link from `/login`. Same answer whether or not the address exists; covered by the existing abuse limits (bolt 046).
7. **setup-guide-update** - `docs/operations/providers-setup.md` rewritten so each "Give me" table is "set these values".

## Out of scope for this bolt
Real accounts and credentials, Stripe or Twilio adapters (the seam allows them; a fake provider in tests proves a second one plugs in), GST invoices, auto-charging of saved cards/e-mandates, Azure (all bolt 044).

## Decisions (approved 2026-11-21)
- Is e-mail verification required before first sign-in, or only a reminder? **Decided: not required; show a banner.**
- Stripe adapter now as a second proof, or later? **Decided: later; a fake provider in tests proves the seam.**
- Does a lapsed owner's plan stay "Renew click" with provider None in production? **Decided: yes, as decided 2026-10-03.**

## Acceptance
- With every provider setting empty the app behaves as today and the Providers screen says "not configured".
- With only test values filled in, payment, SMS and email work end to end without any code change.
- Tests: signature vectors, webhook idempotency, provider choice, phone normalisation, recovery flows (HTTP level), UI tests for the three screens.
