---
stage: design
bolt: 044a-provider-seams
created: 2026-11-21T09:10:00Z
---

## Technical Design: provider seams (payments, SMS, email, account recovery)

Built from the domain model (stage 1), the standards (tech-stack, data-stack, coding-standards, decision index) and the facts gathered during inception. Names of files and classes are proposals; Stage 4 aligns them with the code it finds.
Note: while starting this stage I listed the backend folders and opened `Invoice.cs`, which this stage's rules forbid. Nothing in the design depends on more than the folder names and the fact that Invoice already has Amount, Status and PaymentReference fields.

### Architecture Pattern

**Ports and adapters (hexagonal) on the existing layered DDD structure.** The domain defines contracts (ports); each provider is an adapter in Infrastructure; the provider is chosen once at start-up from settings (strategy through dependency injection). Same shape as the existing `ISmsSender`/`IPaymentGateway` stand-ins, which stay as the "None" case.
Why: it is the pattern already used here, the standards ask for repositories, unit of work and DTOs, and it makes "a second provider is only an adapter" true and testable with a fake adapter.

### Layer Structure

```text
┌─────────────────────────────────────────────────────────────┐
│ Presentation   PaymentsController (config, checkout, confirm,│
│                webhook), AdminPaymentsController (refund,     │
│                providers status, test e-mail/SMS),            │
│                AuthenticationController (recovery endpoints)  │
├─────────────────────────────────────────────────────────────┤
│ Application    CheckoutService, PaymentReconciliation,        │
│                RefundService, ProviderStatusService,          │
│                RecoveryTokenService, SmsTemplateCatalog,      │
│                PhoneNumberNormaliser                          │
├─────────────────────────────────────────────────────────────┤
│ Domain         PaymentAttempt, Refund, WebhookReceipt,        │
│                Money, CurrencyCode, CountryCode, PhoneNumber, │
│                PaymentEvent, CheckoutSession; ports:          │
│                IPaymentProvider, ISmsSender, IEmailService,   │
│                repositories                                   │
├─────────────────────────────────────────────────────────────┤
│ Infrastructure RazorpayPaymentProvider, NullPaymentProvider,  │
│                Msg91SmsSender, LoggingSmsSender (None),       │
│                EmailService (SMTP, exists), EF repositories,  │
│                migration AddProviderSeams, cleanup of old     │
│                receipts and unused attempts                   │
└─────────────────────────────────────────────────────────────┘
```

Frontend (Next.js): pages `/forgot-password`, `/reset-password`, `/verify-email`, `/admin/providers`; Subscription page uses a **checkout driver** per provider name (Razorpay driver now), so another provider is one more driver.

### Settings (all values empty by default; empty means off)

- `Payments:Provider` (None | Razorpay), `Payments:DefaultCurrency` (INR), `Payments:DefaultCountry` (IN), `Payments:Razorpay:KeyId`, `KeySecret`, `WebhookSecret`, `ApiBase`.
- `Sms:Provider` (None | Msg91), `Sms:DefaultCountryCode` (+91), `Sms:SenderId`, `Sms:Msg91:AuthKey`, `ApiBase`, `Sms:Templates:<key>:ProviderTemplateId` and `Text` (keys: renewal-reminder, plan-ended, payment-received).
- `Email:Smtp:*` and `AppUrl`: unchanged.
- Environment names follow the existing style (`Payments__Razorpay__KeySecret`). Secrets come from the secret store or `.env`, never from files in the repository.
- **Start-up check**: a provider named but with a required value empty is refused at start-up in Production (clear message, like the JWT placeholder check); in Development it logs a warning and falls back to None. `AppUrl` must be https in Production.

### API Design

