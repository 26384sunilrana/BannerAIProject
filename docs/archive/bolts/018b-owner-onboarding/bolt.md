---
id: 018b-owner-onboarding
unit: banner-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [3a-i]
---

# Bolt: 018b-owner-onboarding

## Delivered
- POST /api/authentication/register is now shop-owner sign-up: it requires ShopName, creates the user and the shop (owned by that user), assigns the ShopOwner role, and returns tokens that carry shop_id and the role.
- Registering with a ShopId is rejected. Before this, anyone could register into any existing shop; logins for a shop are created by its owner (bolt 018).
- If shop creation fails the new user is removed.
- The access token at sign-up previously carried no roles (role names were not loaded); the user is reloaded before the token is issued.
- Tests: OwnerRegistrationTests (5).

## Not done
- BannerUI has no sign-up or login screens (no reference to register in src/BannerUI/src). Added to bolt 026.
- The user guides in the repo root still describe the old sign-up and need updating.
- Email verification is not enforced at login.
