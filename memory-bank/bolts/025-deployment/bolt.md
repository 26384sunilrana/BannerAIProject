---
id: 025-deployment
unit: banner-service
intent: 002-requirements-gap-closure
type: simple-construction-bolt
status: complete
completed: 2026-10-01T00:00:00Z
closes: [Docker and Kubernetes, local then Azure]
---

# Bolt: 025-deployment

## Delivered
- Working migrations: the unapplied, attribute-less migrations were replaced by one InitialCreate with a model snapshot, generated with dotnet ef (design-time factory, no database needed) and applied to SQL Server LocalDB. Building it exposed and fixed model defects: Country.ISOCode vs State.CountryCode length, three SQL Server multiple-cascade-path errors (CarouselComponents, Shop parent, billing tables), Component.BannerId1 shadow foreign key, and unset decimal precision on invoice and subscription prices.
- Start-up: --migrate mode (migrate, seed, exit) for a Kubernetes Job; Database:MigrateOnStartup switch; outside development a migration failure now stops the process instead of serving a broken API; CORS limited to Cors:AllowedOrigins (production default: none); Swagger off in production unless enabled; HTTPS redirect switchable; a development launch profile.
- Packaging: API Dockerfile (non-root, keys and logs on volumes), web Dockerfile with API URL build args, .dockerignore files, docker-compose.yml (SQL Server + API + web) with .env.example.
- Kubernetes (kustomize, renders 11 objects): namespace, config, secret template, shared key volume claim, API deployment + service + autoscaler + disruption budget, web, ingress, migration Job.
- Real-database check: the integration smoke tests run against SQL Server when SMOKE_SQL is set (passed on LocalDB, including migrations and seeding).
- DEPLOYMENT.md documents local, Docker, migrations, required settings, key handling and what was and was not verified.

## Not verified / not done
- Docker images were not built (Docker daemon not running); the web app was not built (no node_modules in the repository).
- Nothing was applied to a cluster.
- Existing databases built from the old model cannot take InitialCreate; development databases should be recreated (see DEPLOYMENT.md).
- Encryption keys are on a shared file volume; Azure Key Vault protection is not wired.
- No CI pipeline.
