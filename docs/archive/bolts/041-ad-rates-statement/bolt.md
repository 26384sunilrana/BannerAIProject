# Bolt 041 - Ad rates, monthly statement, owner override

Status: complete

## Built
- **Ad rates** (admin screen "Ad rates"): an hourly price by place (every shop, country, state or city) and kind of ad (or every kind), plus the part of it the shop is paid. The narrowest place wins, then a rate for the kind before a rate for every kind. Side and mega ads are priced for the whole screen and cut down to the share they take; popups and corner tiles are a flat hourly price. A rate is switched off, never deleted.
- **Pricing an admin ad:** when the administrator sends an ad in, the rate for the shop's place is looked up and the hourly price and shop share are fixed on the ad. With no rate the ad cannot be sent in ("Set one under Ad rates first"). Later rate changes do not touch ads already booked. Owner and executive ads carry no price: the shop prices them as it likes and the platform does not track payment.
- **Monthly statement** (`/ads/statement`, owners and administrators; admin can choose one shop or all): for each shop, the hours each ad really ran in the month on the shop's clock (from approval until it ended, was cancelled or overridden, or now; daily hours and weekdays respected), and for administrator ads what the shop is paid (hours x hourly price x shop share). Own ads show hours and "not tracked".
- **Override:** the shop owner (not an executive) can stop an approved administrator ad that is not over, with an optional reason. The slot is free at once, the shop is paid for the hours it ran, the ad shows "Stopped by the shop", it is written to the ad history, and all administrators are told in the bell. The owner cannot cancel an administrator ad any other way.
- Migration `AdRatesAndPricing`.

## Decisions I took (change if wrong)
- The rate is "what an hour is worth" and the shop is paid a share of it (default 100%). The platform's own cut and what the advertiser pays are outside the application.
- Currency is not modelled: amounts are plain numbers.
- Hours count scheduled hours the ad was approved and not stopped; there is no measurement of what the screen actually played.

## Verified
- Domain 712 (12 new for rate rules), Application 262 (12 new: pricing, override, statement maths), Integration 60 (3 new; also on real SQL Server), Jest 746 (12 new), tsc and next build clean.
- Chrome against the real API on SQL Server, `ads.js` 21/21: an admin ad is refused while no rate exists; the admin adds a rate in the screen; the ad is sent in; owner's statement shows the ad at about 3 hours (backdated in the database) and pays 14 an hour (20% of 100, 70% of that); own ad "not tracked"; admin sees the statement; the owner overrides with a reason, the administrator is told with that reason, the ad leaves the screen at once.

## Not done
- Notify shops by location about a major ad (041b); HIPAA/PII checks on ad content (042); ads shown hours are scheduled hours, not measured.
- No export of the statement (CSV/PDF), no payout record ("paid" marking) - payments wait for the provider work (044).
- Admin ads are still text only (no pictures).
- An owner cannot yet replace the overridden ad with their own in one step: they override, then book.
