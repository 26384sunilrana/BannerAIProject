---
id: 051-app-fixes
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
current_stage: complete
stages_completed:
  - name: plan
    completed: 2026-11-21T12:29:30Z
    artifact: implementation-plan.md
  - name: implement
    completed: 2026-11-21T13:07:44Z
    artifact: implementation-walkthrough.md
  - name: test
    completed: 2026-11-21T13:43:32Z
    artifact: test-walkthrough.md
stories: [one-page-frame, auth-limit-me, one-currency]
started: 2026-11-21T12:21:37Z
created: 2026-11-21
requires_bolts: [050-demo-refresh]
enables_bolts: []
blocks: false
---

# Bolt 051 - Three application fixes found while refreshing the demo

**Goal**: remove three problems that bolt 050 found (see `memory-bank/bolts/050-demo-refresh/drift-list.md`, section E), and show it with the demo: the story runs without waiting for the rate limit, and the pictures that changed are retaken.

## Stories

### 1. one-page-frame: the menu bar shows twice on Ad rates and Ad report
**Cause (read in the code)**: `app/admin/layout.tsx` already puts every page under `/admin` inside the page frame (`AppShell`, administrators only). `app/admin/ad-rates/page.tsx` and `app/admin/ad-reports/page.tsx` put themselves inside a second `AppShell`, so the frame is drawn inside the frame. No other page under `/admin` does this.
**Fix**: take the second `AppShell` out of those two pages.
**Guard**: a web test that fails when any page under `app/admin` uses `AppShell` itself, and a render test that shows exactly one main menu on both pages.

### 2. auth-limit-me: quick navigation is refused with 429 and a one-minute lockout
**Cause (read in the code)**: `RateLimitRules.Classify` sends every path under `/api/authentication` (except `refresh`) to the strict class: 20 a minute per IP address, meant for sign-in and sign-up. `GET /api/authentication/me` is in that class, and `VerifyEmailBanner` (inside `AppShell`) calls it every time a page mounts. Each page wraps its own `AppShell`, so every click on a menu link makes one call. About 20 clicks in a minute from one address, or a few people behind one address, and sign-in, sign-up and the pages themselves are refused for the rest of the minute.
**Fix (recommended, option A)**: classify `GET /api/authentication/me` with everything else (`Api`: 1200 a minute, counted per signed-in user). The call needs a valid sign-in to answer at all, so it cannot be used to guess passwords; sign-in, sign-up, two-step, forgot-password, reset-password, verify-e-mail and resend stay at 20 per address.
**Not in this bolt unless you ask (option B)**: make the page ask `/me` once per session instead of once per page. It would also cut the traffic, but a banner that shows "verify your e-mail" would then not clear itself after the person verifies in another tab.
**Tests**: add `/api/authentication/me` (GET, `Api`) to the existing class test and keep `/api/authentication/login` in `Authentication`; an integration test that 25 calls to `/me` in a minute are all answered while the 21st sign-in is still refused.
**Docs**: `docs/operations/deployment.md` (rate limits line) and `docs/operations/runbook.md` (guessing passwords) say what is counted.

