# HIPAA / personal-information review (engineering self-review, bolt 042b)

This is a review of the application by its developers, not a certification and not legal advice. A real HIPAA assessment needs a
risk analysis by a qualified person, and a Business Associate Agreement with every provider that touches protected data.
The application is **not meant to hold protected health information (PHI)**: it shows banners and ads in shops. The aim of this review is
to keep PHI out, to protect the personal information (PII) it does hold, and to leave a trail if something goes wrong.

## 1. What personal information the application holds

| Data | Where | Protection today |
|---|---|---|
| Sign-in e-mail, first and last name | Users | Plain text (needed to find the user at sign-in). Access: API only, role-checked. Masked in logs. |
| Phone number of a user | Users.PhoneNumber | Encrypted in the database (field encryption). |
| Shop phone, street address, postal code | Shops | Encrypted in the database. |
| Payment method reference, payment reference | Subscriptions, Invoices | Encrypted in the database. |
| Password | Users.PasswordHash | PBKDF2-SHA256, 310,000 rounds, per-user salt (raised in 042b, see below). |
| Sessions | Refresh token | HttpOnly cookie, never in the page; access token kept in memory only. |
| Who did what | AuditLogs | User id, e-mail, shop, method, path, status, IP, duration. No request or response bodies. |
| Ad and banner text | Banners, components, ads | Free text written by shops. Ads are screened (042a). Banners and the default board are not screened. |
| Uploaded pictures and videos | Media storage (disk now) | Private: served only by signed, expiring links. |

## 2. Controls in place

- Role and shop checks on every route (admin, owner, executive); another shop's data is reported as not found.
- Field encryption with keys kept outside the database (key ring in a folder; production should use a protected store).
- Audit log of every call by a signed-in user, reads included, and of every call to the sign-in endpoints; no bodies stored.
- Ad content screening for personal details and health wording, with administrator review and a history of every step (042a).
- Account lockout after repeated failed sign-ins, session revocation ("sign out everywhere"), HttpOnly cookie sessions, forgery header on cookie-based calls.
- Signed, expiring media links; uploads limited to images and video types, checked by content signature; no active content (SVG, HTML).

## 3. Findings

| # | Finding | Severity | Status |
|---|---|---|---|
| 1 | Passwords hashed with only 10,000 PBKDF2 rounds | High | **Fixed (042b).** New hashes use 310,000 rounds and name their strength; old hashes still work and are replaced at the next sign-in. |
| 2 | Full e-mail addresses written to the application log (send, failure) | Medium | **Fixed (042b).** Logged as `s***@example.com`. |
| 3 | No retention rule for the audit log (kept for ever) | Medium | **Fixed (042b).** `Audit:RetentionDays`, default 2190 (six years, the HIPAA documentation period); 0 keeps for ever. Removed by the clean-up timer. |
| 4 | Audit log could be read on screen but not exported | Medium | **Fixed (042b).** Admin-only CSV export up to 100,000 rows; the export is itself audited; formula characters are defused. |
| 5 | Personal values saved before encryption was on stayed in plain text | Medium | **Fixed (042b).** `dotnet BannerService.dll --encrypt-existing` encrypts them (run once after turning encryption on; safe to repeat). Checked on SQL Server: 3 plain values encrypted, second run 0. |
| 6 | No security headers on API and web answers | Medium | **Fixed (042b).** nosniff, no framing, referrer policy, no-store for API answers, permissions policy, HSTS in production. |
| 7 | No Content-Security-Policy | Medium | **Open.** Needs testing against the editor and the shop screen (media from the API origin). |
| 8 | Banner text, component text and the default-board message are not screened for PII/PHI | Medium | **Open.** The same screening could run on save; the false-positive rate on free banner text is untested. |
| 9 | Ad screening cannot see names of people, street addresses or text inside pictures | Medium | **Open, by design.** Listed so nobody relies on it as a guarantee. |
| 10 | Sign-in e-mail and names are not encrypted | Low | **Open.** Encrypting them needs a searchable hash of the e-mail for sign-in; worth it only if the policy asks for it. |
| 11 | Encryption keys are stored in a folder next to the application | High for production | **Open, planned for bolt 044** (Azure Key Vault and Blob). Until then the folder must be on a protected volume and backed up apart from the database. |
| 12 | Media files are stored unencrypted on disk | Medium | **Open, planned for bolt 044** (storage-side encryption in Azure Blob). |
| 13 | No e-mail or SMS verification, no password reset flow | Medium | **Open, planned for bolt 044** (needs providers). Until then a forgotten password needs the administrator. |
| 14 | Audit log has no tamper protection (a database admin could change rows) | Low | **Open.** Ship the export to write-once storage in production. |
| 15 | No automatic breach notification or access alerts | Low | **Open.** Failed-sign-in spikes and admin exports could raise an in-app notice. |
| 16 | `Jwt:SecretKey` and keys come from configuration; the placeholder is refused in production | OK | Checked, no change. |
| 17 | The 39 older .NET test files are still switched off | Process | **Open** (backlog.md). |

## 4. What a human still has to do (cannot be done in code)

1. A written risk analysis and a named security officer.
2. Business Associate Agreements with the cloud, e-mail, SMS and payment providers chosen in bolt 044, and a decision whether the
   application may ever process PHI at all (the safer rule: it must not, and shops are told so).
3. Backup and restore of the database **and** the key ring, tested; a disaster-recovery plan.
4. A breach-response procedure: who is told, in how many days, using the audit export.
5. Staff training for administrators, who can see every shop's ads, users and the audit log.
6. Decide the retention periods for users who left, invoices and media (the application keeps them until an administrator removes them).

## 5. How to check the fixes

- Password hashes: sign in with an account made before this bolt, then look at the row: it now starts with `v2$310000$`.
- Logs: send a test e-mail and read the log line.
- Audit export: `GET /api/admin/audit-logs/export` as an administrator; the next export lists the first one.
- Encrypt existing data: switch field encryption on, run `dotnet BannerService.dll --encrypt-existing`, then
  `SELECT LEFT(PhoneNumber, 8) FROM Shops` should show `enc:v1:`.
