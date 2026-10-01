---
id: 019-restore-approval
unit: version-control-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [4.10, 4.11, 4.12]
---

# Bolt: 019-restore-approval

## Delivered
- Version retention: a banner keeps its newest 10 versions (older ones are deactivated, numbering keeps increasing).
- Versions are now actually created: submitting a banner for approval snapshots it. Before this, CreateSnapshotAsync was never called, so only restores produced versions.
- Restoring an older version takes the banner off air and resubmits its workflow (or creates a pending one), so it goes through the same approval as a new banner. The API response has RequiresApproval=true.
- The current state is saved as its own version before a restore overwrites it, when it differs from the latest version, so the latest is preserved and the restored copy becomes the new latest version.
- Tests: VersionRestoreApprovalTests (6), plus a submit-snapshot test.

## Not done
- A restore replaces components with new ids, so carousels and effects that reference the old component ids are not carried over.
- While a restored banner awaits approval it is not shown (the shop sees its local default banner); keeping the previously approved version live until the new one is approved is not implemented.
- Snapshots capture content only: schedule and ads are not part of a version.