### 3. one-currency: `$` on Plans, `₹` on Subscription, plain numbers for ads
**What is there**: `lib/pricing.ts` already has `formatMoney(value, currency = 'INR')` and the Subscription pages use it with the plan's currency. The plan response already carries `currency` (default INR; the platform default is the setting `Payments:DefaultCurrency`, decision of 2026-11-21: India first, currency a setting). The admin Plans table (`components/admin/SubscriptionPlansTable.tsx`) and plan page (`app/admin/subscription-plans/[planId]/page.tsx`) write a literal `$`. Ad prices (Ad rates, Ads, Monthly statement) are numbers with two decimals and no currency, because the ad responses have no currency.
**Proposal: one display, `formatMoney`, everywhere money is shown.**
- Plans (table and plan page): `formatMoney(price, plan.currency)`, so `₹29.99` like the owner sees.
- Ads, option 1 (recommended): the ad rate, ad and statement responses get a `currency` field, filled from `Payments:DefaultCurrency` when read (no database change), and the three screens use `formatMoney`. Result: `₹20.00 an hour`, `paid ₹1,240.00`, rates in the same currency as plans, and a country that sets another currency changes all of it.
- Ads, option 2 (smaller, UI only): the three ad screens call `formatMoney(value)` with the INR default. Same look today, but a country with another currency would show rupee signs on ad prices.
**Both options**: the Monthly statement's `money()` helper is replaced by `formatMoney`; amounts keep two decimals.
**Known limit**: an ad keeps the price it was booked at; if the platform currency is ever changed, old prices would be shown in the new one. Recorded in the backlog, not solved here (currency is still not stored per ad).

## Deliverables
1. Web: the two pages without a second frame; the guard test; plans and ad screens with `formatMoney`; updated web tests (`adPricing.test.tsx` expects `50.00 an hour`, the admin plans tests).
2. API: the rate-limit class for `/me`; the extended class test and integration test; with option 1, a `currency` field on the three ad responses and its tests.
3. Docs: the two operations pages; the guides and walkthroughs where a price is written (checked, changed only if they quote an amount or a sign).
4. Demo: the story and picture script run against rebuilt images; no "refused by the rate limit" line in the story log; pictures retaken (Ad rates, Ad report, Plans, Ads, Statement, and any other that changes); Word files rebuilt; the comments in the scripts about the 20-a-minute limit corrected (the retries stay as a safety net).
5. Bolt 050's `drift-list.md` is not touched (other bolt): the result is written in this bolt's `result-note.md`, which quotes section E of the 050 list as closed.

## Dependencies
- Docker stack rebuilt (`docker compose up --build -d`): the API and web images change. The database does not need wiping, but retaken pictures show the shops that story runs made unless the database is wiped first (the permission rule you added for `docker compose down -v` is still in place; wiping removes the Super Admin login again).
- `demo/` scripts from bolt 050 (committed, `3c7ae40`).

## Acceptance criteria
- [ ] Ad rates and Ad report show one main menu; a test fails if an admin page wraps itself in the frame again.
- [ ] 25 calls to `/api/authentication/me` in a minute from one address all succeed; the 21st sign-in in that minute is still refused with 429.
- [ ] Plans (table and plan page), Subscription, Subscriptions, Ad rates, Ads and the Monthly statement show money in the same format and currency.
- [ ] .NET and web test suites pass (counts before and after in the test report).
- [ ] `node demo/e2e-story.mjs` passes twice in a row with no rate-limit wait in the log; `node demo/take-screenshots.mjs` takes every picture; changed pictures and Word files are retaken and rebuilt.
- [ ] Nothing pushed; other bolts' memory-bank folders untouched.

## Risks
- Changing which class `/me` belongs to is a security setting. It is safe only because the call needs a valid token; the test proves sign-in is still limited.
- Per-user counting of `/me` means one signed-in person can make 1200 calls a minute, the same as any other API call.

## Decisions (answered by the user, 2026-11-21)
1. **Rate limit**: option A only. `/me` is counted with the normal API class; the page keeps asking once per page mount (option B is not in this bolt).
2. **Currency for ads**: option 1. The ad rate, ad and statement responses carry `currency`, filled from `Payments:DefaultCurrency`.
3. **Pictures**: the pictures are sample pictures, so they are retaken on a freshly wiped database with only the demo data (`docker compose down -v`, administrator created, demo seeded, pictures first, then the story). The wipe removes the Super Admin login again; it is recreated afterwards with `--create-admin` if wanted.
4. **Bolt type**: `simple-construction-bolt` (Plan, Implement, Test), taken as planned because no other answer was given.

Rules for this bolt: do not push; do not touch the memory-bank of other bolts; stop at each stage checkpoint for approval.
