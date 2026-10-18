# SecureFix AI

SecureFix AI is an AI-assisted vulnerability release workflow for the DevOps for GenAI Ottawa 2026 hackathon.

It is designed to demonstrate:

- how a vulnerability alert is ingested and normalized
- how severity and risk are assessed deterministically
- how AI can recommend a remediation without being allowed to approve it
- how a draft pull request can be proposed safely
- how human approval gates the final action
- how the system stays auditable, observable, and fail-safe

## Core workflow

```text
Dependabot or JSON alert
  -> validate and normalize
  -> risk assessment
  -> AI remediation recommendation
  -> draft PR proposal
  -> human approval
  -> optional human-reviewed merge
  -> audit trail and governance report
```

## Production questions

### How would this be deployed?

Hackathon demo: a React dashboard and ASP.NET Core API shipped as one container, with SQLite and local/mock providers.

Production path: the integrated dashboard/API container can run on Azure Container Apps or AKS, with workers scaled separately and backed by managed messaging, PostgreSQL, managed identity, secrets management, and centralized observability.

### How would this be secured?

The system validates all input, treats advisory content as untrusted data, isolates system prompts from payload text, masks secrets, enforces role-based authorization, and blocks AI from approving, merging, or deploying.

### How would this be governed?

Every recommendation is advisory only. Human approval is mandatory. Policy, reviewer identity, correlation ID, and decision history are recorded in an audit trail and governance report.

### How would this be monitored?

The service exposes `/health`, `/ready`, and Admin-only `/metrics` endpoints, structured logs, and metrics for requests, recommendations, approvals, rejections, AI calls, and failures.

### How would failures be handled?

AI timeout or invalid output falls back to a rules-based recommendation and requires human review. Unsafe actions are blocked. Kill-switch behavior disables action adapters while preserving read-only analysis and audit access.

## Scope

### Must have

- JSON and sample Dependabot payload ingestion
- payload validation and normalization
- deterministic risk scoring
- AI remediation recommendation with fallback
- draft pull request proposal
- mandatory human approval
- structured audit logging with correlation IDs
- health and metrics endpoints
- CI checks for tests and security scanning

### Out of scope

- automatic merge
- automatic production deployment
- autonomous approval
- automatic production deployment from the dashboard
- direct merge or release actions

## Repository guide

- [docs/implementation-roadmap.md](docs/implementation-roadmap.md) - delivery plan and phase gates
- [docs/architecture.md](docs/architecture.md) - target architecture
- [docs/security-model.md](docs/security-model.md) - security controls and authorization model
- [docs/governance-model.md](docs/governance-model.md) - approval and audit model
- [docs/threat-model.md](docs/threat-model.md) - threat analysis and mitigations
- [docs/ai-system-card.md](docs/ai-system-card.md) - AI system card
- [docs/runbook.md](docs/runbook.md) - rollback, recovery, and escalation
- [infra/README.md](infra/README.md) - Azure Container Apps deployment instructions

## Demo expectations

The intended demo should show:

1. a valid alert flowing through validation and risk assessment
2. an AI recommendation that remains advisory
3. a draft PR proposal generated from the recommendation
4. the workflow stopping at pending approval
5. a reviewer approving or rejecting the action
6. audit events and governance output for the full flow
7. a blocked prompt-injection or unauthorized-action attempt
8. a fallback path when AI is unavailable

## Running the dashboard locally

Build the integrated dashboard, then start the API:

```bash
cd src/SecureFix.Web
npm ci
VITE_DATA_MODE=live VITE_AUTH_MODE=demo npm run build
cd ../..
dotnet run --project src/SecureFix.Api --urls http://127.0.0.1:5000
```

Open `http://127.0.0.1:5000`. The development role switcher uses the existing demo bearer token and headers; it must never be enabled in Production. For frontend hot reload, run `npm run dev` and let Vite proxy API calls to the configured ASP.NET development URL.

To run the authenticated API validation script in a second terminal:

```bash
bash ./demo-end-to-end.sh
```

The script produces timestamped JSON evidence in `demo-results-*` and verifies that
remediation is blocked until a `SecurityReviewer` approves the workflow.

Run the security-control demo to validate injection containment, unauthorized approval
rejection, AI fallback, and the kill switch. It starts isolated local API instances and
writes evidence to `demo-security-results-*`.

```bash
bash ./demo-security-controls.sh
```

## Dashboard capabilities

- operational overview with workflow and severity distributions
- searchable, filterable vulnerability work queue
- workflow detail with risk, approval, remediation, proposal, audit, and governance views
- validated alert ingestion and sample payloads
- role-aware human approval and rejection actions
- demo identity switching for local development and an MSAL/Entra production adapter

## Status

The repository provides a reproducible, production-minded demo that combines a governed security workflow with an operator dashboard while preserving security, traceability, and human oversight.
