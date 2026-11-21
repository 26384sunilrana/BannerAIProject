---
stage: model
bolt: 044a-provider-seams
created: 2026-11-21T08:46:00Z
---

## Static Model: provider seams (payments, SMS, email, account recovery)

Scope: the domain concepts needed so that payments, SMS and email can be switched on later by filling in settings. Written from the bolt plan and the decisions of 2026-11-21; no source code was read in this stage.
Existing concepts (Shop, Subscription, SubscriptionPlan, Invoice, User, SubscriptionNotification) are referred to, not redefined; only the changes to them are listed.

### Entities

- **PaymentAttempt** (new): Id, InvoiceId, SubscriptionId, ShopId, Provider (name), ProviderOrderId, ProviderPaymentId (set once known), Amount (Money), Status (Created, Paid, Failed, Refunded, PartlyRefunded), CreatedAt, CompletedAt, FailureReason.
  Rules: an invoice has at most one Paid attempt; Paid and Failed are final; a refund can only follow Paid; provider references are stored encrypted like the existing payment reference; no card data is ever stored.
- **Refund** (new): Id, PaymentAttemptId, Amount (Money), Reason, RequestedByUserId (an administrator), ProviderRefundId, Status (Requested, Done, Failed), CreatedAt.
  Rules: the total refunded never exceeds the amount paid; only administrators may request one; each request is audited.
- **WebhookReceipt** (new): Id, Provider, ProviderEventId, EventType, ReceivedAt, ProcessedAt, Outcome.
  Rules: (Provider, ProviderEventId) is unique, so the same event delivered twice is applied once.
- **Invoice** (existing, one change): gains Currency. Becomes Paid only through a Paid PaymentAttempt, or through the existing Renew click while no provider is configured.
- **SubscriptionPlan** (existing, one change): gains Currency (default INR).
- **User** (existing, change in how tokens are kept): the password-reset and e-mail-verification tokens are stored as hashes, are single-use, and expire (reset 1 hour; verification 24 hours).
- **SubscriptionNotification** (existing, unchanged): remains the once-a-day claim record for reminders; SMS and e-mail results are written to it.

### Value Objects

- **Money**: Amount, CurrencyCode. Equality by value; amounts are decimal with the currency's minor units (paise, cents) used only at the provider boundary.
- **CurrencyCode**: ISO 4217 three letters, upper case (INR by default). Unknown codes are refused.
- **CountryCode**: ISO 3166 alpha-2 (IN by default).
- **PhoneNumber**: E.164 form (+ then digits). Built from user input and the default country code; invalid numbers are refused; printed only masked (last four digits).
- **SmsTemplate**: Key (for example renewal-reminder), ProviderTemplateId (the DLT id in India), Text with named variables.
- **SmsMessage**: To (PhoneNumber), TemplateKey, Variables.
- **CheckoutSession**: Provider, ProviderOrderId, Amount (Money), PublicKeyId (safe to show in the browser).
- **PaymentEvent**: the provider's webhook turned into a neutral form: ProviderEventId, Type (PaymentSucceeded, PaymentFailed, RefundDone), ProviderOrderId, ProviderPaymentId, Amount (Money).
- **ProviderStatus**: Name (Payments, Sms, Email), Provider, Configured (yes/no). Never carries a key, secret or address.
- **RecoveryToken**: random value shown once, plus its hash, expiry and purpose (PasswordReset or VerifyEmail).

### Aggregates

- **Payment** (root PaymentAttempt): Members: Refund. Invariants: at most one Paid attempt per invoice; refunds never exceed the paid amount; currency of attempt, invoice and refunds is the same.
- **WebhookReceipt** (own root): Invariant: unique per provider and event id.
- **Invoice** and **Subscription** stay roots as today; they change only by a PaymentSucceeded event (invoice paid, subscription renewed) or by the existing Renew click.
- **User account recovery**: root User; invariant: at most one live token per purpose; using a token consumes it.

### Domain Events

