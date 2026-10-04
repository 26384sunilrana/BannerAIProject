# Bolt 031 - Admin UI

Status: complete

## Built
- API: `GET /api/admin/subscriptions` (status filter, shop search, paging) on top of the existing admin users and audit-log endpoints.
- Screens (Admin role only, menu entries Users / Subscriptions / Activity):
  - Users: search, include deactivated, per-shop filter, unlock, deactivate (with confirmation), reactivate; an admin cannot deactivate themselves.
  - Subscriptions: status filter, shop search, Reactivate (cut-off shops) or Renew early, "Run renewal job now" with a report.
  - Activity log: date range, failures only, per-user filter, paging.

## Verified
- Domain 443, Application 144, Integration 12 (incl. admin can list, owners cannot), tsc clean, next build OK.
- Jest: 16 new tests pass; the 103 older failures are unchanged (see backlog.md E).
- Chrome against the real API on SQL Server LocalDB: 19/19 checks.

## Not done
- Everything else stays in `memory-bank/intents/002-requirements-gap-closure/backlog.md`.
- `GET /api/subscriptions/{shopId}` returns 404 for a shop without a subscription, which clutters the activity log.
