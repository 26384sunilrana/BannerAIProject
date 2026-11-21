---
stage: plan
bolt: 051-app-fixes
created: 2026-11-21T12:21:37Z
---

## Implementation Plan: three application fixes

### Objective

Remove the three problems bolt 050 found, as decided: (1) one menu bar on Ad rates and Ad report, (2) the "who am I" call no longer counts toward the 20-a-minute sign-in limit, (3) money shown one way everywhere, with the currency coming from the API for ads. Then prove it with the demo on a clean database.

### Deliverables

**1. One page frame**
- `src/BannerUI/src/app/admin/ad-rates/page.tsx` and `.../ad-reports/page.tsx`: the page body is exported directly; the second frame (and its `Roles` import) goes. The admin layout still limits both to administrators.
- New web test `__tests__/auth/adminFrame.test.tsx`: reads every page file under `app/admin` and fails if one uses the page frame itself; renders Ad rates and Ad report under the admin layout and checks for exactly one main menu.

**2. `/me` outside the sign-in limit (option A)**
- `src/BannerService/Presentation/Middleware/RateLimiting.cs`: `Classify` sends `GET /api/authentication/me` to `Api` (counted per signed-in user, 1200 a minute) before the catch-all for `/api/authentication`. Other methods and every other path under it are unchanged. The class summary and the settings comment say so.
- `tests/BannerService.IntegrationTests/RateLimitSmokeTests.cs`: two more rows in the class test (`/api/authentication/me` GET is `Api`; POST to the same path stays `Authentication`) and one test that 25 calls to `/me` by a signed-in user in a minute are all answered while the 21st sign-in is still refused with 429.
- `docs/operations/deployment.md` (rate-limits line) and `docs/operations/runbook.md` (items 3 and 4): say that "who am I" is counted with the normal API calls, not with sign-in.

**3. One currency display**
- API, new `src/BannerService/Application/Services/PlatformCurrency.cs`: a small class holding the platform's currency code (from `Payments:DefaultCurrency`, INR when unset or invalid, using the existing `ProviderSettings.DefaultCurrency`); registered once in `Program.cs`.
- API, `AdRateService`, `ShopAdService`, `AdStatementService`: take it as a last optional constructor argument (so the three existing test constructions compile unchanged; without it the code is INR) and fill a new `Currency` field on `AdRateDto`, the ad response (`ShopAdDto`) and the statement (`AdStatementDto`). No database change; an ad keeps the price it was booked at (known limit, recorded in the backlog).
- Web types: `currency` added to the ad rate, ad and statement types in `src/BannerUI/src/api/adService.ts` and to the plan type in `src/BannerUI/src/types/subscription.ts`.
- Web screens, all through `formatMoney` in `lib/pricing.ts`: `components/admin/SubscriptionPlansTable.tsx` and `app/admin/subscription-plans/[planId]/page.tsx` (plan's own currency, no literal `$`); `app/admin/ad-rates/page.tsx` (price column); `app/ads/page.tsx` (price line); `app/ads/statement/page.tsx` (the `money()` helper goes; shop and city payouts, line prices and the total).
- Tests: `tests/BannerService.Application.Tests/Services/AdPricingServiceTests.cs` (currency is INR by default and follows the setting, on rate, ad and statement); web tests `adPricing.test.tsx` (price line now shows `₹50.00 an hour, the shop is paid 70% of it`), `admin.test.tsx` (plans show `₹`), `ads.test.tsx` (statement amounts), and a new case that a plan with currency USD shows `$`.

**4. Demo, pictures and documents** (after the code works)
- Rebuild the API and web images (`docker compose up --build -d`); wipe the database (`docker compose down -v`, permission rule in place), create the demo administrator, seed the demo.
- `node demo/take-screenshots.mjs`, then `node demo/build-docx.mjs`, then `node demo/e2e-story.mjs` twice in a row; the logs must have no "refused by the rate limit" line. Compare the retaken pictures with the old ones and look at each that changes (Ad rates, Ad report, Plans, Ads, Statement, and any other).
- `demo/e2e-story.mjs` and `demo/take-screenshots.mjs`: comments that say `/me` counts toward 20 a minute are corrected; the retries stay as a safety net.
- Guides and walkthroughs: search for amounts and currency signs; change only what quotes one. The walkthrough lines "20.00 an hour" style text, if any, follow the new display.
- Bolt records: `implementation-walkthrough.md`, `test-walkthrough.md`, `result-note.md` (quotes section E of bolt 050's drift list as closed; bolt 050's own folder is not touched). One local commit at the end; `BannerAIProject.sln` and `demo/output/` left out; not pushed.

### Dependencies

- Docker stack (rebuilt images; wiped database for the pictures; your Super Admin login goes again and is recreated with `--create-admin` if you give me a password).
- .NET 8 SDK and Node for the test suites; Chrome for the demo scripts.
- Nothing from other open bolts.

### Technical Approach

1. Record the starting point: run the .NET unit, integration and web suites once and note the counts (bolt 044a ended at 396 + 809 unit, 137 integration, 830 web).
2. Frame fix and its guard test first (smallest, web only); then the rate-limit class change with its tests (the integration test first, to see it fail, then the change); then the currency work, API first (`PlatformCurrency`, DTO fields, service tests), then the web types, screens and tests.
3. Run all suites again; fix what the new display changes in existing tests; keep a list of every changed expectation.
4. Rebuild the images, wipe, seed, retake pictures, rebuild the Word files, run the story twice.
5. Write the walkthrough, test report and result note; commit once.

### Acceptance Criteria

- [ ] Ad rates and Ad report show one main menu; a test fails if an admin page wraps itself in the frame again.
- [ ] 25 calls to `/api/authentication/me` in a minute from one address all succeed; the 21st sign-in in that minute is still refused with 429 (test).
- [ ] Plans (table and plan page), Subscription, Subscriptions, Ad rates, Ads and the Monthly statement show money in the same format and the right currency; ad responses carry `currency`.
- [ ] .NET and web test suites pass; counts before and after are in the test report.
- [ ] `node demo/e2e-story.mjs` passes twice in a row with no rate-limit wait in the log; `node demo/take-screenshots.mjs` takes every picture; changed pictures and Word files are retaken and rebuilt.
- [ ] Nothing pushed; other bolts' memory-bank folders untouched.

### Changes agreed at approval (2026-11-21T12:29:30Z)

- **Currency stored with each ad** (replaces "label with the platform currency"): \`ShopAd\` and \`AdRate\` get a \`Currency\` column (INR for existing rows) in one small migration; an ad copies the currency of the rate that priced it when its price is fixed, and keeps it. Responses carry the stored currency. A statement adds up only lines in the statement currency (the platform's); a line in another currency is shown in its own currency and marked as not in the total.
- **Plans can have another currency**: the New plan and Edit plan forms get a currency choice, default INR; plan table and page show the plan's own currency.

### Open points for you (answered)

1. The currency of existing ads is not stored: an ad booked in rupees would show in the new currency if the setting changes later. This bolt only labels; storing the currency per ad is a later bolt (backlog line added in this bolt's result note). Fine?
2. The plan page and table will show plan prices with the plan's own currency, so a plan priced in dollars would show `$`. Today every plan is INR.
