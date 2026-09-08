# Security Model

SecureFix AI treats all external vulnerability content as untrusted data.

## Controls

- validate all payloads
- enforce allowed severity values and CVE formats
- isolate system prompts from untrusted text
- validate AI output against a strict schema
- apply role-based authorization
- mask secrets and sensitive headers in logs
- block AI from approving, merging, deploying, or changing policy
- provide a kill switch for action adapters

## Identity and Authentication

The API supports two authentication modes, selected via `AUTH_MODE`:

- `entra` (required in Production): validates Microsoft Entra ID (Azure AD) issued JWTs
  using Microsoft.Identity.Web. Tokens must have the correct issuer, audience, signature,
  and expiration. Roles are asserted by Entra ID in the token's `roles` claim, which
  Microsoft.Identity.Web maps to `ClaimTypes.Role` for use by `[Authorize(Roles = ...)]`.
- `demo` (local/dev only): a static bearer token plus a self-asserted `X-User-Role` header.
  This mode is intentionally weak — it exists only for offline demos — and the API fails
  to start if `ASPNETCORE_ENVIRONMENT=Production` and `AUTH_MODE` is not `entra` (fail closed).

App roles are defined once on the Entra ID app registration (see
`infra/entra-app-registration.sh`) and assigned to users/groups from the Enterprise
Application's "Users and groups" blade — never granted by the application itself.

## Roles

- Admin
- SecurityReviewer
- Developer
- Viewer

## Secrets

Secrets must come from environment variables or managed secret storage in production.
No credentials are committed to source control.

