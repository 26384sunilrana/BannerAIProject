# Pending work (to do later, as the full product)

**Kept**: 2026-10-02. Everything the delivered bolts listed as "Not done", plus the plan items not started, with
items that were fixed afterwards removed. Source bolt in brackets. Nothing here is lost: it is deliberately left for later.

## A. Requirements not built yet (from the original brief)

| Item | Requirement | Notes |
|---|---|---|
| **Bolt 023** Advertising | 4.14-4.21: ad space as a percentage of the banner, mega advertisement (sides switch for a period), popup ads (size irrelevant), minor ad as a chargeable component, major ad notification to shops by location, charge rules by locality / area / banner size with shop-level override, space returns to the shop banner when an ad ends | Today: an `Advertisement` entity with type, budget, target and metrics exists; no layout, popup, charge rules or notifications |
| Pricing by storage / banner size | 1h: subscription cost based on banner size and storage | Plans carry a storage limit; usage is never measured or enforced |
| Reporting and HIPAA/PHI | 5viii: per-user and overall reporting with HIPAA/PHI in mind | Dashboards, analytics and an audit log exist; no formal HIPAA review, retention or export |

## B. Money, messages and external services (stand-ins today)

- **Payments** [022, 026, 030]: subscribing, renewing and "Renew now" charge nothing (`NoPaymentGateway` reports success). A real provider, refunds, receipts and failed-payment handling are needed.
- **SMS** [030]: only a logging stand-in; no provider.
- **Email** [030]: SMTP sending exists but is off until `Email__Smtp__*` is set. Verification and password-reset emails are not wired into sign-up and reset flows.
- **Azure storage** [028, 025]: media on local disk only; no Blob provider. Encryption keys on a shared file volume; Key Vault protection not wired.

## C. Accounts and security

- Forgot-password, email verification (not enforced at login) and password-reset by email wait for the email provider (bolt 044). The old `verify-email`, `reset-password` endpoints read the signed-in user, so they cannot work for the people who need them; rebuild them with the email work [018b, 026, 036].
- [036 done] Tokens are no longer in local storage: the access token is in memory and the refresh token is an HttpOnly cookie. Left: the access token is still readable by a script running on the page while the page is open (a page-script attack can use the session but cannot copy it away); no per-device list of sessions; no "remember me" choice (always 7 days).
- Shop screen needs one sign-in on its machine; no pairing code or device token; long-offline behaviour untested [030].
- A locked-out owner cannot renew; an admin must reactivate; the admin screen now exists (bolt 031) but there is no self-service path [030].
- No bulk re-encryption of older plain data; audit-log retention and export missing [024].
- `Jwt:SecretKey` in `appsettings.json` is a development placeholder; real secrets must come from a secret store [024].
- [036 done] Owner hand-over (owner with password, or administrator) and moving a sales executive between shops. Left: moving a shop owner or an owner-less shop's users; a hand-over request that the new owner must accept; an access token issued before a hand-over or move keeps its old role/shop claim for up to 15 minutes (server checks use the database for ownership, but role-only checks use the token).

## D. Editor and media

- [037 done] Undo/redo (moves, resizes, settings; 50 steps) and keyboard Ctrl+Z / Ctrl+Y / Ctrl+S. Left: undo does not take back adding or deleting a component or a layer change (those are stored straight away, so history starts afresh after them); no banner-wide background colour (use a full-size shape or picture on the lowest layer with "Fit to banner") [027].
- Restoring a version replaces component ids (carousel / effect links are lost); a restored banner goes off air while awaiting approval instead of keeping the last approved one live; versions hold content only (not schedule or ads) [019].
- Media [034 done: library, delete, clean-up, usage, size and length]: files in a folder are not found by the clean-up if they have no record (no listing in the storage interface); no thumbnails or resizing; uploads cannot resume; storage limit is shown but **not enforced** (bolt 039); an uploaded video with no readable header just has no length; WebM/MP4 with unusual layouts (fragmented MP4, very long headers) are not tested with real encoder output; 4K/8K limited only by the 500 MB cap; no per-shop view of storage for the administrator; no folders/tags.
- Schedule: browser time zone only (shops have no time zone), no calendar view, one window per banner, no repeating daily hours [029].
- Default board for a shop is fixed (name and clock); owners cannot design it [030].
- `PublishWorkflow.Publish` does not set `Banner.IsPublished`; "live" is derived from the workflow [017].

## E. Quality, tests and delivery

- 39 stale test files are still excluded from the .NET test projects (7 of the original 46 were brought back in bolt 032). They target models and APIs that were redesigned (about 500 compile errors); cover their behaviour with HTTP-level integration tests in each area's bolt instead [tests `.csproj` Compile Remove lists].
- API-level tests are thin: the integration project runs 11 smoke tests; most controllers have no HTTP tests [017, 018].
- Docker images were never built (daemon was not running); nothing has run on a cluster; the CI workflow (`.github/workflows/ci.yml`, bolt 032) has not run on GitHub yet [025].
- Existing databases built from the old model cannot take `InitialCreate`; a baseline is needed for any database with data [025].
- User guides in the repo root (ADMIN, SHOP_OWNER, BANNER_CREATOR) describe the old sign-up and screens and need rewriting [018b].

