---
id: 018-team-limits-approvers
unit: banner-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [3a-ii, 3a-iii, 3a-iii.1]
---

# Bolt: 018-team-limits-approvers

## Delivered
- ShopTeamService + /api/shops/{shopId}/team: owner adds up to 2 sales executive logins (needs a live subscription), removes one and adds another, and chooses approvers (owner, either executive, or both; never none).
- Shop.OwnerIsApprover / ApproverUserIds / CanApprove; publish workflow approve and reject (and approval requests) now enforce it, returning 403 otherwise.
- Interpretation: "2 active logins" is read as 2 sales executives in addition to the owner.

## Security and wiring fixes found on the way
- ShopContextMiddleware ran before authentication and rejected every request (login included) with 401. Order fixed; the middleware only sets the shop context for authenticated callers.
- JWT carried the claim "ShopId" while all controllers and the middleware read "shop_id". Token now has both.
- Eight controllers (admin dashboard, plans, subscriptions, invoices, advertisements, analytics, publish workflow, shop dashboard) had no [Authorize]. Added; admin dashboard and plan management require the Admin role.
- Advertisement, Analytics and PublishWorkflow controllers trusted userId/userName request headers. They now read the identity from the token.

## Not done
- Route shopId on several controllers is still not compared to the token's shop (cross-tenant access for authenticated users). Planned in 024.
- Self-registration gives every user the SalesExecutive role and does not create a shop or owner; owner onboarding is still missing.
- Migration 20261001020000_AddShopApproverSettings has no [Migration] attribute (same as the others).
- Not exercised end to end against a running API and SQL Server.
