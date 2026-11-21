---
stage: test
bolt: 051-app-fixes
created: 2026-11-21T13:11:37Z
---

## Test Report: three application fixes

### Summary

- **Tests**: all pass. Web 839/839 (was 830, 67 suites), application 416/416 (was 396), integration 143/143 (was 137), domain 809/809 (unchanged). TypeScript type check clean.
- **Live checks on the rebuilt stack**: 25 of 25 "who am I" calls answered with no 429, and sign-in still refused from the 20th permit; story 42/42 twice in a row with no rate-limit wait; 46/46 pictures.
- **Coverage**: not measured (no coverage run in this bolt); every changed rule has a test, and the frame guard and the rate-limit tests were seen failing before the change.

### Test Files

- [x] `src/BannerUI/__tests__/auth/adminFrame.test.tsx` (new) - no page under the admin folder wraps itself in the frame; Ad rates and Ad report each show exactly one main menu under the admin layout. Failed 3 of 3 on the old pages.
- [x] `tests/BannerService.IntegrationTests/RateLimitSmokeTests.cs` - sorting rows for the "who am I" read (GET is ordinary; POST, a longer path and logout stay strict) and a test that five calls to it leave the sign-in limit untouched while sign-in is still limited. Failed before the change, passes after.
- [x] `tests/BannerService.Application.Tests/Services/AdPricingServiceTests.cs` - a rate takes the platform currency when saved; an ad keeps the currency of the rate that priced it after the platform changes; the statement adds up only its own currency and marks the others; the statement follows the setting.
- [x] `tests/BannerService.Application.Tests/Services/PlatformCurrencyTests.cs` (new) - the setting, INR when missing or not a three-letter code.
- [x] `tests/BannerService.Application.Tests/Controllers/SubscriptionPlanCurrencyTests.cs` (new) - new plan INR by default, another currency in any case, bad codes refused, list carries currency, update keeps or changes it, change blocked with active subscriptions.
- [x] `src/BannerUI/__tests__/auth/adPricing.test.tsx` - price line now shows the rupee sign (the old check matched by substring and would have passed either way), a USD ad shows the dollar sign, statement amounts and city totals in rupees, a mixed-currency statement line is marked "not in the total".
- [x] `src/BannerUI/__tests__/components/PlanForm.test.tsx` - currency defaults to INR and labels the prices; another currency can be chosen; an edited plan keeps its own code (JPY, not in the short list).
- [x] `src/BannerUI/__tests__/components/SubscriptionPlansTable.test.tsx` (new) - rupees for INR and for a plan that does not say, dollars for USD.
- [x] `demo/e2e-story.mjs`, `demo/take-screenshots.mjs` - the story (42 steps) and the pictures (46) against the rebuilt stack.

### Acceptance Criteria Validation

- ✅ **Ad rates and Ad report show one main menu; a test fails if an admin page wraps itself again**: tests above, and the retaken pictures `admin-06-ad-rates.png` and `admin-10-ad-report.png` show one menu bar.
- ✅ **25 calls to `/api/authentication/me` in a minute from one address succeed; the 21st sign-in is still refused**: live, against the Docker stack with default limits: 25 of 25 answered (200); sign-in attempts then ran 401 x19 and 429 from the 20th (one real sign-in plus 19 attempts = 20 permits, none used by the "who am I" calls). Run with a non-existent address so no real login could be locked.
- ✅ **Plans, Subscription, Subscriptions, Ad rates, Ads and the Monthly statement show money in the same format and currency**: pictures show the rupee sign on Plans, Ad rates, Ads and the statement; no hand-formatted amount is left in the web source; the ad responses carry `currency`.
- ✅ **.NET and web test suites pass; counts before and after**: above.
- ✅ **Story passes twice in a row with no rate-limit wait; pictures taken; changed pictures and Word files rebuilt**: runs `f2cuj` and `f51ov`, 42 of 42, 0 rate-limit lines in both logs; 46 of 46 pictures; the three role Word files rebuilt after the last text change; the end-to-end Word file is from `f51ov`.
- ✅ **Nothing pushed; other bolts' memory-bank folders untouched**: the only memory-bank changes are this bolt's folder and one line in the intent backlog; nothing pushed.

### Issues Found

- None open. Found and fixed during implementation: the administrator's plan API had no currency at all (response, create or update), which is why the Plans page showed a fixed dollar sign; the first version of the statement page warned "not in the total" when an older response lacked the flag (now only on an explicit false).
- `next lint` cannot run: the project has no ESLint configuration (it only offers to create one). The type check is the static check used.
- The new migration was applied to a real SQL Server container holding only the wiped, freshly seeded database; it was not tried on a database that already holds ads (the default INR is in the migration).

### Notes

- Not covered by a render test: the single-plan page (needs the plan hook); it uses the same formatter and passes the type check.
- The statement picture shows tiny hours (0.02 h) because the demo was seeded minutes before; the amounts are right for those hours.
- The word "Payments" appears once in the administrator guide as the name of the setting (`Payments:DefaultCurrency`); no sentence claims payment works.
- The Super Admin login `26384sunilrana@gmail.com` was removed by the database wipe (twice, with your approval); recreate it with `--create-admin` if needed.
- Committed locally in one commit after the user approved completion; `BannerAIProject.sln` and `demo/output/` left out; not pushed.
