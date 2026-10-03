# Running and deploying Banner AI

## Local development (no Docker)
1. SQL Server running locally; `ConnectionStrings:DefaultConnection` in `src/BannerService/appsettings.Development.json` or user secrets (not committed).
2. `dotnet run --project src/BannerService` (uses the Development profile: migrations and seed data run at start-up, Swagger at `/swagger`).
3. `cd src/BannerUI && npm install && npm run dev` (API URL: `NEXT_PUBLIC_API_URL`, default `http://localhost:5000/api`).

## Local stack with Docker
```
cp .env.example .env        # set SQL_SA_PASSWORD and JWT_SECRET_KEY
docker compose up --build
```
API on http://localhost:5000, web app on http://localhost:3000, SQL Server on 1433.

## Tests
```
dotnet test tests/BannerService.Domain.Tests
dotnet test tests/BannerService.Application.Tests
dotnet test tests/BannerService.IntegrationTests              # in-memory database
SMOKE_SQL="Server=(localdb)\MSSQLLocalDB;Database=BannerAI_Smoke;Trusted_Connection=True;TrustServerCertificate=True" \
  dotnet test tests/BannerService.IntegrationTests            # same tests on a real SQL Server (runs migrations and seeding)
```

## Database migrations
- There is one baseline migration, `InitialCreate`, with a model snapshot. Add changes with
  `dotnet ef migrations add <Name> --project src/BannerService -o Infrastructure/Data/Migrations`
  (the design-time factory means no running database is needed).
- Apply them with `dotnet ef database update`, at start-up (default), or with `dotnet BannerService.dll --migrate`,
  which migrates, seeds and exits. In Kubernetes the `banner-migrate` Job does this and pods start with
  `Database__MigrateOnStartup=false`.
- **Existing databases** created from the earlier, attribute-less migrations or by `EnsureCreated` cannot take `InitialCreate`
  (tables already exist). For development, drop and recreate. For anything with data, build a baseline: create the database from
  the new migration on a scratch server, compare it with the old database (a schema-compare tool such as `sqlpackage /Action:DeployReport`),
  apply the differences by script, and only then insert the `InitialCreate` and later migration ids into `__EFMigrationsHistory`.
  This path has not been tried on a database with data; try it on a copy first.

## Shop screen playback
- A banner plays through `/display`: visual effects, rotating pictures and rotating videos run exactly as in Preview.
- Videos with sound: Chrome and Edge refuse to start a video with sound on a page nobody has clicked. The screen then plays the video **muted**
  rather than not at all. On a machine that only runs the screen, start the browser with `--autoplay-policy=no-user-gesture-required`
  (for example `chrome.exe --kiosk --autoplay-policy=no-user-gesture-required http://your-site/display`) to allow sound.
- Links to pictures and videos last 4 hours; the screen asks for fresh ones after 3 hours without a visit to the server.

## Sign-in session (cookie)
- The web app keeps the short-lived access token (15 minutes) in the page's memory only. The long-lived refresh token (7 days) is an
  **HttpOnly cookie** (`banner_refresh`, path `/api/authentication`) that scripts cannot read. A page reload or a new tab gets a new
  access token by sending that cookie; signing out clears it and ends the session on the server.
- Forgery protection: calls that rely on the cookie (refresh, sign-out) must carry an `X-Requested-With` header, and the cookie is
  `SameSite=Lax`. Cross-origin access with credentials is granted only to the origins in `Cors__AllowedOrigins__n`
  (development with no list allows localhost on any port; production allows none).
- Settings (`Auth:Cookie:*`): `Enabled` (default true; false returns the refresh token in the response body for non-browser clients),
  `Name`, `SameSite` (Lax, Strict, None), `Secure` (default: on in Production, off otherwise), `Domain`.
- The web app and the API must be on the **same site** (same registrable domain; ports and sub-domains may differ). Through the Kubernetes
  ingress they share one host. If they are on different sites, set `Auth__Cookie__SameSite=None` (needs `Secure=true`, so HTTPS).
- Running Production mode over plain HTTP (for example a local compose stack): set `Auth__Cookie__Secure=false`, or browsers drop the cookie.
- Two tabs that refresh at the same moment both succeed: a refresh token that was just replaced is accepted again for 30 seconds
  and returns the same replacement.
- Ending sessions: signing out, changing the password, signing out everywhere, deactivating a user, moving a user or handing a shop over
  all revoke refresh tokens. An access token already issued stays valid until it runs out (at most 15 minutes).

