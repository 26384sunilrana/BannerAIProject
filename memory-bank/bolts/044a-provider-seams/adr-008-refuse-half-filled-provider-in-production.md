---
bolt: 044a-provider-seams
created: 2026-11-21T09:34:00Z
status: accepted
---

# ADR-008: A half-filled provider stops the application at start-up in Production

## Context

With empty settings meaning "off" (ADR-004), a provider that is named but missing a key or secret could silently fall back to doing nothing. In production that would mean owners cannot pay, or reminders and recovery mails never arrive, with no error anywhere. The application already refuses the placeholder JWT secret in production.

## Decision

In Production, if `Payments:Provider` or `Sms:Provider` names a provider and any required value is empty, the application refuses to start with a clear message naming the missing setting (never its value). `AppUrl` must be https in Production. In Development the same situation logs a warning and falls back to `None`. With the provider set to `None` or left empty there is no error.

## Rationale

A loud failure at deployment is cheaper than a quiet failure in front of customers, and it matches the existing JWT check.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Always fall back to None with a warning | Never blocks start | Silent failure in production | Hides mistakes |
| Fail in every environment | Consistent | Blocks local development and demos | Annoying for developers |
| Check only at first use | No start-up block | Failure appears at the worst moment | Late |

## Consequences

### Positive

- Misconfiguration is found during deployment, not by owners.
- Messages say which setting is missing without exposing secrets.

### Negative

- A typo in one secret blocks the whole API from starting until fixed.

### Risks

- A restart loop in the cluster on a bad config: the readiness probe and the message make the cause easy to find.

## Related

- **Stories**: payment-seam, sms-seam-msg91, provider-status-and-test-buttons
- **Standards**: operations guide (deployment.md) should list the required settings
- **Previous ADRs**: ADR-004
