# Bolt 041b - Book an ad on every shop of a place

Status: complete (not driven in Chrome; see below)

## Built
- The administrator can book one ad on every active shop in a city, a state, a country or everywhere ("Book for a whole place" on the Ads page, `POST api/shop-ads/campaign`).
- Each shop is booked on its own and priced by its own rate. A shop where the ad cannot go (screen taken, no rate) is skipped, its stray draft is cancelled, and the result lists every skipped shop with the reason. The rest are booked.
- Every shop owner gets the in-app message; a mega ad says "A major ad was booked on your screen" (requirement 4.20, notify shops by location of a major ad).
- Limits: at most 500 shops per campaign (choose a smaller place), text-only, admin only.
- The space returning when an ad ends (4.21) is covered since 040b: the live list is worked out from time.

## Verified
- Integration 61 (1 new through the real application: two shops in a city, one already taken; skipped with the reason; the owner told; a shop in another place untouched), Jest +3, tsc clean.

## Not done
- No Chrome run of the campaign form (the form is the same AdForm as before plus a place chooser).
- The dates of a campaign are read on the clock of the computer being used, because shops in one place can have different clocks; a note on screen would help.
- One notice per ad per owner; no digest.
