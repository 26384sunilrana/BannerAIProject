# Bolt 046 - Abuse limits

Decisions from the owner (3 Oct 2026): renewal is a "Renew" button for now (no payment option yet); no GST yet (no company); the application must never hold or show PHI/PII and ads must be verified; one screen per shop; the owner's Android screens are the target devices.

Built: per-minute limits (sign-in/sign-up by IP, refresh, upload pieces per user, general per user), 429 with Retry-After and a clear message, never on /health, refusals in the activity log; forwarded-header trust only when configured (needed behind an ingress); configmap updated; docs.
Verified: Integration 87 (guessing is slowed after 4 tries, sign-up shares the limit, API limit, health never limited, request classification). Limits are off in the test factories except one test.
Next: two-step sign-in (047), health checks/metrics (048), screen pairing/heartbeat/proof of play (049).
