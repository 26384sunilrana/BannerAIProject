---
last_updated: 2026-11-21T09:35:00Z
total_decisions: 8
---

# Decision Index

This index tracks all Architecture Decision Records (ADRs) created during Construction bolts.
Use this to find relevant prior decisions when working on related features.

## How to Use

**For Agents**: Scan the "Read when" fields below to identify decisions relevant to your current task. Before implementing new features, check if existing ADRs constrain or guide your approach. Load the full ADR for matching entries.

**For Humans**: Browse decisions chronologically or search for keywords. Each entry links to the full ADR with complete context, alternatives considered, and consequences.

---

## Decisions

### ADR-001: Multi-Tenant Data Isolation at Repository Layer
- **Status**: accepted
- **Date**: 2026-09-26
- **Bolt**: 001-banner-service (Banner Service)
- **Path**: `docs/archive/bolts/001-banner-service/adr-001-multitenant-isolation.md`
- **Summary**: The Banner Service operates in a multi-tenant environment where each shop must be isolated. We enforce isolation at the repository layer via request-scoped ShopContext and query-time filtering.
- **Read when**: Building any service that operates in multi-tenant environment; designing data access layers; working on user/tenant authentication flows; implementing queries that must filter by shop/tenant context

### ADR-002: JSON Storage for Polymorphic Component Properties
- **Status**: accepted
- **Date**: 2026-09-26
- **Bolt**: 001-banner-service (Banner Service)
- **Path**: `docs/archive/bolts/001-banner-service/adr-002-json-component-properties.md`
- **Summary**: Components on a banner have different properties based on type (Text, Image, Video, Graphics). We store component-specific properties as JSON in a single column, with type safety enforced at the application layer.
- **Read when**: Working on polymorphic data structures; implementing component systems with variable properties; adding new component types; designing extensible schemas; storing heterogeneous data in relational databases

### ADR-003: Component Layering (ZIndex) Design
- **Status**: accepted
- **Date**: 2026-09-26
- **Bolt**: 001-banner-service (Banner Service)
- **Path**: `docs/archive/bolts/001-banner-service/adr-003-zindex-layering.md`
- **Summary**: Components must be layered on a banner with rendering order determined by ZIndex. We use integer ZIndex (0-100) with uniqueness enforced at the application layer and database unique index for data integrity.
- **Read when**: Implementing component layering or depth management; designing preview/rendering systems; working on effects that depend on component order; managing z-order in visual composition tools

### ADR-004: Providers are swappable adapters chosen by settings, with "None" as a real option
- **Status**: accepted
- **Date**: 2026-11-21
- **Bolt**: 044a-provider-seams (intent 002-requirements-gap-closure)
- **Path**: `bolts/044a-provider-seams/adr-004-provider-adapters-chosen-by-settings.md`
- **Summary**: Payments, SMS and e-mail need outside providers and the owner has no accounts yet, and the standards say nothing about integrations. Each provider area gets a domain port, one adapter per provider, chosen at start-up from settings, with "None" as today's behaviour and empty settings always meaning off.
- **Read when**: Adding or changing a payment, SMS or e-mail provider; adding any third-party integration; deciding where country, currency or provider names may appear in code

### ADR-005: Auto-renew is ignored and hidden while a real payment provider is configured
- **Status**: accepted
- **Date**: 2026-11-21
- **Bolt**: 044a-provider-seams (intent 002-requirements-gap-closure)
- **Path**: `bolts/044a-provider-seams/adr-005-auto-renew-ignored-with-real-provider.md`
- **Summary**: Subscriptions have an auto-renew switch, but charging saved cards or mandates is out of scope and untested. While a real provider is configured the renewal job ignores the switch and the Subscription screen hides it; owners pay by checkout.
- **Read when**: Working on subscription renewal, the renewal job, saved-card or mandate payments, or the Subscription screen

### ADR-006: "Paid" comes only from one idempotent reconciliation; the webhook is the source of truth
- **Status**: accepted
- **Date**: 2026-11-21
- **Bolt**: 044a-provider-seams (intent 002-requirements-gap-closure)
- **Path**: `bolts/044a-provider-seams/adr-006-paid-only-through-idempotent-reconciliation.md`
- **Summary**: A payment can be reported twice, by the browser and by a webhook that may repeat. Both feed one reconciliation that verifies the signature, records a unique receipt, checks amount and currency, and pays and renews in one transaction, backed by a unique paid-per-invoice index.
- **Read when**: Touching payment, invoice-paid, refund or webhook code; adding a payment provider; investigating a double or missing payment

### ADR-007: Recovery tokens are hashed, single-use and anonymous; a reset ends all sessions
- **Status**: accepted
- **Date**: 2026-11-21
- **Bolt**: 044a-provider-seams (intent 002-requirements-gap-closure)
- **Path**: `bolts/044a-provider-seams/adr-007-hashed-single-use-recovery-tokens.md`
- **Summary**: Forgot-password never sent mail and reset required a signed-in user, with tokens stored as plain text. Tokens are now random, stored only as SHA-256 hashes with an expiry, work once, identify the person without a session, and a reset signs the person out everywhere.
- **Read when**: Working on password reset, e-mail verification, sessions, sign-in security, or anything that issues one-time links

### ADR-008: A half-filled provider stops the application at start-up in Production
- **Status**: accepted
- **Date**: 2026-11-21
- **Bolt**: 044a-provider-seams (intent 002-requirements-gap-closure)
- **Path**: `bolts/044a-provider-seams/adr-008-refuse-half-filled-provider-in-production.md`
- **Summary**: A provider that is named but missing a key could silently do nothing in production. The application refuses to start in Production with a clear message (never the value), and falls back to None with a warning in Development.
- **Read when**: Changing start-up checks, deployment settings or secrets handling; adding settings for a new provider
