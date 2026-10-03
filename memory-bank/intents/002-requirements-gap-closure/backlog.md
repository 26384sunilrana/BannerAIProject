# Backlog: what is left

Replaces the old plan, roadmap and pending lists (all earlier bolts are done; see `memory-bank/bolts/`). Last updated 2026-10-04, after bolt 049.
Related: [enterprise-readiness.md](enterprise-readiness.md) (gap table), [hipaa-review.md](hipaa-review.md) (open items 7-15), [decisions.md](decisions.md).

## Next, in order (no outside accounts needed)

1. Browser pairing check of bolt 049 on the compose stack; SQL-mode run of the screen tests; walkthrough steps for Screens.
2. Screen banner text and default-board message for PII (block PII patterns, warn on health wording).
3. Move the browser (Playwright) scripts from the scratchpad into the repo and CI.
4. Bring back the 39 excluded old .NET test files (about 500 compile errors; cover behaviour with HTTP-level tests instead).
5. Data export and deletion for a person; audit-log retention and export.
6. Restore of an old version: keep the last approved version live; restored versions replace component ids; versions hold content only.
7. Accessibility and phone-layout pass.
8. Backup and restore scripts with a drill.
9. Content-Security-Policy.

## Waiting on provider accounts (bolt 044, last)

Real payments (and a real Renew), SMS, email; forgot-password and email verification screens (old endpoints read the signed-in user and cannot work); Azure Blob, Key Vault, Azure SQL; staging, pen test, legal pages. Guide: `docs/operations/providers-setup.md`.

## Known gaps by area

**Accounts and security**
- Access token readable by page script while open; no per-device session list; no "remember me" choice.
- Password hashing PBKDF2 10 000 rounds; raise to 600 000 with a versioned format.
- Nothing limits sessions per login. A role-only check uses the token for up to 15 minutes after a hand-over.
- `Jwt:SecretKey` in `appsettings.json` is a dev placeholder; real secrets come from a secret store.
- A locked-out owner cannot self-renew (needs payments).

**Editor and media**
- Undo does not cover adding/deleting a component or layer changes; no banner-wide background colour.
- Rotation: lists only for pictures and videos; one transition for a whole list; no pause when tab hidden; no reduced-motion option; autoplay with sound needs a kiosk flag (`--autoplay-policy=no-user-gesture-required`).
- Old separate Effects and Carousel API endpoints are unused; remove.
- Media: no thumbnails/resizing, no resume, no folders/tags, no admin view of storage per shop; orphan files are not found by clean-up; fragmented MP4 untested; logo above ~400 KB is not kept offline.
- Calendar is one week at a time. `PublishWorkflow.Publish` does not set `Banner.IsPublished`.

**Pricing and ads**
- Banner-size pricing undefined; no 80% storage alert; downgrade below usage allowed.
- Ads: no statement export or payout marking, hours are scheduled not measured (screen proof of play now exists, not yet used in statements), currency not modelled, no notification history page or opt-out per kind. Details in bolts 040b, 041, 042a.
- Takeover: ads on a closed shop, proof of sale. Details in bolt 036b.

**Location**: old District level unused; owners cannot request a new city/group; no bulk city import; no search by identifier; admin-created shop has no owner login attached.

**Screens (bolt 049)**: one screen per shop; proof of play counted from heartbeats (cap 150 s per gap); nothing tested on Android hardware.

**Quality and delivery**
- Docker images built and run on compose only; nothing on a Kubernetes cluster, no two-pod test, no Prometheus run.
- Real MP4/H.264 playback not tested (no encoder). Azure SQL not tried (LocalDB / SQL container only).
- CI workflow (`.github/workflows/ci.yml`) has not run on GitHub yet.
- Existing databases built from the old model need a baseline migration.
- `GET /api/subscriptions/{shopId}` returns 404 for a shop with no subscription (noisy log).
