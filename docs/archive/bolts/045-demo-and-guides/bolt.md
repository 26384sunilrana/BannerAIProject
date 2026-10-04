# Bolt 045 - Demo, role walkthroughs and provider guide

Status: complete

## Built
- `--create-admin <email> <password>` command: a new installation had **no way to get an administrator** (sign-up only makes owners; the seeded system user cannot sign in). Tested (integration).
- `demo/seed-demo.mjs`: fills a running system, through the real API, with Sunrise Cafe (Mumbai, Silver plan, default board), an owner and an executive, a live banner, a banner waiting for approval, three ads (owner, admin, executive), a price list.
- `demo/take-screenshots.mjs`: takes the 37 pictures used in the guides.
- `DEMO_START_HERE.md` (set-up in five commands, the three logins, reset, troubleshooting) and `docs/demo/{OWNER,EXECUTIVE,ADMIN}_WALKTHROUGH.md`: step by step with a picture per step.
- `docs/PROVIDERS_SETUP_GUIDE.md`: what to sign up for and what to hand over for e-mail (SMTP, already built, only settings), payments, SMS (incl. India DLT) and Azure.
- Fixed: clicking a component in the editor snapped it to the grid and marked the banner "Unsaved changes" (found while taking the pictures); unit test added.
- User guides corrected: banners are "Reject"ed on Approvals, ads are "sent back".

## Verified
- A clean start from nothing (volumes deleted, images rebuilt): admin created, demo seeded, pictures taken. A rehearsal of the owner and executive steps in Chrome passed (approve and publish, approve executive ad, override-but-not-cancel on the admin ad, phone number refused, executive menu).
- Domain 785, Application 262, Integration 76, Jest 767, tsc clean.

## Not done
- The pictures are of the demo data on 3 Oct 2026 (dates in them will look old later; re-run the two scripts to refresh).
- No video walkthrough.
