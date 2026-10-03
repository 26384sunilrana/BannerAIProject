# Bolt 036b - Shop identity and takeover

Status: complete. Built from the rule you gave on 2026-10-03.

## The rule, as built
- **Shop names may repeat.** Sign-up never checked names; creating or renaming a shop no longer refuses a name that is in use.
- **One name at one address is one shop.** Saving a shop whose name and address (and postal code, when both have one) match another active shop is refused with a message pointing to the Takeover page. Matching ignores case, spaces and punctuation ("12, Main Road." = "12 main road"). A shop with no address yet matches nothing. A closed (inactive) or archived shop no longer counts.
- **Takeover (screen "Takeover", `api/shop-takeovers`):**
  1. The new owner (who signed up with their own shop) types the name and address of the shop; the request is only accepted if such an active shop exists, so ids are never revealed. The old owner is told in the bell with a masked address of the asker.
  2. The old owner confirms with their **password** (counted as a sign-in attempt) and chooses what happens to the associates, or declines with a reason. An administrator can answer for an owner who cannot be reached, but must give a reason; it is recorded as done by an administrator. The asker can cancel until it is answered.
  3. **Associates stay (only the owner is swapped):** the old owner's login is switched off, the new owner moves into the existing shop as its owner, the shop keeps its banners, files, associates and plan, the old owner's payment method is removed and auto-renewal switched off, the new owner's placeholder shop is archived. **Approvals are blocked** (banners and executive ads) until the new owner opens Team and confirms or changes who approves (notice with a button).
  4. **Associates change too:** the old shop is closed, all its logins (owner and associates) are switched off and sessions ended, its plan stops renewing; the new owner's shop takes the address, place and time zone; the new owner adds new associates under Team.
  5. Everyone involved is signed out so their next sign-in carries the new shop; other open requests for the same shop are closed ("taken over by someone else"); administrators are told.
- Migration `ShopTakeovers` (table, and the flag on shops).

## Verified
- Domain 785 (13 new for matching), Integration 75 (6 in the rebuilt shop-name class: names and addresses, asking rules and masking, owner-only swap with approval gate, replace, decline/cancel/finality, administrator and rival request), Jest 766 (9 new), tsc and next build clean; on real SQL Server too.
- Chrome against the real API, `takeover.js` 16/16: same name allowed, same address refused, ask, bell message, wrong password refused, hand-over, old owner cannot sign in, new owner must confirm approvers and the notice goes, associates-change path ends with the old shop closed and the address on the new shop. `ads.js` 21/21 and `lifecycle.js` 20/20 re-run.

## Also fixed
- Integration tests on a real SQL Server database failed about half of the runs: each test class starts its own application, and two applications applying migrations at the same moment collided ("object already exists"). The test factory now starts applications one at a time; 5 fresh-database runs in a row pass. (In real deployments migrations belong to the `--migrate` Job, as documented.)

## Decisions I took
- "Both owners confirm" = the asker by asking (they must be a signed-in owner of a shop of their own) and the old owner by confirming with the password; an administrator may stand in for the old owner.
- With "associates stay", the old owner's login is switched off (they have no shop left), not kept as an associate.
- Banners, files and ads of a closed shop stay with that closed shop; nothing is copied to the new shop.

## Not done
- Ads still booked on a closed shop are not stopped by the closing (the shop screen of a closed shop cannot sign in anyway); the administrator statement keeps counting them until their end date.
- No proof of sale is collected (documents); the administrator route has a written reason only.
- The Team notice has no Jest test of its own (covered by the integration test and the browser run).