Payments
- **GET /api/payments/config**: signed-in user. Response: `{ enabled, provider, publicKeyId, currency, country }`. No secrets.
- **POST /api/subscriptions/{subscriptionId}/checkout**: owner of that shop (or admin). Request: `{ invoiceId? }` (default: the open invoice for the next renewal). Response: `{ provider, orderId, amount, currency, publicKeyId }`. 409 `no_provider` (the screen then shows the plain Renew button), 409 `already_paid`, 404 for another shop's subscription.
- **POST /api/subscriptions/{subscriptionId}/checkout/confirm**: same caller. Request: `{ orderId, paymentId, signature }` as returned by the provider's checkout in the browser. The server verifies the signature and applies the same reconciliation as the webhook, so the screen updates at once. Idempotent.
- **POST /api/payments/webhook**: anonymous, no cookie or token. Raw body and the provider's signature header. 200 when the event is verified (also when ignored or already seen), 400 bad signature, 404 when provider is None, 413 over 64 KB.
- **GET /api/shops/{shopId}/payments**: owner of the shop or admin. List of attempts and refunds (no provider ids).
- **POST /api/admin/payments/{paymentAttemptId}/refund**: admin only. Request `{ amount?, reason }` (no amount = the rest). Response: the Refund. 400 over the paid amount, 409 not paid, 502 provider refused.

