# Bolt 048 - Health checks, metrics, alerts

Built: /health/live and /health/ready (database, media folder, background jobs), Kubernetes probes switched to them, /metrics (prometheus-net: requests plus shops, ads waiting, locked logins, refused sign-ins, rate-limit refusals, job last success/late), token-protected in production, background jobs report heartbeats, a watchdog that tells administrators about failed-sign-in spikes and stopped jobs (once an hour each), Prometheus alert rules, runbook.
Verified: Integration 103 (11 new). Not run against a real Prometheus.
Not done: log shipping, tracing (OpenTelemetry), dashboards, per-screen offline alerts (bolt 049).
