---
stage: implement
bolt: 050-demo-refresh
created: 2026-11-21T11:02:12Z
---

## Implementation Walkthrough: demo and walkthrough refresh

### Summary

The demo story, the picture-taking script, the three role walkthroughs, the Word files and the four written guides now match the application as it is today, and they were proved against a live stack.
The story passes all 42 steps, twice in a row on a freshly wiped database; the picture script takes 46 of 46 pictures. Payment, e-mail and SMS are not part of any of it.

### Structure Overview

Three scripts in `demo/` work from the same running stack. One plays a story with three browser windows (administrator, owner, sales executive) and a fourth window for the shop television; it writes one Word file with a picture of every step.
One takes the pictures for the role walkthroughs from the seeded demo cafe. One builds a Word file per role from the walkthrough text and those pictures. The written guides sit next to them in `docs/guides/`.
Every difference found between what the documents said and what the screens show was recorded in the drift list before it was fixed.

### Completed Work

- [x] `demo/e2e-story.mjs` - the story, now 42 checked steps in nine chapters. Menu links are looked up in the main menu only. New chapter 9 covers the calendar, the shared file library, pairing a television, the screen showing Online, turning on two-step sign-in and signing in again with a code. The ad step also checks that a phone number in a headline is refused. Sign-in waits out the rate limit and tries once more.
- [x] `demo/take-screenshots.mjs` - 46 pictures. New pictures for Screens, My account, the executive's calendar, files, screens and account, and the administrator's Shops. The Users and Subscriptions lists are filtered to the demo, the Shops step pages forward to the demo cafe, and pages wait out the rate limit once.
- [x] `demo/build-docx.mjs` - unchanged in behaviour; rebuilt the three role Word files from the new text and pictures (it was not yet committed).
- [x] `demo/package.json`, `demo/package-lock.json` - the two libraries the scripts need (not yet committed).
- [x] `docs/guides/walkthroughs/owner.md` - menu with Screens, new Subscription text without any payment remark, new steps for Screens and My account with two-step sign-in.
- [x] `docs/guides/walkthroughs/executive.md` - menu with Screens, calendar picture, new steps for My files, Screens and My account, the Approvals page, one more row in "what you cannot do".
- [x] `docs/guides/walkthroughs/admin.md` - full menu (Admins and Providers named and left out on purpose), Users and Subscriptions text matched to the screens, a Shops step, steps renumbered, no mention of payment, e-mail or SMS.
- [x] `docs/guides/walkthroughs/images/` - 46 pictures, all retaken from this run.
- [x] `docs/guides/walkthroughs/docx/` - three role Word files and the end-to-end Word file.
- [x] `docs/guides/start-here.md` - what the demo contains, and a new section on the three scripts; the "does not do yet" section on e-mail and payments is gone.
- [x] `docs/guides/shop-owner.md`, `admin.md`, `banner-creator.md` - checked screen by screen; labels, menu order, buttons and "not available yet" lists corrected; the header no longer says bolt 049.
- [x] `memory-bank/bolts/050-demo-refresh/drift-list.md` - eight script problems, the pictures and sentences that were wrong, what was added, four application findings and what was left out.
- [x] `memory-bank/bolts/050-demo-refresh/result-note.md` - run ids, steps passed, what was left out.

### Key Decisions

- **Two-step setup is the last thing the owner does in the story**: it makes the owner's later API sign-ins impossible, and the sign-in with a code is itself a step worth showing. The code is worked out in the script the way an authenticator app does.
- **The television is a fourth browser window with no sign-in**, reading the code from the page like a person would.
- **Left alone: shop takeover and administrator management**: takeover stays in the walkthroughs as before (not added to the story); administrator management and Providers are named in the walkthrough and guide but not shown.
- **Rate limit handled in the scripts, not in the application**: the application limit is reported as a finding for you to decide on.
- **Pictures are filtered, not edited**: the lists in the pictures are narrowed with the screens' own search boxes and pages, so no rows are hidden or removed.

### Deviations from Plan

- The plan listed the activity log and administrator management as possible additions; the activity log was already in the story, and administrator management was left out as you decided.
- The ad statement and ad report were already in the story; only the checks were confirmed.
- Added beyond the plan: the phone-number refusal check, an administrator Shops step, and retries for the rate limit.

### Dependencies Added

- [x] `playwright-core` - drives Chrome for the story and the pictures (already in `demo/package.json`).
- [x] `docx` - writes the Word files (already in `demo/package.json`).

### Developer Notes

- The pictures were retaken on a freshly wiped database (the picture script first, then the story). Pictures taken after story runs would show the shops those runs made, so keep that order.
- Each story run takes about three to five minutes and may wait a minute once or twice because of the application's rate limit; every step and the API calls retry once.
- `demo/output/` holds every run's pictures and is not meant to be committed.