## Media
- Files are stored on local disk under `MediaService__LocalStoragePath` (a shared, persistent volume when more than one pod runs). Azure Blob comes last.
- Picture size and video length are read from the file headers; no outside tool is needed.
- A clean-up job runs every `Media:Cleanup:IntervalHours` (default 6; `Media:Cleanup:Enabled=false` turns it off). It removes uploads that were started
  but not finished within `Media:Cleanup:AbandonedUploadHours` (default 24) and drops the record of a deleted file after `Media:Cleanup:KeepDeletedRecordsDays` (default 30).
  An administrator can run it at once with `POST /api/media/admin/cleanup`.
- A file that a banner (or an earlier version of a banner) uses cannot be deleted. Deleting on Windows while a browser is still streaming the file
  works; the bytes are removed by the next clean-up run.

## Secrets
- Nothing secret is stored in the repository: `appsettings.json` has an empty connection string and an empty `Jwt:SecretKey`,
  and the API refuses to start without them.
- Local development: copy `src/BannerService/appsettings.Development.example.json` to `appsettings.Development.json` (that file is git-ignored). The example points at LocalDB (`(localdb)\MSSQLLocalDB`, Windows sign-in, no password)
  with a signing key that is only for development. To use another SQL Server, set `ConnectionStrings__DefaultConnection`
  as an environment variable or with `dotnet user-secrets`.
- Everywhere else the two values come from the environment: `docker-compose.yml` reads them from `.env`, and Kubernetes reads them
  from the `banner-secrets` secret (`deploy/k8s/secret.example.yaml`).

## Continuous integration
`.github/workflows/ci.yml` runs on every push to master and every pull request: API build and the three .NET test projects,
the same integration tests against a SQL Server container, and the web type check, Jest tests and production build.
It has not run on GitHub yet; the commands in it were run locally.

## Configuration that must be set outside development
| Setting | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server / Azure SQL |
| `Jwt__SecretKey` | 32+ random characters; production refuses to start with the placeholder |
| `Security__KeyDirectory` | Shared, persistent folder for encryption keys (see below) |
| `Cors__AllowedOrigins__0..n` | Only if the web app is on a different origin; production allows none by default |
| `Security__RequireHttpsRedirect` | `false` behind a TLS-terminating ingress |
| `Swagger__Enabled` | `true` to expose Swagger in production |

### Renewals, reminders and the shop screen
A background job (`SubscriptionLifecycleWorker`, every `Subscriptions__LifecycleIntervalMinutes`, default 60; switch off with
`Subscriptions__LifecycleEnabled=false`) renews subscriptions with automatic renewal, sends reminders (email and text, 7, 3 and 1
days before an ending that will not renew itself), starts a one-week grace period when the date passes unpaid (daily reminders;
the shop shows only its default banner), and when the week is over expires the subscription, ends the shop's sessions and
refuses its logins. Platform admins are never locked out. An admin can run the job at once with
`POST /api/subscriptions/admin/run-lifecycle` and reactivate a shop with `POST /api/subscriptions/{id}/renew`.

Three things must be connected before this is real:
- **Email:** set `Email__Smtp__Host` (plus `Port`, `User`, `Password`, `From`, `EnableSsl`). Without it nothing is emailed; the attempt is
  recorded in `SubscriptionNotifications` as not delivered.
- **Text messages:** only a logging stand-in exists (`ISmsSender`). Add a provider implementation and register it.
- **Payments:** `IPaymentGateway` is a stand-in that reports every charge as paid (`NoPaymentGateway`), so automatic renewals and
  "Renew now" cost nothing. Replace it before taking money.

The shop screen is `/display`: sign in once on the screen's machine. It shows the live banner, switches by schedule, and falls back
to the default board (shop name and clock), which is kept in that browser, when nothing is scheduled, the plan has ended, or the
server cannot be reached.

### Uploaded media
Images and videos are stored as files under `MediaService__LocalStoragePath` (`/media` in the containers), not in the database.
Mount the same persistent volume on every API pod (`banner-media`, `ReadWriteMany`) and include it in backups. The browser loads
files through short-lived signed links (`/api/media/{id}/download?expires=...&sig=...`) signed with `Media__SigningKey`
(defaults to a key derived from `Jwt__SecretKey`). Only PNG, JPEG, GIF, WebP, MP4 and WebM are accepted, the content is checked
against its type, and uploads are limited to 500 MB (sent in 8 MB pieces). Azure Blob storage is not implemented.

### Encryption keys
Personal and payment fields are encrypted with ASP.NET Data Protection. **Losing the key ring makes that data unreadable.**
Back up the `Security__KeyDirectory` contents, mount the same volume on every API pod (the `banner-keys` claim is
`ReadWriteMany`), and never store it inside the container image. A stronger setup protects the key ring with Azure Key Vault
(`ProtectKeysWithAzureKeyVault`) and keeps it in Blob Storage; that needs the `Azure.Extensions.AspNetCore.DataProtection.*`
packages and has not been added.
Also turn on Transparent Data Encryption for Azure SQL (on by default) and TLS everywhere.

