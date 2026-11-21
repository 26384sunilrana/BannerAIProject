---
stage: plan
bolt: 050-demo-refresh
created: 2026-11-21T10:30:00Z
---

## Implementation Plan: demo and walkthrough refresh

### Objective

Make the demo script, its screenshots, the Word walkthroughs and the written guides match the application as it is today, and prove it by running them against a live stack. Payment and e-mail are left out: the demo runs with no provider connected, so renewal is the plain Renew click and no mail is sent.

### What exists today

- `demo/e2e-story.mjs`: one story, start to finish, in three browser windows (administrator, owner, sales executive), 30-odd checked steps; builds one Word file. Not yet committed.
- `demo/take-screenshots.mjs`: pictures for the three role walkthroughs (edited, not committed); `demo/seed-demo.mjs`: fills in the demo cafe; `demo/build-docx.mjs`: makes the Word files (not committed).
- `docs/guides/walkthroughs/{admin,owner,executive}.md` with `images/`, and three Word files in `docs/guides/walkthroughs/docx/` (not committed).
- `docs/guides/{start-here,admin,shop-owner,banner-creator}.md`.
- The application has grown to about 38 pages. The story covers sign-in, places, ad rates, plans, sign-up, subscription, shop location, default board, team, banners, editor, schedule, approvals, ads and the shop screen. It does not cover: managed screens (pairing), two-step sign-in and the account page, the file library, the calendar, ad statements and reports, the activity log, administrator management and shop takeover.

### Deliverables

1. **A drift list**: every step of the story and every picture that fails or no longer matches a screen, found by running the scripts against the live stack.
2. **A working `demo/e2e-story.mjs`** (all steps pass), with selectors and expectations fixed.
3. **New steps for what is missing** (see the questions below), each with a picture and a check.
4. **Rebuilt walkthroughs**: the three markdown walkthroughs and `images/`, the three role Word files and the end-to-end Word file.
5. **Guides re-read against the screens** and corrected: `start-here.md`, `admin.md`, `shop-owner.md`, `banner-creator.md`.
6. **A short result note** with the run id, steps passed and what was left out.

### Dependencies

- **A running stack**: API on http://localhost:5000, web on http://localhost:3000, and an administrator login. The guide starts it with Docker Desktop (`docker compose up --build -d`), which includes the SQL Server container. Docker is installed here but **Docker Desktop is not running**, so I cannot start the stack until you start it (or tell me another way to run it).
- `npm install` in `demo/` (a browser driver is downloaded the first time).
- Nothing from bolt 044a is needed, and nothing from it should appear: with no provider connected the Subscription screen shows the plain Renew button, as before.

### Technical Approach

1. Start the stack and run `seed-demo.mjs`; run `e2e-story.mjs` and `take-screenshots.mjs` as they are and record every failure in the drift list. No fixes in this step.
2. Fix the scripts in the order of the story. The steps are checked against what the page shows and through the API, so a changed label or selector is the usual cause.
3. Add the new steps at the end of each role's part, so the existing order keeps working: administrator (places, ad report, activity log, administrators), owner (screens pairing, account and two-step setup, file library, calendar, ad statement), executive (file library, calendar).
4. Rebuild the documents from the final run and replace the old pictures.
5. Re-read each guide next to the screen it describes; correct wording, names and steps; remove anything about payments or e-mail that is not true today.
6. Run the whole story twice on the same database (it makes new people each time) to prove it can be repeated.

### Acceptance Criteria

- [ ] The story runs from start to finish with every step passing, twice in a row.
- [ ] Every screen reachable from the main menu of each role either appears in a walkthrough or is listed as left out on purpose.
- [ ] The three role walkthroughs, their Word files and the end-to-end Word file are rebuilt from that run.
- [ ] No step, picture or sentence mentions online payment, a payment provider, e-mail or SMS delivery as something that works.
- [ ] The guides name the real labels and menu items of the current screens.
- [ ] The drift list is kept in the bolt folder, with what was fixed.

### Questions for you

1 - **How do I get a stack?** Start Docker Desktop and tell me, then I follow `start-here.md`. (Recommended.)
2 - **Which new areas go into the demo?** Recommended: managed screens, the account page with two-step sign-in, the file library, the calendar, the ad statement and report, and the activity log. Leave out: shop takeover and administrator management (rare, easy to get wrong in a demo). Say if you want a different set.
3 - **Forgot-password and verify-e-mail screens exist now but need mail to work.** I would leave them out of the demo and the guides, as you said no e-mail. Is that right?
4 - **The uncommitted demo files** (`e2e-story.mjs`, `build-docx.mjs`, `package.json`, the Word files, the edited screenshot script, the `.sln` change): should the bolt commit them with the refreshed versions at the end? I would commit the demo and docs but leave the `.sln` change out unless you say it is yours to keep.
