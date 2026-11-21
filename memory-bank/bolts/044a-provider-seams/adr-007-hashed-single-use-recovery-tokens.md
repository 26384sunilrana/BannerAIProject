---
bolt: 044a-provider-seams
created: 2026-11-21T09:33:00Z
status: accepted
---

# ADR-007: Recovery tokens are hashed, single-use and anonymous; a reset ends all sessions

## Context

Forgot-password never sent its mail, and reset-password required a signed-in user, which a person who forgot the password cannot be. Verification tokens had the same problems. Tokens were stored as plain text. The application must not leak whether an address has an account (HIPAA/PII review, bolt 042b) and already has sign-in abuse limits (bolt 046).

## Decision

A recovery token is 32 random bytes (base64url). Only its SHA-256 hash is stored, with an expiry (reset: 1 hour; e-mail verification: 24 hours). The token alone identifies the person, so reset and verify need no session. A token works once. A successful reset ends all of the person's sessions. Forgot-password gives the same answer whether or not the address exists. The reset page removes the token from the address bar at once.

## Rationale

A database leak or log must not give attackers working reset links. Hashing a high-entropy random value is enough; no slow hash is needed.

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Keep plain tokens | No change | A database read gives account takeover | Weak |
| Signed tokens (JWT) with no storage | No lookup | Cannot be made single-use or revoked without storage | Weak |
| Require the user id in the link | No hash lookup | Exposes ids; needs matching logic | Unnecessary |

## Consequences

### Positive

- A stolen database cannot be used to reset passwords.
- No account enumeration through this flow.

### Negative

- Plain tokens already stored stop working (they lived one hour).
- One more index on the users table.

### Risks

- Mail delivery timing could hint at account existence: accepted and noted; the existing rate limits bound probing.

## Related

- **Stories**: account-recovery-screens
- **Standards**: security notes in hipaa-review.md (finding 13)
- **Previous ADRs**: none
