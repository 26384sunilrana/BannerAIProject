# Pending work (to do later, as the full product)

**Kept**: 2026-10-02. Everything the delivered bolts listed as "Not done", plus the plan items not started, with
items that were fixed afterwards removed. Source bolt in brackets. Nothing here is lost: it is deliberately left for later.

## A. Requirements not built yet (from the original brief)

| Item | Requirement | Notes |
|---|---|---|
| **Bolt 023** Advertising | 4.14-4.21: ad space as a percentage of the banner, mega advertisement (sides switch for a period), popup ads (size irrelevant), minor ad as a chargeable component, major ad notification to shops by location, charge rules by locality / area / banner size with shop-level override, space returns to the shop banner when an ad ends | Today: an `Advertisement` entity with type, budget, target and metrics exists; no layout, popup, charge rules or notifications |
| Pricing by storage / banner size | 1h: subscription cost based on banner size and storage | Plans carry a storage limit; usage is never measured or enforced |
| Reporting and HIPAA/PHI | 5viii: per-user and overall reporting with HIPAA/PHI in mind | Dashboards, analytics and an audit log exist; no formal HIPAA review, retention or export |
| Hero / carousel in the editor and screen | 4.4, 4.7: rotating background components | Backend carousel exists; editor has no controls and the screen does not play it |
| Visual effects in the editor | 4.3: effect per component | Backend effects exist; editor has no controls |
| Video playlists in the editor | 4.5-4.6 | Backend (bolt 020) done; no editor controls, screen does not play playlists, durations are not read from uploaded videos |

## B. Money, messages and external services (stand-ins today)

- **Payments** [022, 026, 030]: subscribing, renewing and "Renew now" charge nothing (`NoPaymentGateway` reports success). A real provider, refunds, receipts and failed-payment handling are needed.
- **SMS** [030]: only a logging stand-in; no provider.
- **Email** [030]: SMTP sending exists but is off until `Email__Smtp__*` is set. Verification and password-reset emails are not wired into sign-up and reset flows.
- **Azure storage** [028, 025]: media on local disk only; no Blob provider. Encryption keys on a shared file volume; Key Vault protection not wired.

## C. Accounts and security

- Forgot-password, email verification (not enforced at login), account settings screens [018b, 026].
- Tokens kept in browser local storage; a cookie-based session is safer against script injection [026].
- Shop screen needs one sign-in on its machine; no pairing code or device token; long-offline behaviour untested [030].
- A locked-out owner cannot renew; an admin must reactivate; the admin screen now exists (bolt 031) but there is no self-service path [030].
- No bulk re-encryption of older plain data; audit-log retention and export missing [024].
- `Jwt:SecretKey` in `appsettings.json` is a development placeholder; real secrets must come from a secret store [024].
- Self-registered users cannot be moved to another shop or have an owner transferred; no owner change flow [018].

## D. Editor and media

- Undo/redo buttons inert; no background colour (the API has none) [027].
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
