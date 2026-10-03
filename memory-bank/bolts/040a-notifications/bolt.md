# Bolt 040a - In-app notifications (first step of the ads work)

Status: complete

Chosen first because the ads rules need it (an owner overriding an admin ad must notify the admin) and nothing else needs to exist before it.

## Built
- A bell in the top bar: unread count (checked every minute, only while the tab is visible), the latest 10 messages, "Mark all as read", links to the page the message is about, closes with Escape or a click outside.
- API `api/notifications` (own messages only): list, unread-count, mark one read, mark all read. One row per recipient.
- `NotificationService` for producers: notify users, all admins, or a shop's owners. A failure to store never fails the action that caused it. Links must be a page of this application (no other sites).
- First producers: submitting a banner for approval tells the shop's owners; approving or sending back tells the person who submitted it.
- Read messages are removed after 30 days, unread after 90, by the existing clean-up timer.
- Migration `InAppNotifications`.

## Also fixed
- Saving a shop (for example its time zone) failed with "A shop with this name already exists" when another shop had the same name, because sign-up does not check names. The name is now checked only when it changes. Found by the browser run.
- Bolt 039's unit test for "no plan" still expected no limit (not run last time); updated to the 1 GB starter allowance.

## Verified
- Domain 669, Application 250 (9 new), Integration 50 (2 new: messages through the real workflow, shop name), Jest 715 (6 new), tsc and next build clean.
- Chrome against the real API on SQL Server LocalDB, new script `schedule.js` 13/13: shop time zone from the shop form; a banner scheduled 9:00-12:00 on weekdays; overlap refused naming the other banner; shared dates with other hours allowed; saved schedule shown on the shop clock; calendar shows five weekday blocks, the 9:00 block 9 hours down; default board designed, shown on the screen and still shown with the API unreachable; bell shows 1, names the banner, opens Approvals and clears. This also closes the browser gap of bolt 038.
- Older scripts: lifecycle 20/20 and media 11/11; library 12/13, the one miss is a stale expectation (a no-plan shop now shows "of 1 GB"). The library and lifecycle scripts had to be updated for the empty image box (037) and the shop clock (038).

## Not done
- No notification page with the full history (the bell shows 10); no e-mail or SMS copy (needs providers, 044); no per-kind opt-out.
- Other producers (subscription ending, storage nearly full, ad events) to be added with their features.

## Next
Bolt 040b: ad entities, who may create them, slots with a booking calendar, approval and audit (see roadmap R7).
