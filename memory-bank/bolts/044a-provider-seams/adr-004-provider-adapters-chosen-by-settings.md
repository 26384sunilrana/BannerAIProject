---
bolt: 044a-provider-seams
created: 2026-11-21T09:30:00Z
status: accepted
---

# ADR-004: Providers are swappable adapters chosen by settings, with "None" as a real option

## Context

Payments, SMS and e-mail all need outside companies, and the owner has no accounts yet. The application is India-first (INR, +91, Razorpay, MSG91 with DLT) but must work for other countries later. Two stand-ins already exist (`NoPaymentGateway`, `LoggingSmsSender`) and e-mail already sends through SMTP when configured.
The standards (tech-stack, data-stack) say nothing about third-party integrations.

## Decision

Each provider area has a contract in the domain (a port). Each provider is one adapter in Infrastructure. Settings name the provider (`Payments:Provider`, `Sms:Provider`); the choice is made once at start-up through dependency injection. `None` is a first-class provider: it is today's behaviour (Renew click, SMS and mail not sent). A provider is "configured" only when all its required settings are non-empty, and an empty setting always means off. Country, currency and phone default are settings, never constants.

## Rationale

The same shape as the existing stand-ins, so nothing is thrown away. It keeps provider names out of the domain and lets tests use a fake provider, which also proves that a second provider plugs in without changing the domain.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Call the provider's SDK directly from services | Fastest to write | Locks the domain to one provider; hard to test; rupees and country leak everywhere | Fails the international-flexibility requirement |
| Several providers active at once, chosen per shop or country | Most flexible | Routing rules, per-shop credentials, much more testing | Not needed now; can be added behind the same port |
| Build only the stand-ins and wait for accounts | No risk of wrong adapter | Going live needs real engineering; owner's request was "only values to add" | Does not meet the goal |

## Consequences

### Positive

- Going live is filling in settings.
- A new provider is one adapter plus one front-end checkout driver.
- With all settings empty the app behaves exactly as before.

### Negative

- The contract must be general enough for several providers, so some provider-specific features are out of reach until the port grows.
- Adapters are first tested against fakes, not live accounts.

### Risks

- A provider's real behaviour differs from its documentation: the first live check is part of bolt 044, and the adapters are small to fix.

## Related

- **Stories**: payment-seam, sms-seam-msg91, provider-status-and-test-buttons
- **Standards**: candidate for tech-stack.md ("third-party providers sit behind ports")
- **Previous ADRs**: none directly; follows the layering in ADR-001
