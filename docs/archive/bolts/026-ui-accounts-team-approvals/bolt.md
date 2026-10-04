---
id: 026-ui-accounts-team-approvals
unit: banner-editor-ui
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [UI for sign-up, login, team, approvals]
---

# Bolt: 026-ui-accounts-team-approvals

## Screens
- /register: shop-owner sign-up (name, email, password, shop name, optional city and phone) with field validation.
- /login: sign in, return to the page the visitor came from (same-site addresses only).
- /dashboard: role-aware home. Navigation shows only what the role may use.
- /subscription (owner): plans with monthly, quarterly, half-yearly and yearly prices, subscribe, change plan (from tomorrow), change period, auto renewal on/off.
- /team (owner): add up to two sales executives (needs a subscription), remove one, choose approvers (owner, either executive, or both; never none).
- /banners: create a banner, open the editor, submit for approval.
- /approvals: banners by status (Needs action / Live / All). Approvers approve or reject (reason required) and publish or unpublish; others see "Waiting for an approver". History per banner.

## Plumbing
- Session: tokens in local storage, claims decoded for roles and shop, automatic refresh after a 401 (one refresh at a time), sign out clears everything.
- API client: unwraps both { success, data } and plain responses; shows the server's message for errors.
- Backend changes needed by the UI: refresh-token works from the refresh token alone (it required a user id from an expired access token, so it could never work); GET /api/shops/{id}/team/my-role tells any member whether they own the shop and may approve.
- Fixed so the web app builds: removed a custom webpack rule that broke CSS modules, excluded tests from the Next type check, fixed 16 type errors, chunk uploads now send the MD5 header the server requires.
- package-lock.json added; @testing-library/user-event added (existing tests imported it).

## Verification
- Chrome (headless) against the real API on SQL Server LocalDB: 28 checks pass, covering sign-up, duplicate email, subscribe, quarterly price, add two executives and the limit, approver choice, create/submit/approve/publish, role-limited navigation, non-approver view, rejection with reason, wrong password, return address, reload, and token refresh.
- Jest: 36 new tests pass. The whole suite is 380 passing, 118 failing; the 118 failures existed before this work (they were 118 before and after).

## Not done
- Payment: subscribing activates the plan immediately; there is no payment gateway.
- The banner editor itself still talks to the API with its own field names (title, x, y, ...) while the API uses name, positionX, ...; editor saving is not verified against the real API.
- No forgot-password, email verification or account settings screens.
- Admin screens for users, shops and audit logs are not built (shops and plans pages existed).
- Tokens are in local storage; a cookie-based session would be safer against script injection.
- The 118 failing legacy Jest tests were not repaired.
