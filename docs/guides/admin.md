# Administrator Guide

For the product owner who runs BannerAI for many shops: users, plans and subscriptions, places, ads and their prices, compliance reviews, takeovers
and the activity log.

This guide matches the application as built through bolt 042b (October 2026).

## Contents
1. Signing in and the menu
2. Users
3. Plans and subscriptions
4. Places (country, state, city, group)
5. Shops
6. Ads: booking, prices, statement, report
7. Compliance review of ads
8. Shop takeovers
9. Activity log and export
10. Messages (the bell)
11. Running the system: operations notes
12. Not available yet

## 1. Signing in and the menu
Sign in with your administrator login. The menu: Home, Ads, Shops, Places, Ad rates, Ad report, Plans, Subscriptions, Users, Activity, Takeover.
Administrators do not belong to a shop. The bell shows messages for you.

## 2. Users
**Users** lists logins with search (name, e-mail, shop). For each: **Deactivate / Activate**, **Unlock** (after five wrong passwords) and a link to their
**activity**. You cannot deactivate yourself. **Move** sends a sales executive to another shop (that shop needs an active plan and a free place), and
**Make owner** makes a member of a shop its owner (the previous owner becomes an executive); everyone involved has to sign in again.

## 3. Plans and subscriptions
- **Plans**: Basic, Silver, Gold, Platinum with price per month and per year (quarterly and half-yearly are worked out from the year), banners, shops, users and
  **storage** limits. A shop with no plan has the starter allowance of 1 GB.
- **Subscriptions** lists every shop's plan, status (Trial, Active, Renewal pending, Payment failed, Grace period, Suspended, Expired, Cancelled) and renewal date; filter by status or shop.
  **Run lifecycle now** applies reminders, grace and expiry at once (it also runs by itself every day). An expired or suspended shop cannot sign in; its screen
  keeps the default board.

## 4. Places
**Places** is the hierarchy: country → state → city → group (a group is a part of a city). Each has a unique identifier (CTY-…, GRP-…). Add, rename,
switch off or delete (a place with shops or children in it cannot be deleted). Set the **time zone** of a country or city: shops follow their city, then their
country, unless the shop sets its own.

## 5. Shops
**Shops** lists every shop with status and plan; shops are created by owners signing up. You can edit details, deactivate a shop, or archive it.
Two shops may share a name; one name at one address is one shop (see Takeovers).

## 6. Ads
### Booking
**Ads** → choose a **Shop** → **Book an ad** (business name, headline, text, kind, colours, dates and hours) → **Save as draft** → **Send in**. Your ads are
approved when sent in and the shop owner is told. Admin ads are **text only** for now. **Book for a whole place** books the same ad on every active shop in a
city, a state, a country or everywhere (up to 500 shops); each shop is priced by its own rate, and shops where it cannot go (screen taken, no rate) are
listed with the reason.
You see **every** ad on the shop, including the ones the owner and executives booked, marked by who booked them, so you can plan around them. A mega ad needs
the screen to itself; side strips share up to half; one popup at a time; one tile per corner.

### Prices (**Ad rates**)
A rate is the price of **one hour** for a place (every shop, a country, a state or a city) and a kind of ad (or every kind), and the **share paid to the shop**.
The narrowest place wins, then the kind. Side and mega ads are priced for the whole screen and cut down to the share they take. An ad cannot be sent in
until a rate covers its shop. Changing or switching off a rate does not touch ads already booked: they keep the price they were booked at.

### Statement
**Ads → Monthly statement**: for each shop, the hours every ad ran in the month on the shop's clock and, for your ads, what the shop is paid
(hours × price × share). The shop's own ads show hours only. Filter by shop; the city totals are shown. A shop owner can **override** one of your ads for a better
local offer: it stops at once, the shop is paid for the hours it ran, and you get a message.

### Report
**Ad report**: for the month, how many ads each person booked, how many went live, were sent back or cancelled, are waiting, and how many were held for review.

## 7. Compliance review of ads
Ads must not carry personal or health information. The program refuses phone numbers, e-mail addresses, card numbers, identity numbers and dates of birth.
Wording about patients, diagnoses, prescriptions, diseases and the like holds the ad as **Waiting for compliance review**: you get a message, open **Ads** (choose the
shop), read the reason, and **Approve** or **Send back** (a reason is required). The history of the ad records who did what and when. The screening cannot see names,
street addresses or text inside pictures: it is a safety net.

## 8. Shop takeovers
**Takeover** lists every request. A new owner asks with the shop's name and address; the old owner confirms with their password and chooses whether the
associates stay (only the owner changes) or change too (old shop closes, new shop takes its address). If the old owner cannot be reached, **Confirm for the owner** with
a written reason (for example, the papers were sent to you). Answer carefully: the old logins are switched off and the plan stops renewing.

## 9. Activity log and export
**Activity** shows who called what, newest first, with filters for dates, user, shop and failures. **Export as CSV** downloads the same entries (up to
100,000). Entries are kept six years by default (`Audit:RetentionDays`). Bodies are never stored.

## 10. Messages (the bell)
You are told when an ad needs a review, when an owner overrides an ad of yours, and when a shop changes hands.

## 11. Running the system
See [deployment guide](../operations/deployment.md) for secrets, the `--migrate` job, the `--encrypt-existing` command, retention, media storage and the shop-screen notes, and
`memory-bank/intents/002-requirements-gap-closure/hipaa-review.md` for the personal-data review and what a person still has to do.

## 12. Not available yet
Payments, e-mail and SMS providers, forgot-password and e-mail verification, self-service renewal, Azure storage and Key Vault (bolt 044, after a full human
review); pictures on admin ads; currency; statement export; marking a statement as paid.
