# Runbook

## Common operations

- start the service in Docker
- run unit and security tests
- submit a sample alert
- review the recommendation
- approve or reject the workflow
- inspect audit and governance output
- provision or update the Entra ID app registration and its roles

## Identity operations

- **Provision app registration and roles**: `./infra/entra-app-registration.sh "SecureFix-AI-API"`.
  Re-run to update the four app roles (`Admin`, `SecurityReviewer`, `Developer`, `Viewer`)
  idempotently.
- **Assign a user or group to a role**: Entra ID portal → Enterprise Applications →
  `SecureFix-AI-API` → Users and groups → Add assignment. Application-only assignment;
  the API never grants roles itself.
- **Rotate/verify configuration**: confirm `AUTH_MODE=entra` and `AzureAd__TenantId`,
  `AzureAd__ClientId`, `AzureAd__Audience` are set in the deployment environment. The API
  fails to start in `Production` if `AUTH_MODE` is not `entra` (fail closed).
- **Local/dev only**: `AUTH_MODE=demo` uses a static bearer token (`SECUREFIX_DEMO_TOKEN`)
  and a self-asserted `X-User-Role` header — never use this mode outside local development.

## Failure handling

- AI unavailable: use the fallback recommendation and require human review
- invalid AI output: reject the output and block action
- database unavailable: fail safely and do not claim persistence
- action failure: keep the approval record and allow explicit retry
- unexpected behavior: activate the kill switch

## Recovery

- roll back the container image
- revert policy configuration
- disable a bad model or prompt version
- reprocess a failed event if safe
- escalate to a human owner
