# Construction Log: 044a-provider-seams

Intent: 002-requirements-gap-closure (no unit; bolts 031 and later sit directly under the intent)

- **2026-11-21T08:45:30Z**: 044a-provider-seams started - Stage 1: Domain Model
- **2026-11-21T08:52:00Z**: 044a-provider-seams stage-complete - domain-model → technical-design
- **2026-11-21T09:10:00Z**: 044a-provider-seams stage-awaiting-approval - technical-design written (rule slip: backend folders and Invoice.cs were opened in stage 2; disclosed in the document)
- **2026-11-21T09:20:00Z**: 044a-provider-seams stage-complete - technical-design → adr-analysis
- **2026-11-21T09:35:00Z**: 044a-provider-seams adr-analysis - 5 ADRs created (ADR-004 to ADR-008), decision index updated; awaiting approval to start Stage 4: Implement
- **2026-11-21T09:40:00Z**: 044a-provider-seams stage-complete - adr-analysis → implement
- **2026-11-21T10:30:00Z**: 044a-provider-seams implement - code written for all 7 stories; API project builds with 0 errors, web app type-checks; migration AddProviderSeams generated (not applied to any database); awaiting approval to start Stage 5: Test

## Differences between the design and the code (stage 4)

- A payment attempt has no invoice until it succeeds (the renewal creates the paid invoice), so `PaymentAttempt.InvoiceId` is nullable; the one-paid-per-invoice index ignores empty values.
- One `IPaymentRepository` covers attempts, refunds and webhook receipts instead of three repositories.
- A webhook receipt that was recorded but not applied (the server failed halfway) is retried when the provider repeats the event; only an applied receipt counts as a repeat. Two deliveries at once are stopped by a row version on the attempt.
- SMS templates take their values in order; the provider names for the gaps are `Sms:Templates:<key>:VariableNames` (default var1, var2). `Sms:SenderId` was dropped: in MSG91's flow the sender belongs to the registered template.
- With a real payment provider an owner's Renew click answers 409 `payment_required` (administrators can still renew by hand). The renewal job ignores auto-renew.
- Old reset and verification links that were plain tokens are cleared by the migration (they lived one hour).
- Open, to decide in bolt 044: a brand-new subscription still starts its first period without payment, also with a provider; the admin plan screens cannot set a plan's currency (default INR); `next lint` is not set up in the web app.
- **2026-11-21T10:40:00Z**: 044a-provider-seams stage-complete - implement → test
- **2026-11-21T11:35:00Z**: 044a-provider-seams test - 396 + 809 .NET unit, 137 integration, 830 web tests pass; report written (ddd-03-test-report.md); awaiting approval to complete the bolt
- **2026-11-21T09:57:15Z**: 044a-provider-seams completed - all 5 stages done (bolt-complete.cjs marked the bolt complete, then stopped on "unit undefined": this bolt has no unit and no story files, like bolts 031-049)

Note: the times written by hand in this log before this line are estimates; the completion time comes from the clock.
