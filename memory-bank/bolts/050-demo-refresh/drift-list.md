# Drift list: demo, pictures and guides against the current application

Kept while bolt 050 is in Stage 2. Each line is something that no longer matched, and what was done.
Run commands (in `demo/`): `node e2e-story.mjs`, `node take-screenshots.mjs`, `node build-docx.mjs`. Stack up with `docker compose up --build -d`, administrator made with `--create-admin`, `node demo/seed-demo.mjs` run.

## A. Scripts

| # | Where | What failed | Cause | Fix |
|---|---|---|---|---|
| 1 | story 01 Sign in as the administrator | `getByRole('link', { name: 'Places' })` matched 2 elements | Home now also has a card that links to Places | Look in the main menu only: `menu(page)` = `getByRole('navigation', { name: 'Main' })` (done) |
| 2 | story 05 Create the shop account | `getByRole('link', { name: 'Banners' })` matched 4 elements | Same: Home cards repeat the menu links | Same fix (done) |
| 3 | story 10 Sign in as the sales executive | Same ambiguous `Banners` link; the "no Team" check looked at the whole page | Same | Both look in the main menu only (done) |
| 4 | story 06 Choose a subscription plan | `getByRole('radio').first()` timed out | The Subscription page has a hidden radio for the period (Monthly to Yearly) before the plan cards | Look in the radio group named "Plans" (done) |
| 5 | story 40 expectation text | Said "What was shown" lists hours; the screen says "Nothing was reported in this period" right after pairing | The screen reports once a minute; the check never tested the claim, so it passed | Text now says it fills in as the screen reports (done) |
| 6 | screenshots, administrator sign-in (one run) | `waitForURL` timed out on sign-in; every administrator step after it failed | The API allows 20 sign-in and "who am I" calls a minute from one address, and every page load makes one; a run straight after the story hit the limit | Both scripts wait 65 s and sign in once more (done) |
| 7 | screenshots, Users and Subscriptions | Pictures showed the Super Admin's real e-mail address and the shops the story had made | The pictures were of whatever is on the database | Users is searched for `demo-`, Subscriptions for `Sunrise` (done) |
| 9 | story steps 5 and 3, and step 42 (on the fresh database, three failures) | Sign-up, sign-in and the API sign-in refused with "Too many requests" | The 20-a-minute limit, reached because the story starts right after the picture run | Every step retries once after a minute if the page says so; the API helper retries once on 429; step 42 has its own wait (done) |
| 10 | screenshots, admin-13 Shops on the fresh database | The step waited for a "View" button | On the card "View" is a link, not a button | Wait for the link (done) |
| 8 | screenshots, admin-13 Shops (failed twice, not a rate-limit problem) | `text=Sunrise Cafe` never appeared | The Shops page lists 6 cards a page in name order and has no search; each story run adds a shop, so Sunrise Cafe moved to page 2 | The step pages forward until the demo cafe shows; `visit()` also waits a minute and reloads once if a page comes up empty (done) |

## B. Pictures that no longer matched a screen (all retaken)

- Owner and executive menus: **Screens** was missing. Administrator menu: **Admins** and **Providers** were missing.
- Administrator Users: new **Reset two-step** button, **Include deactivated** box, "Email not verified" mark.
- Administrator Subscriptions: button is **Run renewal job now** (was written as "Run lifecycle now"); **Renew early** on each row; filters are **Status** and **Shop**.
- Administrator Plans: table of names and prices; limits are under **View** and **Edit**.
- Owner Subscription: **Renew early**, **Renew automatically**, change of plan and period (the walkthrough said only "plan, price and renewal date" and mentioned paying online).
- Administrator Shops had no step in the walkthrough: added.

## C. Sentences in the walkthroughs and guides that were wrong (all corrected)

- Owner walkthrough step 14 (payment remark removed); owner guide: "Turn off" is **Switch it off**; "reminders arrive before it ends" (reminders go out only by e-mail or text, so none arrive in the demo); "Not available yet" said the owner cannot renew the plan (there is **Renew early**) and listed e-mail and payment items; "Currency is not shown".
- Administrator guide: menu without Admins and Providers (and in a different order); "Run lifecycle now"; "archive" a shop (no such button; there is **Create Shop**); the two-step requirement for administrators does not apply to the demo (`REQUIRE_TWO_FACTOR_FOR_ADMINS` defaults to false in `docker-compose.yml`); "Not available yet" listed payments, e-mail, SMS, self-service renewal.
- Executive guide: "Turn off" is **Switch it off**; the "Not available yet" list.
- Start here: no mention of the scripts; last section talked about e-mails, texts and payments.
- All three guides said "as built through bolt 049 (October 2026)".

## D. Added to the story (new steps, each with a picture and a check)

Chapter 9: schedule calendar (owner, executive), upload to My files (owner) and the shared library (executive), the television shows a pairing code, the owner adds the screen, the television starts playing, the screen shows Online, two-step sign-in turned on (the script works the code out the same way an authenticator app does), sign in again with a code. Step 23 now first tries a phone number in an ad headline and checks that it is refused. The ad statement and the ad report were already in the story.

## E. Application findings (not changed in this bolt; for the user to decide)

1. **Ad rates and Ad report show the menu bar twice**: the whole page is drawn inside a second page frame (seen in `admin-06-ad-rates.png` and `admin-10-ad-report.png`).
2. **429 on `/api/authentication/me`** fills the Activity log during quick navigation: the "auth" limit (20 a minute per address) also counts `/me`, which the page frame calls on every full page load. A busy person, or a whole shop behind one address, can hit it; sign-in is then refused for a minute.
3. **Currency**: the administrator's Plans page shows `$`, the owner's Subscription page shows `₹`, ad prices are plain numbers.
4. The Subscription page says "Online payment is not connected yet; subscribing activates the plan straight away." when no provider is set. True, and the walkthrough text does not repeat it.

## F. Left out on purpose

Shop takeover is still in the owner and administrator walkthroughs as before (screens that exist, not added to the story). Not in the story and not in the walkthroughs: administrator management (**Admins**, only mentioned), **Providers** (only mentioned), forgot-password, reset-password and verify-e-mail screens, payment, e-mail and SMS.
