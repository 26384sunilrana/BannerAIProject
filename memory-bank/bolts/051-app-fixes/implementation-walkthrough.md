---
stage: implement
bolt: 051-app-fixes
created: 2026-11-21T13:06:55Z
---

## Implementation Walkthrough: three application fixes

### Summary

The Ad rates and Ad report pages no longer draw the menu bar twice. "Who am I" no longer counts toward the 20-a-minute sign-in limit, so quick navigation is no longer refused. Money is shown one way everywhere, in the currency it was priced in: plans can have a currency (INR by default), and ad rates, ads and the monthly statement carry one, stored with the amount. The demo now runs with no rate-limit wait.

### Structure Overview

Web: the admin layout alone gives admin pages their frame; every price on screen goes through the one shared money formatter. API: a small platform-currency setting is read once and handed to the ad services; a rate takes it when saved, an ad copies it from the rate that priced it, and the statement adds up only amounts in its own currency. The rate limiter sorts one more request (the "who am I" read) into the ordinary class. One migration adds the currency column to ad rates and ads.

### Completed Work

- [x] `src/BannerUI/src/app/admin/ad-rates/page.tsx`, `.../ad-reports/page.tsx` - no second page frame; prices on Ad rates use the shared money formatter.
- [x] `src/BannerService/Presentation/Middleware/RateLimiting.cs` - the "who am I" read is an ordinary call, counted per signed-in person; sign-in, sign-up, two-step and the rest stay at 20 a minute per address.
- [x] `src/BannerService/Application/Services/PlatformCurrency.cs` (new) and `Program.cs` - the platform currency (setting, INR when empty or invalid), registered once.
- [x] `src/BannerService/Domain/Entities/AdRate.cs`, `ShopAd.cs`, `Infrastructure/Data/ApplicationDbContext.cs` - a three-letter currency on rates and ads, INR for existing rows.
- [x] `src/BannerService/Infrastructure/Data/Migrations/20261121124134_AddAdCurrency*` - adds the two columns; applied cleanly on a real SQL Server container.
- [x] `src/BannerService/Application/Services/AdRateService.cs`, `ShopAdService.cs`, `AdStatementService.cs` - currency on the responses; rate takes the platform currency when saved; an ad keeps its rate's currency; statement totals count only lines in the statement's currency, others are shown in their own and marked.
- [x] `src/BannerService/Presentation/Controllers/SubscriptionPlanController.cs` - the administrator's plan responses, create and update carry a currency; empty means INR; an invalid code is refused; a currency change counts as a price change (blocked while shops are subscribed).
- [x] `src/BannerUI/src/types/subscription.ts`, `src/api/adService.ts` - currency fields (and "in total" on statement lines).
- [x] `src/BannerUI/src/components/admin/PlanForm.tsx` - a Currency choice (INR first, a short list, a plan's own code kept), labels follow it.
- [x] `src/BannerUI/src/components/admin/SubscriptionPlansTable.tsx`, `app/admin/subscription-plans/[planId]/page.tsx`, `app/ads/page.tsx`, `app/ads/statement/page.tsx`, `components/ads/AdForm.tsx` - all money through the shared formatter; the statement's own helper is gone; a line in another currency says it is not in the total.
- [x] `tests/BannerService.IntegrationTests/RateLimitSmokeTests.cs` - sorting rows for the "who am I" read, and a test that calls to it leave the sign-in limit untouched while sign-in is still limited.
- [x] `tests/BannerService.Application.Tests/Services/AdPricingServiceTests.cs`, `PlatformCurrencyTests.cs`, `Controllers/SubscriptionPlanCurrencyTests.cs` - currency on rate, ad and statement; the setting; plan create and update.
- [x] `src/BannerUI/__tests__/auth/adminFrame.test.tsx` (new), `adPricing.test.tsx`, `components/PlanForm.test.tsx`, `components/SubscriptionPlansTable.test.tsx` (new) - one menu bar and the source guard; new money display; mixed-currency statement; plan currency.
- [x] `docs/operations/deployment.md`, `runbook.md` - what the limit counts.
- [x] `docs/guides/admin.md`, `shop-owner.md`, `start-here.md`, `walkthroughs/admin.md` - currency wording; ₹ on the quoted rate.
- [x] `demo/e2e-story.mjs`, `demo/take-screenshots.mjs` - comments corrected; the retries stay as a safety net.
- [x] `docs/guides/walkthroughs/images/`, `docs/guides/walkthroughs/docx/` - pictures retaken and Word files rebuilt on a freshly wiped database.
- [x] `memory-bank/intents/002-requirements-gap-closure/backlog.md` - bolt 050 done, bolt 051 next; the "currency not modelled" line updated.

### Key Decisions

- **The "who am I" read joins the ordinary class, by method and exact path**: it needs a valid sign-in, so it cannot be used to guess anything; a test proves sign-in is still limited.
- **Currency stored with the amount (decided at approval)**: a rate takes the platform currency when saved, an ad copies it from the rate when its price is fixed. An amount never changes meaning if the setting changes.
- **A statement never adds two currencies**: lines in another currency are shown in their own currency and left out of the totals, and say so.
- **Optional constructor arguments for the currency**: the existing service constructions in tests keep working; without it everything is INR.
- **A plan's currency change is treated like a price change**: blocked while shops are subscribed.

### Deviations from Plan

- Added at approval: currency stored per ad and rate (a migration), and a currency choice on the plan form.
- Found while reading: the administrator's plan API had no currency at all (its own response type, create and update); fixed as part of the plan currency work.
- The plan page (single plan) is covered by the type check and the same formatter, not by its own render test (it needs the plan hook).

### Dependencies Added

None.

### Developer Notes

- Run order for clean pictures: wipe, seed, pictures first, then the story (the story adds shops).
- The retries in the demo scripts are a safety net; a normal run now has no rate-limit wait (two runs in a row, none).
- The statement's tiny hours in the pictures come from a database seeded minutes before.
