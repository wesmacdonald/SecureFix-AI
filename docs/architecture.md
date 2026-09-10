# Architecture

SecureFix AI uses a controlled workflow:

1. ingest a vulnerability alert
2. validate and normalize the payload
3. assess risk deterministically
4. generate an AI recommendation
5. build a draft PR proposal
6. wait for human approval
7. record audit and governance data

## Target shape

- React and TypeScript dashboard built with Vite and served by the API as static assets
- ASP.NET Core API for ingestion, dashboard queries, and workflow actions
- service layer for business logic and read-optimized dashboard aggregation
- repositories for persistence abstraction
- security layer for demo or Microsoft Entra identity and role authorization
- governance layer for approval, audit, and reporting
- observability layer for logs, health, readiness, and process metrics

The dashboard and API use same-origin requests in the integrated deployment. UI role gating improves usability, but API authorization remains authoritative. Durable dashboard counts come from persisted workflow records; `/metrics` remains Admin-only process telemetry.

## Deployment path

Hackathon demo: one container serving the compiled dashboard and API, with SQLite and local/mock providers.

Production target: separately scalable containers with managed identity, managed queueing, PostgreSQL, secrets management, and centralized telemetry.
