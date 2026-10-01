---
id: 016-subscription-periods
unit: subscriptions
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
created: 2026-10-01T00:00:00Z
closes: [1a, 1c, 1d, 1f]
---

# Bolt: 016-subscription-periods

## Delivered
- BillingPeriod adds Quarterly and HalfYearly (Annual = Yearly). Prices are pro-rata from the yearly price so every period costs the same per month.
- Subscription.AutoRenew (default true); PUT /api/subscriptions/{id}/auto-renew. RenewalService skips renewals when off.
- Plan upgrade/downgrade is scheduled for the next day (PendingPlanId, PendingPlanEffectiveAt); SubscriptionService.ApplyDuePlanChangesAsync applies due changes.
- Migration 20261001000000_AddSubscriptionAutoRenewAndPendingPlan.
- Tests: tests/BannerService.Domain.Tests/Entities/SubscriptionPeriodTests.cs

## Not done
- ApplyDuePlanChangesAsync is not scheduled yet (needs a hosted service, bolt 022).
- Migrations in this repo have no [Migration] attributes and no model snapshot, so they are not applied by Migrate().
- Tests: SubscriptionPeriodTests pass (Domain suite 387/387).
