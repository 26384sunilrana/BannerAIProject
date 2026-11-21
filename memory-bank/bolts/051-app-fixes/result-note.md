# Result note: bolt 051 three application fixes

**Date**: 2026-11-21. Addresses section E, items 1 to 3, of `memory-bank/bolts/050-demo-refresh/drift-list.md` (bolt 050's own folder is not changed).

1. **Menu bar twice on Ad rates and Ad report**: fixed (second page frame removed) and guarded by a test.
2. **429 after quick navigation**: fixed. "Who am I" is an ordinary call (1200 a minute per signed-in person); sign-in, sign-up and the rest stay at 20 a minute per address. Proven live: 25 of 25 answered, sign-in refused from the 20th permit. The demo story no longer waits for the limit (two runs, none).
3. **Currency**: one display (formatMoney) everywhere. Plans have a Currency choice (INR default); ad rates and ads store a currency with the amount (migration AddAdCurrency, INR for existing rows); the statement adds up only the platform currency and marks other lines.

**Counts**: web 830 to 839, application 396 to 416, integration 137 to 143, domain 809. Story 42/42 twice (f2cuj, f51ov), pictures 46/46, on a freshly wiped database.

**Still open, for later**: no conversion between currencies; the new migration on a database that already holds ads; a lint setup for the web project.
