# Bolt 042b - HIPAA review and hardening

Status: complete. The review itself is `memory-bank/intents/002-requirements-gap-closure/hipaa-review.md` (findings, what was fixed, what stays open, what a person has to do).

## Fixed
- Passwords: PBKDF2 raised from 10,000 to 310,000 rounds; the hash names its strength; old hashes verify and are replaced at the next sign-in.
- E-mail addresses masked in logs.
- Audit log: retention (default six years) removed by the clean-up timer; admin CSV export (Activity screen "Export as CSV"), audited, formulas defused.
- `--encrypt-existing` command encrypts personal values saved before encryption was on.
- Security headers on the API and the web app.

## Verified
- Domain 772 (25 new), Integration 70 (4 new: old hash upgraded at sign-in, headers, CSV export audited and admin-only, encryption command refuses when off), Jest +2 (export button), tsc clean.
- Encryption command checked on SQL Server LocalDB: 3 plain shop values (phone, address, postal code) became `enc:v1:...`, a second run changed 0.
- Not re-run in Chrome after the password change (sign-in is covered by the integration tests).

## Open (see the review)
- Content-Security-Policy, screening banner text, Key Vault and encrypted media (bolt 044), tamper protection of the audit log, access alerts.
