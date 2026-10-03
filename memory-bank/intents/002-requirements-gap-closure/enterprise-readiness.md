# Enterprise readiness: what is built, what is missing (assessment of 3 Oct 2026)

Status words: **Done** (built and tested), **Partial**, **Missing**. "Needs you" = I cannot do it without an account, a decision or a person.

## 1. Money and providers
| Item | State | Needs you |
|---|---|---|
| Real payments, checkout, refunds, receipts | Missing (stand-in gateway) | Provider account, rules, GST details (see PROVIDERS_SETUP_GUIDE) |
| Self-service renewal, auto-renew charging | Missing | Same |
| Real e-mail (reminders) | Done, needs SMTP settings | SMTP account |
| Forgot password, verify e-mail screens | Partial (old endpoints, no screens) | SMTP |
| SMS, one-time codes | Missing (stand-in) | SMS provider and India DLT |
| GST/tax invoices as PDF, credit notes | Missing | GSTIN, rates, invoice format |

## 2. Security
| Item | State |
|---|---|
| Role and shop isolation, audit log, export, retention, field encryption, password hashing, headers, cookie sessions | Done |
| **Brute-force and abuse limits per IP / per account on sign-in, sign-up and uploads (rate limiting)** | **Missing** (only per-account lockout) |
| **Two-step sign-in (TOTP) for administrators and owners** | **Missing** |
| CAPTCHA or e-mail check on sign-up (stops fake shops) | Missing |
| Content-Security-Policy | Missing (needs testing) |
| Screening of banner text, uploaded pictures (adult/violent/PII) | Missing (ads only) |
| Single sign-on (Microsoft/Google) for staff | Missing (optional) |
| Secrets and keys in a vault, storage-side encryption | Missing (Azure, bolt 044) |
| Dependency and container vulnerability scanning in the pipeline | Missing |
| Independent penetration test | Missing — needs you (a vendor) |
| Right to export and delete a person's or shop's data | Missing |

## 3. Reliability and operations
| Item | State |
|---|---|
| Containers build and run; migrations as a job; manifests render | Done |
| **Health checks (live/ready), metrics, tracing, alerts (failed sign-ins, errors, jobs not running, disk full)** | **Missing** (only a log) |
| Central log shipping and retention | Missing |
| **Backups and a tested restore of database, key ring and media** | **Missing** (documented, never drilled) |
| Staging environment, blue/green or rolling deploy with rollback | Missing |
| Infrastructure as code (Bicep/Terraform), container registry, pipeline run on GitHub | Partial (workflow written, never run) |
| Two or more API pods, load balancing, session and job behaviour | Not verified |
| **Load and soak tests** (many shops polling every 30 s, large uploads) | **Missing** |
| Media served from a CDN, thumbnails, video transcoding/resizing | Missing |
| Live push to screens instead of polling | Missing (30-second poll is fine to a few thousand screens) |

## 4. The shop screen as a managed device
| Item | State |
|---|---|
| Plays banners, ads, default board, offline fallback | Done (browser) |
| **Screen pairing by code instead of signing a person in on the TV** | **Missing** — today the TV holds an owner's login |
| **Heartbeat, "last seen", offline alert to the owner, remote refresh** | **Missing** |
| **Proof of play (what played, when, how long) and reports for advertisers** | **Missing** (hours are scheduled, not measured) |
| Several screens per shop, groups of screens | Missing (one screen per shop) |
| Kiosk app / hardware guidance (Android TV, Raspberry Pi, Windows kiosk), real MP4/H.264 playback check on the target device | Missing — needs a device |

## 5. Product depth
| Item | State |
|---|---|
| Banner editor, effects, rotation, schedule, calendar, approvals, ads, rates, statements, takeover, notifications | Done |
| Restore an old banner version without breaking the live one | Partial (bug noted) |
| Banner templates and a shared template library | Missing |
| Bulk actions, search and filters across banners and ads | Partial |
| Notification preferences, daily digest by e-mail | Missing |
| Granular permissions (beyond admin/owner/executive), several admins with limited rights | Missing |
| Support tools: look at a shop "as" the owner (audited), ticket link | Missing |
| Reports: advertiser report, city and shop dashboards with charts, CSV export of statements | Partial |
| Multi-language and currency, regional date formats | Missing (English, plain numbers) |
| Marking a statement as paid, payout records | Missing |

## 6. Quality
| Item | State |
|---|---|
| Unit, integration (also on real SQL), UI tests: ~1,900 | Done |
| **The browser (end-to-end) scripts live outside the repository and are not in the pipeline** | **Missing** — I will move them in |
| 39 older .NET test files still switched off | Open |
| Accessibility check against WCAG 2.1 AA (keyboard, contrast, screen reader) | Missing |
| Phone/tablet layout check, other browsers (Edge, Firefox, Safari) | Missing |
| Performance budget for the web app | Missing |

## 7. Legal and process (people, not code)
Terms of service, privacy policy, cookie notice, refund policy, data-processing agreement with providers, a named security officer, a breach procedure,
a support and escalation process, staff training for administrators, and an agreed service level.

## 8. Recommended order
1. **Now, no outside accounts needed (I can do these):** abuse protection (rate limiting) and two-step sign-in; health checks, metrics and alerts; end-to-end suite into the repository and into the pipeline;
   screen pairing, heartbeat and offline alerts; proof of play; data export and deletion; restore-version fix; accessibility and responsive pass; the 39 old tests; backup and restore scripts with a drill.
2. **With your accounts:** e-mail (forgot password, verify), payments and renewal, SMS, then Azure (storage, vault, database, hosting) and the cloud deployment with staging.
3. **With other people:** penetration test, accessibility audit, legal pages, load test on the real environment, hardware trial of the screen on the actual TVs.
