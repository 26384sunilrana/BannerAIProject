# Maintenance Log

## 2026-11-21T08:38:13Z - Status Sync

**Triggered by**: analyze-context integrity check (user-approved fix), on branch testVibeCode

| Artifact | Old Status | New Status | Reason |
|----------|------------|------------|--------|
| bolts/031..049 (19 stubs, except 043) | no status field | status: complete (frontmatter added) | Each record's text says complete |
| bolts/043-delivery | no status field | status: in-progress | Record says "mostly complete"; Kubernetes cluster run, two API pods and real MP4 playback are not done |
| intents/002-requirements-gap-closure/requirements.md | missing | created, status: construction | Intent is open: backlog items and bolt 044 remain |
| intents/002-requirements-gap-closure/enterprise-readiness.md | undated as stale | review note added | Rows predate bolts 046-049 |

**Not changed on purpose**
- Units 001-005 of intent 002 stay `complete`: all bolts they list (016-030) are complete. They do not cover bolts 031-049.
- No bolt 044 record yet; it is planned last (providers).

---

## 2026-11-21T14:20:00Z - Migration check on a database with data

**Triggered by**: pre-push review of bolts 044a, 050 and 051, on branch testVibeCode (user-approved)

A scratch database in the compose SQL Server container was built at migration `AddAdminManagement` (the last one on GitHub), filled with a shop, a user with outstanding reset and verification tokens, a plan, an ad rate and two ads (one priced), then brought forward with the two newer migrations (`AddProviderSeams`, `AddAdCurrency`) by the generated SQL script. The scratch database was dropped afterwards; the demo database was not touched.

| Check | Result |
|-------|--------|
| Both migrations apply without error | yes |
| Ads, rate, plan and user rows kept | yes (2 ads, 1 rate, 1 plan, 1 user) |
| Currency on existing ads and rates | INR on every row, none empty |
| Amounts untouched | admin ad still 20.00, rate 100.00 at 70%, plan 79.99 |
| Outstanding password-reset and verification tokens | cleared (NULL), as ADR-007 intends: people ask for a new link |
| New tables | PaymentAttempts, WebhookReceipts, Refunds created |

Not tried: a database with data from before `AddAdminManagement`, or Azure SQL.

---
