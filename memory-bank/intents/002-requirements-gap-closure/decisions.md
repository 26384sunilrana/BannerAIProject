# Decisions made by the product owner

| Date | Area | Decision |
|---|---|---|
| 2026-10-02 | Providers (payments, SMS, email) | None chosen yet. Done last (bolt 044). Stand-ins stay until then. Anything needing real email (verification, reset mail) waits too. |
| 2026-10-02 | Location | A Group is fixed by location, inside a city. Time zone is derived from the city, editable. |
| 2026-10-02 | Media | Local disk now. Azure Blob and Key Vault come last, after a full human review. |
| 2026-10-02 | Who creates ads | Admin, shop owner, and the owner's sales executives. Advertisers are not users; they contact the owner or the admin. |
| 2026-10-02 | Ad charges | Admin ads: price from admin-configured location rates; the shop is paid monthly for the hours the ad ran (statement, no gateway). Owner/executive ads: priced freely, no payment tracking. |
| 2026-10-02 | Ad slot booking | A slot booked by an owner or executive shows to the admin as booked for that time. |
| 2026-10-02 | Override | The owner may override an admin ad; the admin is notified in the app. |
| 2026-10-02 | HIPAA / PII on ads | Ads must not carry PHI or PII. Validation, approval and an audit log of every create / approve / override / change. |
| 2026-10-03 | Shop takeover | Same name at a different address is allowed. Same name and same address is a takeover confirmed by both owners: associates stay = swap owner and re-verify approvers; associates change = old shop closed, new shop created. |
| 2026-10-03 | Screens | Own browser as the device, to run on Android screens. One screen per shop for now. |
| 2026-10-03 | Payment | A "Renew" button click only until a payment provider exists. |
| 2026-10-03 | GST | No company yet, so no GST handling. |
| 2026-10-03 | PHI / PII | The system must never hold PHI or PII, and must never show it on screen, in ads or banners. It has to be verified. |
