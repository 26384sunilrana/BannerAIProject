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
  the new migration on a scratch server, then compare and script the differences.

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
