---
id: 018b-owner-onboarding
unit: 002-banner-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
---

# Bolt: 018b-owner-onboarding

POST /api/authentication/register is now shop-owner sign-up: it requires ShopName, creates the user and the shop (owned by that user), assigns the ShopOwner role, and returns tokens that carry shop_id and the role.

Full record: [docs/archive/bolts/018b-owner-onboarding/bolt.md](../../../docs/archive/bolts/018b-owner-onboarding/bolt.md)
