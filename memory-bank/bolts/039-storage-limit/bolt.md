# Bolt 039 - Storage limit enforced

Status: complete (scoped down, see below)

## Built
- Starting an upload is refused with 413 (`code: storage_limit`, used and limit bytes) when the file would pass the plan's `MaxStorageGB`. Uploads in progress count at once with their declared size, so several parallel uploads cannot pass the limit together. A plan limit of 0 means unlimited.
- A shop with **no plan gets a starter allowance of 1 GB** (before, it had no limit at all). The usage figure shown on My files uses the same rule.
- My files shows "almost full" from 80% and "full" at 100% with a link to the plans; a refused upload shows the reason and a "See plans" link.

## Verified
- Domain 669, Integration 48 (new test: two half-size uploads fit, the next is refused), Jest 709 (media library 16), tsc clean. Not run in Chrome.

## Not done (decide with the owner)
- **Banner size** pricing (requirement 1h says "banner size / storage"): plans are not priced or limited by banner pixel size or file size per banner. Only storage is enforced. Needs a decision on what "size" means.
- Pricing changes for plans stay as the admin sets them in the plan form; no per-GB metered billing (needs the payment provider, bolt 044).
- No per-shop storage view for the administrator; no email or alert when a shop reaches 80%.
- A downgrade to a plan smaller than the current usage is not blocked; the shop just cannot upload more.
