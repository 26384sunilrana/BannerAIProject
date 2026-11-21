---
stage: test
bolt: 044a-provider-seams
created: 2026-11-21T11:30:00Z
---

## Test Report: provider seams (payments, SMS, email, account recovery)

### Summary

- **Unit tests** (.NET, application layer): 396/396 passed (270 before this bolt, so 126 new).
- **Unit tests** (.NET, domain): 809/809 passed (unchanged).
- **Integration tests** (.NET, the real application on an in-memory database): 137/137 passed (26 new, plus the existing ones).
- **Web tests** (Jest): 830/830 passed (791 before, so 39 new). The TypeScript compiler reports no errors.
- **Security tests**: the security rules listed below are each covered by new tests; all passed.
- **Performance tests**: not done (see Issues).
- **Coverage of the new and changed backend files**: 94.2% of lines (1353 of 1437), measured over all three .NET suites together. Web files of this bolt: 91.5% of lines, 90.2% of statements; the four-line page wrappers are not exercised.

### Acceptance Criteria Validation

- ✅ **payment-seam**: with every setting empty the application behaves as before. Payments config says off, checkout answers 409, the webhook answers 404, status says "not connected" - covered by `ProviderOffSmokeTests`, `PaymentServiceTests`, `ProviderSettingsTests`.
- ✅ **payment-seam (provider choice)**: a provider that is named but not fully set up counts as None; unsupported names, empty values and a non-https `AppUrl` in production are reported by setting name and never by value - `ProviderSettingsTests`. The refusal at start-up in production is the same check wired into `Program.cs`; it was not run as a start-up test (see Issues).
- ✅ **payment-seam (currency/country as settings)**: plan currency flows to the order, attempt and invoice; USD and INR tested; phone default country changeable - `Checkout_UsesThePlansCurrency…`, `AnotherCountryIsOnlyASetting`.
- ✅ **razorpay-adapter-webhook**: signature checks (checkout and webhook, constant time, empty secret never matches), event mapping, order creation with minor units and our reference only, refunds without the administrator's free-text reason - `RazorpayProviderTests`. Applied through the real application with scripted HTTP - `RazorpaySmokeTests`.
- ✅ **razorpay-adapter-webhook (idempotency)**: same event twice, browser confirm followed by webhook, two requests at once, and a half-applied receipt retried - `PaymentServiceTests`, `Webhook_PaysAnOrder_Once…`.
- ✅ **razorpay-adapter-webhook (never apply the wrong payment)**: another amount or currency is accepted with 200 but not applied, and administrators are notified - `APaymentForAnotherAmountOrCurrency…`, `Webhook_ForAnotherAmount…`.
- ✅ **checkout-and-currency**: checkout makes an order for the plan price; confirm pays, renews once and writes the paid invoice with its currency; another shop's subscription is refused; failed payment counts a failure; owner's Renew click needs payment while an administrator can still renew by hand; auto-renew is ignored with a provider and unchanged without one - `RazorpaySmokeTests`, `LifecycleWithProviderTests`.
- ✅ **sms-seam-msg91**: registered template only (free text refused), numbers normalised for India and another country, unknown template or bad number not sent, provider refusal and outage reported as not sent without throwing, numbers masked - `Msg91SmsSenderTests`, `PhoneNumberTests`, `SmsTemplateCatalogTests`, `SmsFor_…`.
- ✅ **provider-status-and-test-buttons**: administrators only (owner gets 403, anonymous 401); shows connected or not and missing setting names; no key, secret or address appears in any answer; test buttons say why nothing was sent - `ProviderStatusServiceTests`, `ProviderStatus_IsForAdministrators…`, `TheProvidersScreen_ShowsPaymentsConnected…`.
- ✅ **account-recovery-screens (backend)**: same answer for known and unknown addresses; token mailed and only its hash stored; the hash alone is no use; reset needs no session, is single-use, expires, ends all earlier sessions; verify works once; resend limited to one a minute - `AccountRecoveryTests`, `ProviderOffSmokeTests`.
- ✅ **account-recovery-screens (web)**: forgot, reset and verify pages, the sign-in link, the reminder banner (only when mail can be sent and the address is unverified), token removed from the address bar - `providers.test.tsx`.
- ✅ **setup-guide-update**: `providers-setup.md` now lists the values to fill in. Documentation only; reviewed by reading, no test.
- ⚠️ **Whole bolt, "with only test values filled in, payment, SMS and e-mail work end to end"**: proven against scripted provider answers, not against the real Razorpay or MSG91 services or a real mail server (see Issues).

### Security checks that passed
Webhook with a bad signature refused and leaves no receipt; oversize webhook refused (413); checkout and payment list limited to the owner's own shop; refunds and provider screens administrator-only; provider keys and secrets absent from every response; no names or e-mail addresses in the order sent to the provider; administrator's refund reason not sent to the provider; reset and verify tokens stored hashed; stored hash unusable as a token; unknown, expired and reused links refused; identical answer for known and unknown addresses; reset ends earlier sessions; mail failure does not change the answer; plain-text SMS refused.

### Issues Found

1. **A test had gone out of date by itself** (`RenewNow_BeforeTheDate_AddsAPeriodToTheCurrentOne`): it used a fixed "today" of 10 Nov 2026 while the code under test reads the real clock, so it began to fail on 20 Nov 2026. Not caused by this bolt; fixed by making the test relative to the real date.
2. **Test setup changed**: the in-memory test database cannot run transactions, which the payment step uses. `SmokeFactory` now ignores that warning. Tests that run on a real SQL Server (`SMOKE_SQL`) are unaffected.
3. **Not tested on a real SQL Server**: the in-memory database does not enforce the unique indexes (one paid attempt per invoice, one receipt per event), the row-version check or the filtered indexes, and the generated migration was never applied. `PaymentRepository` is at 63% for that reason (the duplicate-insert and concurrency paths). The same tests can be run on SQL with `SMOKE_SQL`; this was not done.
4. **Not tested against the real services**: Razorpay and MSG91 were exercised through fixed signatures and a scripted HTTP handler built from their published APIs. Their real responses, DLT template rules and webhook timing are untested (the first live check is bolt 044).
5. **No browser test**: the payment form, its script loading and the new pages were tested with Jest and mocks, not in a browser; no Playwright run.
6. **Start-up refusal in production** is covered by the settings checks but not by starting the application in Production mode.
7. **Performance not measured**: the design targets (webhook under 500 ms, no outside call inside the transaction) were reviewed in code but not load tested.
8. **Lint** is not set up in the web app (`next lint` asks to configure itself); only the compiler was run.
9. **39 older .NET test files stay switched off** (existing backlog item); unchanged by this bolt.

### Recommendations

- Run the integration suite once with `SMOKE_SQL` pointing at a SQL Server (or LocalDB) before the first deployment; it is the only way to prove the migration and the unique indexes.
- Decide in bolt 044 what happens to a brand-new subscription's first period when a provider is on (today it starts without payment).
- In bolt 044, with Razorpay test keys, run one real checkout, one failed payment, one webhook delivery and one refund, then do the same with MSG91 (including the DLT template ids).
- Move a short Playwright script for the new pages into the repository when backlog item 3 is done; the demo refresh (bolt 050) will exercise sign-in and subscription screens.
- Add the lint setup to the web app.
