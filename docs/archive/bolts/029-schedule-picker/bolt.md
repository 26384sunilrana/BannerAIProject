---
id: 029-schedule-picker
unit: banner-editor-ui
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [3a-2, 3a-3, 5vi in the UI]
---

# Bolt: 029-schedule-picker

Owners and executives can now use the publish window from bolt 017.

## UI
- Each banner on /banners shows when it runs ("Fri 2 Oct, 9:00 AM - 12:00 PM" or "Not scheduled") with a Set / Change schedule form (start and end in the visitor's time zone).
- The form checks both ends, end after start and not already past; the server's overlap refusal names the other banner. Back-to-back hours (12:00 after 12:00) are allowed.
- Changing the schedule of a pending, approved or live banner warns first, then sends it back for approval.
- Submit for approval is disabled until a schedule exists (and the API refuses it too: a banner with no schedule would never be shown).
- Approvers see when each banner will be shown. The dashboard says which banner is showing now, or that the shop shows its own default banner.

## API
- Banner responses carry publishStartAt / publishEndAt.
- Submitting without a schedule is refused (400) with a clear message.
- Dates were sent without a time zone ("2026-10-02T03:30:00"), so browsers read UTC times as local time, hours off. Every stored date is now read back as UTC (a model-wide converter; no schema change).
- The overlap message no longer prints UTC times.

## Verification
- Chrome against the real API on SQL Server: 13 schedule checks; the account (28), editor (18) and media (11) runs still pass after the changes (70 checks in all).
- Domain 440, Application 110, Integration 10 (also on SQL Server LocalDB); Jest 398 passing (103 older failures, unchanged).

## Not done
- Times use the browser's time zone; the shop has no time zone of its own, so someone opening the page from another country sees different hours (the stored moment is the same).
- The shop's display client (the screen that actually plays the banner and falls back to the local default) is not built: GET /api/banners/active exists, nothing consumes it yet. Bolt 022 covers the default banner.
- No calendar view of the shop's schedule; one window per banner (no repeating daily hours).
