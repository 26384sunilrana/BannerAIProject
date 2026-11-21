---
stage: test
bolt: 050-demo-refresh
created: 2026-11-21T11:22:25Z
---

## Test Report: demo and walkthrough refresh

### Summary

- **Tests**: 42/42 story steps passed, twice in a row on a freshly wiped database (runs `cf9st`, `ch8ov`); 46/46 pictures taken on that database; 3 role Word files and 1 end-to-end Word file built with every picture in place (14, 9, 20 and 57 pictures) and no step marked failed; 4 document checks passed (menu coverage, links and pictures, forbidden wording, labels). No .NET or web unit tests were run: this bolt changes no application code.
- **Coverage**: not applicable (scripts and documents). Coverage of the screens is the menu check below: every item in the owner, executive and administrator menus is in a walkthrough, or named and left out on purpose.

### Test Files

- [x] `demo/e2e-story.mjs` - the story is the test: 42 checked steps across the administrator, owner, executive and shop television, run against the live stack.
- [x] `demo/take-screenshots.mjs` - 46 pictures, each step fails if the page does not show what it should.
- [x] `demo/build-docx.mjs` - builds the Word files and reports a picture that is missing.
- [x] A one-off checker (kept outside the repository) - read the live main menu of each role against the walkthrough text, searched the guides and the Word files' text for payment, e-mail and SMS wording, resolved every link and picture, and looked up every bold label in the web source.

### Acceptance Criteria Validation

- ✅ **The story runs from start to finish with every step passing, twice in a row**: runs `cf9st` and `ch8ov`, 42 of 42 each, on a freshly wiped database (earlier runs `a6h6m` and `aaicd` on the old one did the same). Each final run waited one minute once for the application's rate limit and went on; three earlier runs on the fresh database failed on that limit before every step and API call was given a retry.
- ✅ **Every screen reachable from the main menu of each role appears in a walkthrough or is listed as left out on purpose**: owner 13 of 13, executive 8 of 8, administrator 13 of 13 menu items found in the walkthroughs. Admins and Providers are named and left out on purpose; Takeover is in the owner and administrator walkthroughs but not in the story.
- ✅ **The three role walkthroughs, their Word files and the end-to-end Word file are rebuilt from that run**: rebuilt after the last text change; no "picture missing", no failed step in any of the four.
- ✅ **No step, picture or sentence mentions online payment, a payment provider, e-mail or SMS delivery as something that works**: no hits in the guides, walkthroughs or Word files, except "Payment failed", a real status name on the Subscriptions screen. The Subscription screen's own line "Online payment is not connected yet" appears in the end-to-end pictures and says the opposite.
- ✅ **The guides name the real labels and menu items of the current screens**: the check found two wrong headings (the Team page says "Who approves new and changed banners" and "Who approves ads booked by sales executives"); fixed in the owner and executive guides and the owner walkthrough. The other unmatched bold items are emphasis, sample data or breadcrumb paths (for example "Ads → Monthly statement"), read by eye.
- ✅ **The drift list is kept in the bolt folder, with what was fixed**: `drift-list.md`, 8 script problems, the pictures and sentences that were wrong, and the application findings.

### Issues Found

- Four things in the application, none changed here: Ad rates and Ad report draw the menu bar twice; the 20-a-minute "auth" limit also counts `/api/authentication/me`, so the Activity log fills with 429 and every story run waits a minute once; currency is `$` on the administrator's Plans page and `₹` on the owner's Subscription page; the Shops list pages six at a time with no search.
- Resolved: the pictures were retaken on a freshly wiped database, so the Shops and Ad report pictures no longer show shops made by story runs.
- Three pictures (`admin-signed-in`, `owner-signed-in`, `seller-signed-in`) are taken but not used by any walkthrough (as before this bolt).

### Notes

- Not exercised, only read against the screens: Override this ad, Hand the shop over, Confirm for the owner, the compliance review of health wording, Export as CSV, mega-ad and popup sizes.
- Committed locally at completion: the demo scripts, the walkthroughs, pictures, Word files, guides and this bolt's folder; `BannerAIProject.sln` and `demo/output/` left out; not pushed.
