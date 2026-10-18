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

The integrated dashboard uses an MSAL browser adapter when built with `VITE_AUTH_MODE=entra`.
Only public identifiers and API scopes are compiled into browser assets; client secrets are
never used by or exposed to the dashboard. In demo mode, the visible role switcher is a local
UX aid backed by the intentionally weak demo headers. Hiding or disabling a UI action is not
a security boundary: every operation remains protected by API role authorization.

## Roles

- Admin
- SecurityReviewer
- Developer
- Viewer

## Browser controls

The API serves the dashboard with a restrictive Content Security Policy, clickjacking
protection, MIME-sniffing protection, and a strict referrer policy. Static dashboard routes
are anonymous so the browser can load the application shell; protected data and mutations
still require authenticated API calls.

## Secrets

Secrets must come from environment variables or managed secret storage in production.
No credentials are committed to source control. `VITE_*` values are public build-time
configuration and must never contain secrets.

