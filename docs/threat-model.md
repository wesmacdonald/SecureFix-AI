# Threat Model

## Key threats

- prompt injection through advisory text
- vulnerable dependencies
- unauthorized approval
- privilege escalation
- secrets exposure
- data leakage
- denial of service
- supply chain attacks

## Mitigations

- input validation
- output schema validation
- role-based authorization backed by Microsoft Entra ID app roles (`Admin`,
  `SecurityReviewer`, `Developer`, `Viewer`), assigned to users/groups on the Entra
  Enterprise Application, never granted by the API itself
- fail-closed authentication: the API refuses to start in Production unless
  `AUTH_MODE=entra`, so the weak demo bearer-token handler can never protect a
  production deployment
- least-privilege action adapters
- secret masking
- kill switch
- CI security checks
- append-only audit records
