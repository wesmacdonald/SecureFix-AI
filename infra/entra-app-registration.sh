#!/usr/bin/env bash
# Creates (or updates) the Microsoft Entra ID app registration for the SecureFix API,
# including the four application roles enforced by [Authorize(Roles = "...")] in the API.
#
# Requires: az cli, logged in with permission to create app registrations
# (Application Administrator or Cloud Application Administrator, or an owner of an
# existing app when re-running this script).
#
# Usage:
#   ./infra/entra-app-registration.sh "SecureFix-AI-API"
#
# Outputs the values needed for AzureAd__TenantId, AzureAd__ClientId and AzureAd__Audience.

set -euo pipefail

APP_NAME="${1:-SecureFix-AI-API}"

ROLES_JSON=$(cat <<'JSON'
[
  {
    "allowedMemberTypes": ["User"],
    "description": "Full administrative access, including operational metrics and kill switch control.",
    "displayName": "Admin",
    "id": "8f3a1e10-2b1a-4a4e-9b1e-000000000001",
    "isEnabled": true,
    "value": "Admin"
  },
  {
    "allowedMemberTypes": ["User"],
    "description": "Reviews and approves or rejects remediation proposals; views governance reports.",
    "displayName": "Security Reviewer",
    "id": "8f3a1e10-2b1a-4a4e-9b1e-000000000002",
    "isEnabled": true,
    "value": "SecurityReviewer"
  },
  {
    "allowedMemberTypes": ["User"],
    "description": "Views alerts and triggers ingestion/workflow actions.",
    "displayName": "Developer",
    "id": "8f3a1e10-2b1a-4a4e-9b1e-000000000003",
    "isEnabled": true,
    "value": "Developer"
  },
  {
    "allowedMemberTypes": ["User"],
    "description": "Read-only access to alerts, recommendations and reports.",
    "displayName": "Viewer",
    "id": "8f3a1e10-2b1a-4a4e-9b1e-000000000004",
    "isEnabled": true,
    "value": "Viewer"
  }
]
JSON
)

EXISTING_APP_ID=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv)

if [[ -z "$EXISTING_APP_ID" ]]; then
  echo "Creating app registration '$APP_NAME'..."
  APP_ID=$(az ad app create --display-name "$APP_NAME" --sign-in-audience AzureADMyOrg --query appId -o tsv)
else
  echo "App registration '$APP_NAME' already exists (appId=$EXISTING_APP_ID). Updating roles..."
  APP_ID="$EXISTING_APP_ID"
fi

# Set the App ID URI (audience) if not already configured.
az ad app update --id "$APP_ID" --identifier-uris "api://$APP_ID"

# Replace app roles with the definition above (idempotent).
az ad app update --id "$APP_ID" --app-roles "$ROLES_JSON"

# Ensure a service principal (Enterprise Application) exists so users/groups can be assigned to roles.
az ad sp create --id "$APP_ID" >/dev/null 2>&1 || true

TENANT_ID=$(az account show --query tenantId -o tsv)

echo ""
echo "App registration ready."
echo "  AzureAd__TenantId = $TENANT_ID"
echo "  AzureAd__ClientId = $APP_ID"
echo "  AzureAd__Audience = api://$APP_ID"
echo ""
echo "Next step: assign users or groups to the Admin, SecurityReviewer, Developer and Viewer"
echo "roles from the Enterprise Application's 'Users and groups' blade in the Entra portal,"
echo "or via 'az ad app permission' / Microsoft Graph for automated onboarding."
