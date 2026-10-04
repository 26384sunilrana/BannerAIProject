# Bolt 033 - Location hierarchy (closes requirements 2a-f)

Status: complete

## Built
- **Hierarchy:** Country > State > City > Group > Shop. New `City` and `LocationGroup` tables; shops carry `CityId` and `GroupId`.
  A group is an area inside one city (decision: "by location"); a shop's group must belong to the shop's city.
- **Unique identifiers** at every level, platform-wide and never changed: `CNT-`, `STA-`, `CTY-`, `GRP-`, `SHP-` plus eight characters
  that leave out look-alikes (0/O, 1/I/L, U). Made in code, guaranteed by unique indexes, retried on a collision.
  A shop gets its `SHP-` identifier when it takes a subscription; existing countries, states and subscribed shops are backfilled at start-up.
- **CRUD** (`/api/locations`): anyone signed in reads the active places; only an administrator creates, renames, switches off/on or deletes.
  Delete is refused while anything is under the place (states, cities, groups or shops), with the reason; switch it off instead.
  Duplicate names inside the same parent are refused (case-insensitive). A child cannot be added under, or switched on beneath, a place that is off.
  Renaming a city updates the city name kept on its shops.
- **Shop placement:** `PUT /api/locations/shops/{shopId}` (owner of that shop or admin). The shop's state and country follow the city.
- **Screens:** admin "Places" (four linked columns, identifiers, counts, rename/switch/delete, shops in a group); `LocationPicker`
  (country > state > city > group); shop create (admin) and edit (owner/admin) pages; shop page shows its identifiers; admin "Shops" lists all shops; owner menu gets "My shop".
- Migration `LocationHierarchy`.

## Fixed on the way
- Shop status was sent as a number (1/2/3) but the web app compared it with "Active": counts and badges were wrong. Now sent as a name.
- The "Create Shop" and "Edit" buttons led to pages that did not exist; both pages now exist.
- The admin "Shops" page showed only the admin's own shops, and the list total was the page size. It now lists all shops with a real total.
- Any member of a shop could edit the shop's details; now owner or admin only.

## Verified
- Domain 568, Application 172, Integration 14 (also on SQL Server LocalDB with real migrations and seeding), Jest 558, tsc clean, next build OK.
- Chrome against the real API: 19/19 checks (build the hierarchy, refuse duplicates, place a shop, subscribe and get the SHP identifier, group lists the shop, in-use delete refused, switch-off hides from owners, unused group deleted, admin vs owner buttons).

## Not done
See backlog.md (district level left unused, owner cannot request a new city, no import, no search by identifier).
