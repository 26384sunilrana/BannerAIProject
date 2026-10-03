# Bolt 049: managed shop screens

- Device pairing by 6-character code; secret hashed on server; 1-hour Screen JWT limited by ScreenRestrictionMiddleware.
- One screen per shop (MaxScreensPerShop = 1). Owners pair/rename/remove; executives view.
- Heartbeat every 60 s gives proof of play per banner/ad/default board per UTC day (gap capped at 150 s).
- Watchdog notifies owners after ~10 min offline (once per 6 h). Retention via media-cleanup job.
- UI: /player (TV), /screens (owner), dashboard card, nav item.
- Tests: ScreenSmokeTests (integration, in-memory), screens.test.tsx (13 Jest tests).
- Not verified: browser E2E, SQL mode, Docker rebuild, real Android hardware.
