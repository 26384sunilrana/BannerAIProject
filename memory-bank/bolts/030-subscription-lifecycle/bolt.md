---
id: 030-subscription-lifecycle
unit: subscription-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-02T00:00:00Z
closes: [1e, 1g, shop screen, default banner]
---

# Bolt: 030-subscription-lifecycle (plan item 022)

## What happens now
- An hourly job (and `POST /api/subscriptions/admin/run-lifecycle` on demand) drives every subscription:
  - auto renewal on: charged on the date (through a payment gateway abstraction); a failed charge starts grace;
  - auto renewal off: reminders by email and text 7, 3 and 1 days before;
  - date passed unpaid: one week of grace with a daily reminder; the shop shows only its default banner; with auto renewal the charge is retried daily;
  - grace over: expired, the shop's refresh tokens are revoked and its logins are refused (also at refresh); platform admins are exempt.
- Each message is claimed in a table with a unique (subscription, kind, channel, day) index, so repeats and several servers never send twice.
- "Renew now" is a real manual renewal (adds to the current period when early, starts today when late) and is how an admin reactivates an expired shop. The old endpoint ran the automatic job's rules and refused when auto renewal was off.
- Email is sent by SMTP when configured; without it nothing is sent and the attempt is recorded as not delivered (it used to print the message, tokens included, to the console and report success).

## UI
- Owner dashboard and subscription page explain the state: ending soon, grace week (and when logins stop), logins off; Renew now / Renew early.
- /display: the shop screen. Plays the live banner scaled to the screen (images, videos, text, shapes), checks every 30 s, refreshes media links, and falls back to the default board (shop name, clock, a note why) when nothing is scheduled, the plan has ended, the session ended, or after a restart with no sign-in; the shop name is kept on that machine.

## Verification
- Chrome against the real API on SQL Server: 20 lifecycle checks (subscribe, live banner on the screen, reminders recorded once per channel and day, warning, grace, screen switches to default board by itself, sign-in still works in grace, expiry, sign-in refused with the reason, default board survives a restart, reactivation, banner back) plus schedule 13, run 28, editor 18, media 11.
- Application 144 tests (lifecycle 20, lock-out 9), Domain 440, Integration 11 (one runs the lifecycle through the real wiring); Jest suite for these areas passes.

## Not done
- SMS: only a logging stand-in; no provider.
- Payments: stand-in that always succeeds; no money is taken.
- Email needs SMTP settings.
- A locked-out owner cannot renew themselves (as specified: logins are disabled); an admin must reactivate, and there is no admin screen for it yet (API only).
- The default board is fixed (shop name and clock); owners cannot design it.
- The screen needs one sign-in on its machine; there is no pairing code or device token, and the screen's access token lifetime/refresh handling after long offline periods is untested.
- Carousel/hero rotation of components is not played by the screen.
