---
id: 017-banner-scheduling
unit: banner-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [3a-2, 3a-3, 5vi]
---

# Bolt: 017-banner-scheduling

## Delivered
- PublishWindow value object (half-open UTC interval); Banner.PublishStartAt/PublishEndAt, Banner.SetSchedule.
- BannerScheduleService: overlapping windows in a shop are rejected (same-day hours included); ResolveActive picks the live, published banner.
- Changing the schedule of an approved or published banner resubmits its workflow for approval (PublishWorkflow.ResubmitForApproval).
- API: PUT /api/banners/{id}/schedule (400 invalid, 409 overlap), GET /api/banners/active (useDefaultBanner=true when nothing is live).
- Migration 20261001010000_AddBannerPublishWindow. Tests: BannerScheduleServiceTests (14).

## Not done
- PublishWorkflow.Publish does not set Banner.IsPublished; the active-banner lookup relies on the workflow status instead.
- Controller/app service are untested (no API-level tests yet); the Local default banner itself is a client concern (bolt 022/026).
