# Roadmap for the pending work (dependency order)

Written 2026-10-02 from `pending.md`. Section letters (A-F) refer to that file. Bolt numbers continue from 031.

## 1. Dependency map

```
 R0 Safety net ──────────────┬─► everything else (changes are checked by CI and real tests)
 (CI, tests, secrets, baseline)

 R1 Providers ─┬─ Payments ──► R2 self-service renewal, R6 storage pricing, R7 ad charges
 (needs your   ├─ Email    ──► R2 verification / reset, R7 ad notices
  choices)     └─ SMS      ──► R7 ad notices (optional channel)

 R3 Location hierarchy (021) ─┬─► R5 shop time zone ──► schedule fixes
                              ├─► R7 Ads (notify by location, charge by locality)
                              └─► R8 reporting by location

 R4 Media foundation ─┬─ Azure Blob + library + usage count ──► R6 pricing by storage / size
 (storage, video      └─ read video length ──► R5 playlist editor + screen playback

 R5 Editor & screen ──► R7 Ads (ad space % is drawn by the same renderer)

 R7 Ads (023)  ──► R8 Reporting & HIPAA (ad metrics are part of the data)

 R9 Delivery (images, cluster, Azure SQL, multi-pod, guides) ◄── last, once features stop moving
```

Nothing in R0, R3 or R4 needs a decision from outside, so they can start straight away. R1 needs your provider choices.

## 2. Phases

| Phase | Bolts | Scope | Depends on | Size |
|---|---|---|---|---|
| **R0 Safety net** | 032 | GitHub/Azure CI pipeline (build, .NET tests, Jest, tsc); triage the 46 excluded test files (rewrite what still matters, delete the rest); fix or retire the 103 failing Jest tests; move secrets out of `appsettings.json`; baseline migration for databases built from the old model [E] | none | M |
| **R3 Location hierarchy** | 033 | Group and City entities, unique IDs for shops/groups/cities/states/countries, CRUD API and admin screens, shop assigned to a city and group [A, req 2a-f] | R0 | L |
| **R4 Media foundation** | 034 | Blob storage provider behind the existing media interface, media library screen, delete and clean-up, storage used per shop, video length and size read on upload [D, B] | R0 | L |
| **R1 Providers** | 035 | Payment gateway (charge, refund, receipt, failed payment), real email wiring, SMS provider, all behind the existing seams [B] | R0, your provider choices | L |
| **R2 Accounts** | 036 | Forgot password, email verification (enforced), account settings, cookie session instead of local storage, owner transfer and move user between shops, self-service renewal for a locked-out owner [C] | R1 (email, payments) | L |
| **R5 Editor & screen** | 037, 038 | 037: carousel, effects and playlist controls in the editor, screen plays them, undo/redo, background colour. 038: shop time zone, repeat daily hours, calendar view, restore-version fixes, owner-designed default board [A, D] | 037 needs R4 (video length). 038 needs R3 (time zone from city) | L + M |
| **R6 Storage-based pricing** | 039 | Plans priced by storage and banner size, usage measured and enforced, upgrade prompts [A, req 1h] | R4, R1 | M |
| **R7 Advertising** | 040, 041 | 040: ad entities, space %, mega swap, popup, minor ad as chargeable component. 041: charge rules by locality/area/size with shop override, notification to shops by location, space returns when an ad ends [A, req 4.14-4.21] | R3, R1, R5 (renderer), R2 not needed | XL |
| **R8 Reporting & HIPAA** | 042 | Per-user and overall reports (by shop, city, ad), audit retention and export, HIPAA/PHI review and the fixes it finds, re-encryption of old plain data [A, C] | R3, R7 | L |
| **R9 Delivery** | 043 | Build Docker images, run on a cluster, Azure SQL, multi-pod lifecycle job, real MP4 playback check, rewrite the three user guides [E, F] | all of the above | M |

Size: M about 1 day of work here, L 2-3 days, XL 4+ days.

## 3. Recommended order and what can run side by side

1. **032 R0** first, alone (everything after it is checked by the pipeline).
2. **033 location** and **034 media** are independent of each other; do 033 first because ads and time zones wait on it.
3. **035 providers** as soon as you have chosen them; if the choice is slow, do 034 and 037 meanwhile.
4. **036 accounts** straight after 035.
5. **037, 038, 039** in that order (038 needs 033, 039 needs 034 + 035).
6. **040, 041 ads** once 033, 035 and 037 are done. This is the longest and riskiest block, so it is deliberately late.
7. **042 reporting/HIPAA**, then **043 delivery**.

Critical path: 032 → 033 → 037/038 → 040 → 041 → 042 → 043.
Providers (035) are off the critical path unless the choice is delayed past 039.

## 4. Decisions needed from you

| Needed by | Decision | My suggestion |
|---|---|---|
| Before 035 | Payment provider (Razorpay, Stripe, other) and currency/tax rules | Pick by where your shops are; I wire it behind `IPaymentGateway` |
| Before 035 | SMS provider and email sender (SMTP host or a service) | Any with a simple HTTP API |
| Before 034 | Media storage: Azure Blob only, or local disk for small installs too | Both, chosen by config |
| Before 033 | Is "Group" a set of shops chosen by the owner, or fixed by location (the brief says "by location")? | By location, inside a city |
| Before 038 | Shop time zone: set by hand or derived from the city | Derived from the city, editable |
| Before 042 | HIPAA scope: do shops really store patient information in banners? | Treat banner text as non-PHI; keep PHI limits to user and audit data unless you say otherwise |
| Before 040 | Ad rules: who may create an ad (admin only, or paying advertisers)? | Admin only for the first version |

## 5. Rules for every bolt

- Tests first for the domain rule, then API, then screen.
- Chrome check against the real API and SQL Server for every user-facing change.
- `bolt.md` written, `pending.md` trimmed, one commit per bolt.
- Report what was not verified.