## F. Not yet fully verified

- Real SQL Server was LocalDB only; Azure SQL not tried.
- Video playback of real MP4 files was not tried (test uploads used files with a valid header only).
- Multi-pod behaviour (shared keys and media volumes, the lifecycle job on several pods) was reasoned about and unit tested, not run.

- Location hierarchy leftovers [033]: the older District level is still there and unused by the new screens (cities replace it for placing shops; districts are not shown); an owner cannot ask for a new city or group (they ask the administrator); no bulk import of cities; a shop moved to another group keeps no history; identifiers are shown but there is no search by identifier; creating a shop for an owner (admin) does not yet attach an owner login.
- `GET /api/subscriptions/{shopId}` returns 404 when a shop has no subscription; noisy in the activity log [031].

- **Deliberately last (bolt 044), by decision on 2026-10-02:** real payment / SMS / email providers, forgot-password and email verification, self-service renewal, Azure Blob + Key Vault + Azure SQL, after a full human review. See `roadmap.md`.

- Password storage uses PBKDF2-SHA256 with 10 000 rounds, far below today's guidance (600 000). Raise it with a versioned hash format so existing passwords are upgraded when people next sign in [036].
- Several people share a Sales Executive login in practice? Nothing stops two devices using one login; there is no limit on sessions per login [036].

- Editor and screen, left after bolt 037 [037]:
  - Rotating lists exist for pictures (image components) and videos (video components). Text and shapes cannot hold a list of items that rotate with an effect (requirement 4.7 is met for pictures and videos only).
  - One transition and timing for a whole picture list (no effect per slide); no transition between videos; the entrance effect plays when the banner appears, not again on every rotation.
  - Rotation does not pause when the tab is hidden; no reduced-motion option.
  - Autoplay with sound: a browser blocks it without a click, so the screen falls back to muted. A kiosk browser needs `--autoplay-policy=no-user-gesture-required` (documented in DEPLOYMENT.md).
  - Video lengths come from the file header; files without one (some recorded WebM) play to their end, and "seconds each" still works.
  - The older separate Effects and Carousel API (`/api/banners/{id}/components/{id}/effects`, `/carousels`) is still in the API but nothing uses it: settings now live in the component itself so they are saved, versioned, previewed and approved with the banner. Remove it with the next clean-up of the API.
  - Shown links to the pictures and videos of a rotation are made when the banner opens (one request per file); a very long list means that many requests.

## Left after bolt 038
- (done in 040a) Chrome run of the scheduling screens.
- Restore of an old version should keep the last approved version live; `PublishWorkflow.Publish` IsPublished handling.
- Calendar shows one week at a time; no month view or drag to reschedule.
- A logo above about 400 KB is not kept offline.

## Left after bolt 039
- Banner-size limits or pricing (what "size" means is undecided); per-GB metered billing waits for providers (044).
- Administrator view of storage per shop; alert at 80% by email; downgrade below current usage is allowed.

## Left after bolt 040a
- Notification history page, e-mail/SMS copy (044), opt-out per kind; more producers to come with ads.
- Sign-up allows two shops with the same name (updates only check on rename). Decide whether sign-up should refuse it.

## Bolt 036b (decided 2026-10-03, DONE): shop identity and takeover
- Same name allowed with a different address. Same name + same address = takeover, needs both owners to confirm (old owner and new owner).
- Associates also changing: deactivate old shop, create new shop and new associates. Associates staying: swap owner only and re-verify the approval mechanism with the new owner.
- Builds on bolt 036 (owner hand-over, moving executives) and the notification bell (040a). Needs a decision on how a new owner proves they are the new owner (confirmation by the old owner in the app is the assumption).

## Left after bolt 040b
- See "Not done" in memory-bank/bolts/040b-ad-bookings/bolt.md (rates and statement, override, location notify, HIPAA checks, admin pictures, month view, conservative strip check).

## Left after bolt 041
- See "Not done" in memory-bank/bolts/041-ad-rates-statement/bolt.md (location notify = 041b, HIPAA = 042, no statement export or payout marking, hours are scheduled not measured, currency not modelled).

## Left after bolts 041b and 042a
- 042b: audit retention and export, HIPAA/PHI review of the whole application, re-encrypting old plain data. Screening of banner text and the default-board message is not done.
- 041b: campaign dates are read on the clock of the computer being used.
- Gaps of the ad screening are listed in memory-bank/bolts/042a-ad-compliance/bolt.md.

## Left after bolt 042b
- HIPAA review open items 7-15 in hipaa-review.md (CSP, screening banner text, e-mail encryption, Key Vault, encrypted media, e-mail verification and reset, audit tamper protection, alerts).

## Left after bolt 036b
- See "Not done" in memory-bank/bolts/036b-shop-takeover/bolt.md (ads on a closed shop, proof of sale, Team notice unit test).
