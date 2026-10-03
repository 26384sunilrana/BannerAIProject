# Bolt 040b - Ad bookings on shop screens

Status: complete (first slice of the ads work; rates, statement and override come in 041)

## Built
- **Ad kinds** (requirements 4.14-4.18): side strip (10-40% of the screen, left/right/top/bottom), mega ad (50-80%, the banner moves to the rest), popup (3-30 s, comes back every 1-60 min, over the banner), small corner tile.
- **Who may book:** the administrator (names the shop), a shop owner, a sales executive of the shop. Advertisers are not users: the ad carries a business name only.
- **Approval:** an owner's or admin's ad is approved when sent in; an executive's waits for the owner, who approves or sends it back with a reason. Only an ad not yet sent in, or sent back, can be edited.
- **Booking calendar:** the admin picks a shop and sees every ad on it, including the owner's and executives', marked by who booked it (so the admin can plan around them). Pending ads hold their slot.
- **Clash rules:** a mega ad needs the screen to itself; side strips may take 50% together and one per edge; one popup at a time; one tile per corner. Daily hours and weekdays (as for banners) let two ads share dates. The refusal names the ad in the way.
- **History:** every create, edit, send-in, approve, send-back and cancel is written to the ad history with who and when, shown on the Ads page.
- **Notifications (bell):** an executive's ad tells the owners; approval or sending back tells the booker; an admin booking tells the shop owners.
- **Shop screen:** draws the live ads around the banner or the default board (strips, mega, popup cycle, corner tiles). An ad that ended or was cancelled is simply not in the list, so its space goes back (this covers the "space returns when an ad ends" rule of 041b).
- **Picture** on an ad from the shop's own files (owner/executive); a file on a live or waiting ad cannot be deleted from the library.
- **Admin ads are text only for now:** the admin has no files of the shop to choose from.
- Migration `ShopAds`.

## Verified
- Domain 700 (31 new rules tests), Application 250, Integration 57 (7 new through the real application, also on real SQL Server), Jest 734 (19 new), tsc and next build clean.
- Chrome against the real API on SQL Server LocalDB, `ads.js` 12/12: owner books and sends in (draft, then approved); a clashing mega ad refused naming the other; admin picks the shop, sees the owner's booking marked "Booked by the owner", books a strip, owner is told in the bell and cannot cancel or change it; the screen shows the left strip at 25% and the right at 20% around the default board; cancelling gives the space back. Earlier scripts re-run: schedule 13/13, lifecycle 20/20.

## Not done (next: 041)
- Admin location rates and the monthly hours statement per shop; free pricing for owner/executive ads.
- Owner override of an admin ad with a notification to the admin (today an owner cannot cancel an admin ad; they must ask the admin).
- Notify shops by location about a major ad (041b).
- HIPAA/PII checks on ad content and approval of those checks (042). Today there is only a hint under the text box and the owner approval for executive ads.
- Admin pictures; a month-view calendar of bookings; ads do not yet count hours shown (needed for the statement).
- The clash check on several side strips is conservative (it counts every strip that overlaps the new one, even if two of them never overlap each other).