## Kubernetes (Azure AKS)
Manifests are in `deploy/k8s` (kustomize).
1. Build and push `src/BannerService` as `banner-api` and `src/BannerUI` as `banner-web`
   (`--build-arg NEXT_PUBLIC_API_URL=https://<host>/api --build-arg NEXT_PUBLIC_API_BASE_URL=https://<host>/api`).
2. Create the secret from `deploy/k8s/secret.example.yaml` (from a vault, not from git).
3. Set image names/tags (`kustomize edit set image ...`) and the host in `ingress.yaml`.
4. `kubectl apply -k deploy/k8s`. The `banner-migrate` Job runs the migrations; re-create it for each release.
5. Needs an ingress controller (nginx), cert-manager or a TLS secret named `banner-tls`, and the `azurefile-csi` storage class.

## Checked vs not checked
- Verified: the solution builds; migrations apply to SQL Server LocalDB; the API runs its sign-up, role, tenant and audit flows
  against SQL Server (smoke tests); `--migrate` mode; `docker compose config`; `kubectl kustomize deploy/k8s` renders.
- Also verified: the web app type-checks and `next build` succeeds, and the sign-up, login, subscription, team and approval
  screens were driven in Chrome against the real API and SQL Server (28 checks).
- Not verified: Docker image builds (the Docker daemon was not running) and anything on a real cluster.

## Time zones and the default board
- Time zone names are IANA names. The API image needs time zone data (`tzdata`); a slim Linux image without it cannot read the zones. Install it in the image.
- A shop uses its own zone, else its city's, else its country's, else UTC. Set the country and city zones in Admin, Places.
- The shop screen keeps the default board design (and the logo, up to about 400 KB) in the machine's browser storage, so it still shows with no connection.

## Personal data: encryption, audit retention, passwords
- After switching field encryption on for a database that already has data, run once: `dotnet BannerService.dll --encrypt-existing`
  (a Kubernetes Job like `--migrate`). It encrypts phone numbers, addresses, postal codes and payment references saved before; running it again changes nothing.
  Back up the key ring (`Security:KeyDirectory`) apart from the database: without it encrypted values cannot be read.
- Activity log: kept `Audit:RetentionDays` days (default 2190, six years); `0` keeps it for ever. Administrators can export it as CSV from Activity.
- Passwords are re-hashed with the current strength at the next sign-in, so no migration step is needed.
- The API sends security headers (no content-type guessing, no framing, no referrer, no caching of API answers, HSTS in production); the web app sets its own in `next.config.js`.
- See `memory-bank/intents/002-requirements-gap-closure/hipaa-review.md` for the review and what a person still has to do.

## Static review of the images and manifests (bolt 043, Docker was not running)
- Found and fixed: the API Dockerfile's `ENV` line had a stray `\n` in place of a line break, so the image would not have built.
- `deploy/k8s/encrypt-existing-job.yaml` runs `--encrypt-existing` once (not in the default kustomization).
- The runtime image `mcr.microsoft.com/dotnet/aspnet:8.0` (Debian) carries time zone data; do not switch to an Alpine or chiseled image without adding `tzdata`.
- Several API pods may run: the daily subscription reminders and the media clean-up are safe to run twice (unique claims and repeatable steps); migrations belong to the `banner-migrate` Job only.
- Verified later (Docker Desktop running): both images build; `docker compose up` starts SQL Server, the API (migrations applied) and the web app; the browser scripts pass against the containers (scheduling 13/13, takeover 16/16, media 11/11, subscription lifecycle 20/20, ads 21/21, compliance 11/11); `--migrate` and `--encrypt-existing` run from the image.
- NOT done: running in a Kubernetes cluster, two API pods at once, and a real MP4 playback check (no video encoder on this machine; the media scripts use a hand-made MP4 header).

## Abuse limits (rate limiting)
- Per minute: sign-in and sign-up 20 per IP address (`RateLimiting__AuthPerMinute`), session refresh 60, upload pieces 600 per user, everything else 1200 per user (or per address when nobody is signed in). Turn off with `RateLimiting__Enabled=false`. Refusals answer 429 with `Retry-After` and are written to the activity log.
- Behind an ingress or load balancer set `Proxy__TrustForwardedHeaders=true` (the Kubernetes configmap does) so the real caller's address is used. Never set it when the API is reachable directly: the header can be forged.
- Many shop screens behind one office or mall address share the 1200 limit only when not signed in; signed-in screens are counted per user.
