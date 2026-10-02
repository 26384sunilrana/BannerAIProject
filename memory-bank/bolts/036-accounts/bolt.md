# Bolt 036 - Accounts (without the email parts)

Status: complete. Forgot password, email verification and self-service renewal wait for the providers (bolt 044), as decided.

## Built
- **Cookie session.** The refresh token moved out of local storage into an HttpOnly, SameSite=Lax cookie limited to `/api/authentication`;
  the access token lives in page memory only.
  - Reload and new tabs pick the session up from the cookie; signing in or out in one tab updates the others.
  - Forgery protection: cookie-based refresh and sign-out need an `X-Requested-With` header; credentials are allowed only to named origins.
  - Concurrent refreshes (two tabs) both succeed: a just-replaced refresh token is accepted for 30 s and returns the same replacement.
  - Sign-out works with an expired access token and always clears the cookie.
  - Settings `Auth:Cookie:*`; `Enabled=false` keeps the old body-token behaviour for non-browser clients. Old tokens left in local storage are deleted.
  - The shop screen (`/display`) waits for the session lookup, and a screen that started offline retries every minute.
- **My account** (`/account`): details (name, phone), change password (checks the current one; wrong guesses count towards the five-try lock;
  ends every other session, keeps this device signed in), sign out everywhere.
- **Hand the shop over:** the owner picks a sales executive on the Team page and confirms with their password; roles swap (the previous owner
  becomes a sales executive), approver lists are fixed up, both people are signed out and the sign-in page says why. An administrator can do it without a password.
- **Move a sales executive to another shop (administrator):** the target needs a running subscription, an active status and a free login (max 2);
  owners, administrators and switched-off logins cannot be moved; the person is signed out.
- Fixed: `GET /me` read the wrong claim name for the shop.

## Verified
- Application 222 (34 new), Integration 28 (13 new, run against the real application incl. CORS pre-flights, also on SQL Server LocalDB), Jest 606, tsc clean, next build OK.
- Chrome against the real API: 21/21 new checks (cookie flags, no token in storage, reload, new tab, sign-out syncs tabs, details persist, wrong/right password,
  other browser signed out, old password dead, hand-over with wrong then right password, roles/menus swap, sign out everywhere, shop screen survives a reload)
  and the earlier admin (19), places (19) and files (13) scripts still pass.

## Not done
See pending.md section C (email-based flows, per-device session list, hand-over acceptance, token claim lag, password hashing strength).
