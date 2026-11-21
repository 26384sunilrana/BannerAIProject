# Result note: bolt 050 demo refresh

**Date**: 2026-11-21. Stack: Docker (api :5000, web :3000, SQL Server), demo seeded, branch testVibeCode.

## Story (`node demo/e2e-story.mjs`)

- **Final proof, on a freshly wiped database** (`docker compose down -v`, administrator made, demo seeded, pictures retaken first, then the story): two runs in a row, both **42 of 42 steps worked**: runs `cf9st` and `ch8ov` (the Word file `BannerAI-End-to-End-Demo.docx` is from `ch8ov`).
- Earlier, on the old database: runs `a6h6m` and `aaicd`, also 42 of 42 twice in a row.
- Before them: run `91v0s` 32 of 32 (the old story, after four selector fixes) and run `97s7h` 42 of 42 (first run with the new chapter). One later run (`9zlwv`) failed step 42 on the rate limit; step 42 was then made to wait and retry, and the two runs above followed.
- Each of the two final runs waited one minute once, at an API sign-in, for the application's rate limit and went on (it is in the logs). Before the story was given a retry for every step and for its API calls, three runs on the fresh database failed on the same limit (`c96ot` at sign-up, `cepav` at the API sign-in) and one passed (`ca05b`).

## Pictures and Word files

- `node demo/take-screenshots.mjs`: **46 of 46** pictures, in `docs/guides/walkthroughs/images/`.
- `node demo/build-docx.mjs`: the three role Word files, all steps worked.

## What was left out on purpose

- Online payment, e-mail and SMS delivery, the forgot-password, reset-password and verify-e-mail screens (as you decided). The demo runs with no provider.
- Administrator management (**Admins**) and **Providers**: named in the walkthrough and guide, not shown.
- Shop takeover: not added to the story; it stays in the owner and administrator walkthroughs as before.

## Not re-checked

- The text of walkthrough steps that describe things the pictures do not show (for example the owner's **Override this ad**, the mega-ad and popup sizes, **Hand the shop over**, **Confirm for the owner**, the compliance review of health wording, **Export as CSV**) was read against the screens that show the buttons but not clicked through. The phone-number refusal is now a checked step.

## For you to decide (application findings, nothing changed)

1. Ad rates and Ad report draw the menu bar twice.
2. The "auth" rate limit (20 a minute per address) also counts `/api/authentication/me`, which every page load calls; the Activity log fills with 429 and a shop behind one address can lock itself out for a minute.
3. Currency: `$` on the administrator's Plans page, `₹` on the owner's Subscription page, plain numbers for ads.
4. (Done) The pictures were retaken on a freshly wiped database, so they show only the demo cafe, the System Shop and the three demo people. Wiping removed the Super Admin login `26384sunilrana@gmail.com` from the local database; it has to be created again with `--create-admin` if needed.
