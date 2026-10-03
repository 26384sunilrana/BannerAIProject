# Runbook: what to do when something goes wrong

For whoever is on call. Each section matches an alert in `deploy/monitoring/prometheus-alerts.yaml` or a message the administrators get in the bell.
Commands assume Kubernetes (`kubectl -n bannerai ...`); with Docker Compose use `docker compose logs api` and `docker compose restart api`.

## Where to look first
| What | Where |
|---|---|
| Is the process up? | `GET /health/live` |
| Can it work? (database, media folder, jobs) | `GET /health/ready` (503 when the database or the media folder fails; a late job only shows "degraded") |
| Numbers | `GET /metrics` (token in production) |
| What happened | the application log (`kubectl logs deploy/banner-api`), and **Activity** in the admin screens (export as CSV for an investigation) |

## API down
1. `kubectl get pods`: is it crash-looping? `kubectl logs <pod> --previous`.
2. Most common causes: the database is unreachable (check `/health/ready` of another pod, the SQL firewall, a rotated password), the key ring volume is missing (encrypted data cannot be read), a bad release (roll back: `kubectl rollout undo deploy/banner-api`).
3. After a release, was the `banner-migrate` job run? A pod that starts before its migrations fails.

## Many errors
1. Look at the log for the first exception of the burst. 2. If it began with a release, roll back. 3. If it is the database, check its size, locks and the connection limit.

## A background job stopped
The job named in the message (`subscription-lifecycle`: reminders, renewals, grace and expiry; `media-cleanup`: abandoned uploads, old notifications, audit retention) has not finished well for three rounds.
1. Find its log lines ("Subscription lifecycle run failed", "Media clean-up failed") and the exception.
2. A restart of the pod starts the job again (`kubectl rollout restart deploy/banner-api`).
3. For the lifecycle job an administrator can also press **Run lifecycle now** on *Subscriptions*: it is safe to repeat.
4. Until it runs, reminders are late and plans that should have ended are still open: tell the owner of the product.

## Many failed sign-ins
1. Open **Activity**, tick *Failures only*, look at the addresses and the e-mail addresses.
2. One address: block it at the ingress or firewall. Many addresses against one account: unlock only when the owner confirms, and ask them to change the password and turn on two-step sign-in.
3. The rate limits already slow guessing (20 a minute per address); a lockout follows five wrong passwords or codes per login.
4. If it is real traffic (a big office behind one address), raise `RateLimiting__AuthPerMinute`.

## Ads waiting for review
Administrators: **Ads**, choose the shop, read the reason, **Approve** or **Send back**. An ad waits for a person: nothing runs it without a decision.

## Many logins locked
Often a shared password or a script. **Users, then Unlock** after checking with the owner. Look at Activity for the cause.

## The disk is full / uploads fail
`/health/ready` shows the media folder failing. Free space or enlarge the volume (`media-pvc.yaml`). Files are not lost by this: an upload that fails is refused with a message and cleaned up by the clean-up job.

## Lost the phone of an administrator
Another administrator: **Users, then Reset two-step**. The last administrator: `dotnet BannerService.dll --reset-two-factor <email>` (see `DEPLOYMENT.md`).

## A shop says its screen is blank
1. Open **Shop screen** as the owner on any computer: does it show? 2. Is the plan active (Subscriptions)? A shop whose plan ended shows only its default board. 3. Is the banner approved and scheduled for now (shop time zone)? 4. On the TV, reload the page and check the network.

## Before and after any release
Back up the database and the key ring first. Run `banner-migrate`. Roll out. Check `/health/ready`. Keep the previous image tag so you can roll back.