- **CheckoutStarted**: Trigger: owner starts payment for an invoice - Payload: InvoiceId, Money, Provider.
- **PaymentSucceeded**: Trigger: verified webhook says paid - Payload: ProviderOrderId, ProviderPaymentId, Money. Effect: invoice Paid, subscription renewed.
- **PaymentFailed**: Trigger: verified webhook says failed - Payload: ProviderOrderId, reason. Effect: failure counted; the existing grace week applies.
- **RefundDone**: Trigger: administrator refund accepted by the provider - Payload: PaymentAttemptId, Money.
- **SmsSent / SmsNotSent**: Trigger: a reminder is sent - Payload: template key, masked number, result.
- **PasswordResetRequested / PasswordWasReset**: Trigger: forgot-password request / successful reset - Payload: UserId (never the token). A reset also ends all sessions.
- **EmailVerified**: Trigger: a valid verification token is used - Payload: UserId.

### Domain Services

- **PaymentProviderRegistry**: chooses the provider named in settings once at start-up; None means no provider. Operations: Current, IsConfigured.
- **PaymentReconciliation**: applies a verified PaymentEvent exactly once (checks WebhookReceipt, checks amount and currency against the invoice, then pays the invoice and renews). Dependencies: Payment and Invoice repositories, Subscription lifecycle.
- **PhoneNumberNormaliser**: raw text and default country code in, PhoneNumber out or refusal.
- **SmsTemplateCatalog**: looks up a template by key from settings; an unknown key is refused, not sent as free text.
- **RecoveryTokenService**: Issue, Validate, Consume for both purposes; hashing and expiry live here.
- **ProviderStatusService**: reports ProviderStatus for payments, SMS and e-mail for the administrator screen.

### Ports (what the domain needs from outside)

- **Payment provider**: create a checkout for Money and an invoice reference; read and verify a webhook into a PaymentEvent; refund a payment. One implementation per provider (Razorpay now). The existing "charge for an automatic renewal" contract stays for the renewal job.
- **SMS sender**: send an SmsMessage using its template; returns delivered or not, never throws. The existing plain-text send stays for compatibility.
- **E-mail sender**: unchanged (SMTP when configured).

### Repository Interfaces

- **PaymentAttemptRepository**: Entity: PaymentAttempt - Methods: Add, GetByOrderId, GetPaidForInvoice, Update, ListForShop.
- **RefundRepository**: Entity: Refund - Methods: Add, ListForPayment, TotalRefunded.
- **WebhookReceiptRepository**: Entity: WebhookReceipt - Methods: TryRecord (false when already seen), MarkProcessed.
- **User, Invoice, SubscriptionPlan repositories**: existing; only the new fields and the token lookups (by token hash) are added.

### Rules that cut across the model

- Only an empty setting means "off": with every provider setting empty, nothing changes from today (Renew click, SMS not sent, mail not sent) and ProviderStatus says not configured.
- India is the default (INR, IN, +91) but each is a setting; no rule may hard-code rupees, a country or a provider.
- No PHI or PII goes to a provider beyond what is required: payment orders carry the invoice number, not names or addresses; SMS text carries only template variables such as a date.
- Recovery responses are identical whether or not the address exists.

### Ubiquitous Language

- **Provider**: an outside company that takes payments, sends SMS or sends e-mail.
- **Seam**: the contract between the application and a provider, so one provider can be swapped for another.
- **Configured**: all required settings for a provider are present; the only state that turns a provider on.
- **Checkout**: the step where the owner pays for an invoice with the provider.
- **Webhook**: the provider's call to the application saying a payment succeeded, failed or was refunded.
- **Idempotent**: applying the same event twice has the same effect as once.
- **Renew click**: today's behaviour with no provider: pressing Renew extends the plan without charging.
- **DLT**: the Indian registration of sender and message templates that SMS needs.
- **Template**: a registered SMS text with named gaps.
- **E.164**: the international phone number form (+91...).
- **Recovery token**: the one-use link value for resetting a password or verifying an e-mail.

### Story coverage

- payment-seam: PaymentProviderRegistry, Money, CurrencyCode, CountryCode, provider port.
- razorpay-adapter-webhook: PaymentEvent, WebhookReceipt, PaymentReconciliation, Refund.
- checkout-and-currency: PaymentAttempt, CheckoutSession, Currency on Invoice and SubscriptionPlan.
- sms-seam-msg91: PhoneNumber, SmsTemplate, SmsMessage, SmsTemplateCatalog, SMS port.
- provider-status-and-test-buttons: ProviderStatus, ProviderStatusService.
- account-recovery-screens: RecoveryToken, RecoveryTokenService, User token rules, recovery events.
- setup-guide-update: no domain concepts (documentation).
