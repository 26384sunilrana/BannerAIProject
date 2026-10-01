# Intent 002: Requirements Gap Closure

**Created**: 2026-10-01T00:00:00Z
**Source**: `.specsmd/OG_DigitalBannerRequirement.md` reviewed against `src/BannerService` and `src/BannerUI`
**Method**: grep and entity inspection of Domain/Application/Presentation. Not a runtime test. Frontend not built.

## Coverage summary

| Req | Requirement | Status | Evidence / gap |
|---|---|---|---|
| 1a | Quarterly / HalfYearly / Yearly billing, equal cost | **Gap** | `BillingPeriod` has only Monthly, Annual |
| 1b | Basic / Silver / Gold / Platinum | Done | `DatabaseSeeder` seeds four plans |
| 1c-d | Auto renewal on/off | **Gap** | No `AutoRenew` field on `Subscription` |
| 1e | SMS + Email renewal reminders | **Gap** | `EmailService` exists, no SMS, no reminder job |
| 1f | Plan change effective next day | **Partial** | `ChangePlan` is immediate; no effective date |
| 1g | 1-week grace with default static banner, then login disabled | **Partial** | `GracePeriod` / `Suspended` status exist; no default banner, no daily reminders, no login lockout wired |
| 1h | Price based on banner size / storage | **Partial** | `MaxStorageGB` on plan; no storage metering |
| 2a | Unique ID per shop owner | **Gap** | Shop has Guid only; no public unique code |
| 2b-e | Group / City / State / Country hierarchy with unique IDs | **Partial** | Country, State, District entities; no Group or City entity; no unique IDs |
| 2f | CRUD for Shop, Group, City, State, Country | **Partial** | `ShopController`, `AddressController`; none for Group/City |
| 3a-i | Register / sign up | Done | `AuthenticationController` |
| 3a-ii/iii | Max 2 active logins per shop, delete and recreate | **Gap** | `MaxUsers` on plan only; not enforced |
| 3a-iii.1 | Approver config (owner / either / both) | **Gap** | `PublishWorkflow` has per-decision reviewer; no shop-level approver setting |
| 3a-2 | Date or content change re-enters approval | **Gap** | Banner has no schedule fields |
| 3a-3 | No current-date banner falls back to local default | **Gap** | No scheduling, no default banner |
| 4.1-4.2 | Components, drag/drop, z-index | Done | Component, LayerManagement, UI canvas (UI untested) |
| 4.3 | Per-component visual effects | Done | EffectService |
| 4.4, 4.7 | Rotating component lists (hero carousel) | Done | CarouselService |
| 4.5-4.6 | Video list: play full or N seconds, mute/volume, short video rule | **Partial** | `VideoComponentProperties` has mute; no rotation seconds / play-full mode |
| 4.8 | Multiple banners, every one approved | Done | PublishWorkflow |
| 4.9 | Preview for all logins | Done | PreviewConfiguration (verify role access) |
| 4.10-4.12 | 10 versions, restore goes through approval, latest preserved | **Partial** | VersionControlService exists; restore-to-approval link not verified |
| 4.13 | Backend data encrypted | **Gap** | No encryption at rest beyond password hashing |
| 4.14-4.18 | Ad space by %, mega ad swap, popup | **Gap** | `Advertisement` has Type/Target; no layout %, no mega swap, no popup mode |
| 4.19-4.21 | Ad charges by locality/area/size, shop-level override, notify shops | **Gap** | No charge rule entity; no notification |
| 5v-vi | No-code publish; same-day hour windows must not overlap | **Gap** | No time-window validation |
| 5viii | User / shop / banner management pages, reporting, HIPAA/PHI | **Partial** | Admin + shop owner dashboards, analytics; no user-management API/UI, no audit log |
| Tech | Next.js, ASP.NET Core, JWT, MSSQL, Docker, K8s | **Partial** | All present except K8s manifests; Dockerfile exists |

## Bolt plan (priority order)

| Bolt | Scope | Closes |
|---|---|---|
| 016-subscription-periods | Quarterly/HalfYearly/Yearly, AutoRenew, next-day plan change | 1a, 1c-d, 1f |
| 017-banner-scheduling | Banner start/end window, overlap validation, re-approval on change, default-banner fallback | 3a-2, 3a-3, 5vi |
| 018-user-limits-approvers | Max 2 logins per shop, delete/recreate, approver settings | 3a-ii/iii |
| 019-restore-approval | Restore version creates pending workflow | 4.10-4.12 |
| 020-video-rotation | Play-full vs N-seconds, volume, short-video rule | 4.5-4.6 |
| 021-location-hierarchy | Group, City entities, unique IDs, CRUD, shop unique ID | 2a-f |
| 022-renewal-lifecycle | SMS + email reminders, daily grace reminders, login lockout, default static banner | 1e, 1g |
| 023-ad-layout-charges | Ad space %, mega swap, popup, charge rules, location notify | 4.14-4.21 |
| 024-security-compliance | Encryption at rest, audit log, user-management API/UI | 4.13, 5viii |
| 025-deployment | K8s manifests, local compose | Tech |
| 026-ui-completion | Build and test BannerUI, wire new APIs | all UI |

Each bolt ends with passing tests in `tests/` and an updated `bolt.md`.
