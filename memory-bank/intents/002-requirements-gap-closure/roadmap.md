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
| **R0 Safety net** | 032 (done) | GitHub/Azure CI pipeline (build, .NET tests, Jest, tsc); triage the 46 excluded test files (rewrite what still matters, delete the rest); fix or retire the 103 failing Jest tests; move secrets out of `appsettings.json`; baseline migration for databases built from the old model [E] | none | M |
| **R3 Location hierarchy** | 033 | Group and City entities, unique IDs for shops/groups/cities/states/countries, CRUD API and admin screens, shop assigned to a city and group [A, req 2a-f] | R0 | L |
| **R4 Media foundation** | 034 | Blob storage provider behind the existing media interface, media library screen, delete and clean-up, storage used per shop, video length and size read on upload [D, B] | R0 | L |
| **R1 Providers (LAST)** | 044 | Payment gateway (charge, refund, receipt, failed payment), real email wiring, SMS provider, all behind the existing seams [B] | R0, your provider choices | L |
| **R2 Accounts** | 036 | Cookie session instead of local storage, account settings, owner transfer and move user between shops [C]. Forgot password, email verification and self-service renewal wait for providers (044) | R0 | M |
| **R5 Editor & screen** | 037, 038 | 037: carousel, effects and playlist controls in the editor, screen plays them, undo/redo, background colour. 038: shop time zone, repeat daily hours, calendar view, restore-version fixes, owner-designed default board [A, D] | 037 needs R4 (video length). 038 needs R3 (time zone from city) | L + M |
| **R6 Storage-based pricing** | 039 | Plans priced by storage and banner size, usage measured and enforced, upgrade prompts [A, req 1h]; charging uses the stand-in gateway until 044 | R4 | M |
| **R7 Advertising** | 040, 041, 041b | 040: in-app notifications, ad entities, who may create, ad slots with booking calendar visible to admin, space %, mega swap, popup, minor ad, approval + audit. 041: admin location rates (configurable), monthly hours statement per shop, owner/executive free pricing, owner override of an admin ad + admin notification. 041b: notify shops by location of a major ad, space returns when an ad ends [A, req 4.14-4.21] | R3, R5 (renderer) | XL |
| **R8 Reporting & HIPAA** | 042 | PII/PHI checks on ad content with approval, audit of every ad action; per-user and overall reports (by shop, city, ad), audit retention and export, HIPAA/PHI review and the fixes it finds, re-encryption of old plain data [A, C] | R3, R7 | L |
| **R9 Delivery** | 043 | Build Docker images, run on a cluster, multi-pod lifecycle job, real MP4 playback check, rewrite the three user guides [E, F] | all of the above except 044 | M |
| **Azure + providers (LAST)** | 044 | Real payment, SMS, email; forgot password, email verification, self-service renewal; Azure Blob and Key Vault; Azure SQL. Starts only after the full human review | 043 | L |

Size: M about 1 day of work here, L 2-3 days, XL 4+ days.

## 3. Recommended order

032 safety net → 033 location + 034 media (local disk) → 036 accounts (no email parts) → 037 editor → 038 time zone and schedule → 039 pricing by storage → 040, 041, 041b ads → 042 reporting and HIPAA → 043 delivery → human review → 044 providers and Azure.

Critical path: 032 → 033 → 037/038 → 040 → 041 → 042 → 043 → 044.

## 4. Decisions (answered 2026-10-02)

| Area | Decision |
|---|---|
| Providers (payments, SMS, email) | None chosen. **Moved to the very end** (bolt 035 becomes the last feature bolt). Until then the stand-ins stay. Anything that needs real email (verification, reset mail) waits with it. |
| Location (033) | A Group is fixed by location, inside a city. |
| Media (034) | Local disk now. Azure Blob is last, after a full human review. |
| Time zone (038) | Derived from the city, editable. |
| Who creates ads (040) | Admin, shop owner, and a sales executive of that shop. Advertisers are not users; they contact the owner or the admin. |
| Ad charges | **Admin ad:** price comes from location-based rates the admin configures. The shop is paid per month for the hours the ad ran on its screen (a monthly statement; no gateway needed). **Owner/executive ad:** the shop prices it as it likes; the platform does not track payment. |
| Ad slot booking | A slot booked by an owner or executive (with owner approval) shows to the admin as booked for that time, so the admin plans around it. |
| Override | If an admin has booked a slot and the owner gets a better local price, the owner may override the admin ad. The admin is notified inside the application. This needs an in-app notification feature (new, not in the old plan). |
| HIPAA / PII (042) | Ads must not carry PHI or PII. Ad content goes through validation and approval, and every create / approve / override / change is in the audit log. |

## 5. Rules for every bolt

- Tests first for the domain rule, then API, then screen.
- Chrome check against the real API and SQL Server for every user-facing change.
- `bolt.md` written, `pending.md` trimmed, one commit per bolt.
- Report what was not verified.
