# Bolt 038 - Time zone, daily hours, calendar, default board

Status: complete (browser run not done, see below)

## Built
- **Shop time zone**, found as shop, then city, then country, then UTC. Editable on the shop form, in Places (country and city) and by API. India defaults to Asia/Kolkata.
- **Daily hours and weekdays** on a banner schedule: a date window plus optional hours each day (may pass midnight) and days of the week, all read on the shop clock. Overlap and "is it live" checks work on real occurrences, with daylight saving handled. Two banners may share dates when their hours do not meet.
- **Schedule calendar** (`/banners/calendar`, `GET schedule/calendar`): a week view in the shop clock, faded when not yet approved.
- **Default board designer** (`/default-board`, owner): message, colours, logo from My files; shown on `/display` and kept on the machine, with the logo as data, so it also shows offline. A logo in use blocks deleting that file.
- Migration `SchedulingAndShopSettings`.

## Verified
- Domain 666, Application 239, Integration 47 (also against real SQL Server on a fresh database), Jest 708, tsc clean, next build OK.
- Two integration tests were fragile on a shared real database (a shared state, an unordered pick of the clashing banner): fixed.

## Not done
- Not driven in Chrome against the real API (time zone, hours, calendar, designer, offline board). The earlier browser scripts were not re-run.
- Re-running the SQL-mode integration tests on a database that already holds an earlier run fails (fixed e-mail addresses); use a fresh database each time.
- Restore-version keeping the last approved version live is still open (pending.md).