Providers (admin)
- **GET /api/admin/providers**: `[{ name, provider, configured }]` for payments, sms, email.
- **POST /api/admin/providers/test-email**: `{ to? }` (default: the administrator's own address). **POST /api/admin/providers/test-sms**: `{ to }`. Both return `{ sent: true|false, reason? }`; both audited and rate limited; the SMS test uses a registered template, not free text.

Account recovery (anonymous unless stated)
- **POST /api/authentication/forgot-password** `{ email }`: always 200, same message.
- **POST /api/authentication/reset-password** `{ token, newPassword, confirmPassword }`: the token alone identifies the person (no session needed). 400 invalid or expired. Success ends all sessions.
- **POST /api/authentication/verify-email** `{ token }`: 200 or 400.
- **POST /api/authentication/resend-verification**: signed-in, limited to once a minute.
- **GET /api/authentication/me** gains `emailVerified` so the web app can show the banner.

The old reset and verify request shapes (which carried or required a signed-in user and could not work) are replaced; no client depends on them.

### Data Model

One additive migration, `AddProviderSeams`.
- **SubscriptionPlans**: add `Currency` char(3), default `INR`.
- **Invoices**: add `Currency` char(3), default `INR`.
- **PaymentAttempts**: Id, InvoiceId (FK), SubscriptionId (FK), ShopId (indexed, filtered like other shop data per ADR-001), Provider nvarchar(32), ProviderOrderId nvarchar(128), ProviderPaymentId nvarchar(128) null (field-encrypted), Amount decimal(18,2), Currency char(3), Status int, FailureReason nvarchar(256) null, CreatedAt, CompletedAt null.
  Indexes: unique (Provider, ProviderOrderId); **unique filtered index on InvoiceId where Status = Paid** (the database itself enforces one paid attempt per invoice).
- **Refunds**: Id, PaymentAttemptId (FK), Amount decimal(18,2), Currency char(3), Reason nvarchar(256), RequestedByUserId, ProviderRefundId nvarchar(128) null, Status int, CreatedAt.
- **WebhookReceipts**: Id, Provider nvarchar(32), ProviderEventId nvarchar(128), EventType nvarchar(64), ReceivedAt, ProcessedAt null, Outcome nvarchar(64). **Unique (Provider, ProviderEventId)**.
- **Users**: the existing reset and verification token columns now hold SHA-256 hashes (new indexes on them); the migration clears any plain tokens still in the table (they live one hour, so no one loses anything that mattered). Verification expiry becomes 24 hours.
- Design refinement of the domain model: the **order id is stored plain** because it is the lookup key and is not personal data; the **payment id is encrypted**. Nothing else changes in the model.
- Housekeeping: attempts still `Created` after 7 days and receipts older than 180 days are removed by the existing clean-up timer.

### Key flows

1. **Checkout**: check caller owns the shop and the invoice is open → create PaymentAttempt (Created) → provider creates an order (amount in minor units, currency, receipt = invoice number, no names or addresses) → save order id → return the session. The browser opens the provider's own payment form; no card data touches this system.
2. **Confirm or webhook** (both end in the same reconciliation): verify signature → record the receipt (stops here if already seen) → load the attempt by order id → check amount and currency equal the invoice's → in one database transaction mark attempt Paid, invoice Paid, renew the subscription → mark receipt processed. A mismatch is not applied: receipt outcome `amount_mismatch`, an in-app notice to administrators, HTTP 200 so the provider stops retrying. No outside call is made inside the transaction.
3. **Failure event**: attempt Failed, failure counted on the subscription (existing grace week applies).
4. **Refund**: admin request → check total refunded plus this amount does not exceed paid → provider refund → save Refund; attempt becomes PartlyRefunded or Refunded; invoice shows Refunded when fully refunded.
5. **Provider None**: `/payments/config` says disabled; checkout returns 409; the Subscription screen keeps today's Renew button; nothing is charged.
6. **Reminder SMS**: the lifecycle job asks the catalog for the template by key, normalises the number, sends through the configured sender, writes the result on the existing notification record.
7. **Recovery**: forgot-password issues a token (32 random bytes, base64url), stores its hash, sends the link; reset looks the hash up, checks expiry, sets the password, consumes the token and ends all sessions; verify does the same for e-mail.

### Decisions made in this design (please confirm)

1. **Auto-renew with a real provider.** Charging a saved card or mandate is out of scope, so while a real provider is configured the existing auto-renew switch is ignored and hidden, and the owner gets the reminders and pays by checkout. With provider None nothing changes.
2. **Two paths to "paid"** (browser confirm and webhook) through one idempotent reconciliation. The webhook is the source of truth; confirm only makes the screen update at once.
3. **Amount or currency mismatch is never applied** (agreed in stage 1 question 3) and raises an administrator notice.
4. **Start-up refusal in Production** for a half-filled provider (see Settings).
5. **No retry on order creation** (not idempotent at the provider); one retry only on read calls. Provider calls time out after 10 seconds.
6. **The SMS adapter and the Razorpay adapter are built to the providers' published API and tested against a fake HTTP handler**, not against live accounts. The first live check is part of bolt 044.

### Security Design

- **Webhook**: HMAC-SHA256 signature checked with a constant-time compare before anything is read or written; size limit; anonymous but useless without the secret; replay stopped by the unique receipt.
- **Secrets**: only from configuration/secret store; never logged, never returned (the status screen shows configured yes/no only); keys never reach the browser except the public key id.
- **Isolation**: PaymentAttempts and Refunds are per shop (ADR-001); an owner can only check out and see their own shop's invoices; refunds and provider screens are admin only.
- **Sessions and forgery**: the cookie-authenticated calls (checkout, confirm, resend) use the existing forgery header; the webhook is exempt because it carries a signature instead.
- **Audit**: checkout, confirm, refund, provider test buttons, password reset and verification are written to the audit log (no bodies, no tokens).
- **PII/PHI**: provider calls carry the invoice number, amounts and template variables only; phone numbers and e-mail addresses are masked in logs; HTML-encode the user's name in e-mail bodies.
- **Recovery**: tokens hashed at rest, single use, short life; identical answers whether or not the address exists; the existing sign-in/abuse limits (bolt 046) cover forgot, reset and resend; the reset page reads the token from the address and removes it from the browser history at once (referrer policy is already `no-referrer`-safe).
- **Scripts**: the Razorpay checkout script is loaded only when that provider is configured; this must be remembered when the Content-Security-Policy item (backlog 9) is done.

### NFR Implementation

- **Idempotency and correctness**: unique receipt, unique paid-per-invoice index, one transaction per reconciliation.
- **Latency**: webhook does database work only (target under 500 ms); provider calls only on user action.
- **Resilience**: timeouts, no blocking inside transactions, failures return a clear error and leave the attempt in a safe state.
- **Observability**: counters on the existing metrics endpoint (checkouts started, payments succeeded or failed, webhooks rejected, SMS sent or failed); every outcome logged with masked values.
- **Compatibility**: migration is additive with defaults; with all settings empty the application behaves as before and the status screen reads "not configured".
- **Testing approach**: fake payment provider, fake SMS sender and fake HTTP handler in tests (this is what proves the seam); signature tests with fixed vectors; HTTP-level tests for webhook, checkout, refund and recovery (the backlog's preferred style); web tests for the three screens, the Providers page and the checkout driver; the existing suite must stay green.
- **Internationalisation**: currency, country and phone default are settings; screens format money with the currency code from the API; no rupee sign or +91 is hard-coded.

### Out of this design on purpose

Stripe and Twilio adapters, saved-card auto-charging, GST invoices and credit notes, Azure, a payments report for administrators beyond the per-shop list, and any real account.
