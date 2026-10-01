---
id: 024-security-compliance
unit: banner-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [4.13, 5viii, tenant isolation]
---

# Bolt: 024-security-compliance

## Delivered
- Tenant isolation: TenantAccessFilter (global) rejects with 403 any call that names another shop (shopId argument, ShopId in a request body) or a resource owned by another shop (ad, report, invoice, subscription, workflow, approval request); admins pass; reviewers can only read their own queue.
- Admin-only: invoice mutation and queues, shop create/list/search/delete and owner assignment, analytics cleanup, ad metrics and expiring list, subscription renewals queue, pending workflows.
- Audit log: every call by a signed-in user (reads included) and every authentication call is stored with user, shop, method, path, status, ip, duration (no bodies). Written in its own scope so it never saves a failed request's changes. GET /api/admin/audit-logs.
- User management: GET /api/admin/users (search, paging), deactivate (also revokes refresh tokens), activate, unlock.
- Encryption at rest: user and shop phone numbers, shop address and postal code, subscription payment method id and invoice payment reference are encrypted with ASP.NET Data Protection (AES-256, keys persisted to Security:KeyDirectory). Plain legacy values stay readable. Encrypted columns can no longer be searched by phone.
- Tests: TenantAccessFilterTests, AdminAndAuditTests, FieldEncryptionTests, and PipelineSmokeTests, which start the real application on an in-memory database and exercise sign-up, JWT, roles, tenant and admin checks, and the audit trail.

## Defects found while building the smoke test (all fixed)
- JwtTokenService read Jwt:Key but appsettings and the token validator use Jwt:SecretKey, so no token could be issued. Token lifetime key also differed.
- UserRole was mapped without its navigation properties, so user.UserRoles (and every role claim) was always empty; Admin and ShopOwner checks could never pass.
- The integration test project targeted net10.0 against a net8.0 app and failed on response writing; now net8.0.
- Production now refuses to start with a missing or placeholder Jwt:SecretKey.

## Not done
- Keys are stored on the file system. For Azure use a shared, protected key store (Blob + Key Vault) so all pods share keys; planned with bolt 025.
- Existing plain data is encrypted only when a row is next saved; no bulk re-encryption job.
- Infrastructure encryption (SQL TDE, TLS) belongs to deployment.
- Audit log retention and export are not implemented.
- Jwt:SecretKey in appsettings.json is a development placeholder; supply it from a secret store.
- The UserRole mapping change may need a schema check against any database created from the earlier model.
