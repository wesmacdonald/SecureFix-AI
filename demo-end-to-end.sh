#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://127.0.0.1:5000}"
DEMO_TOKEN="${SECUREFIX_DEMO_TOKEN:-securefix-demo-token}"
RESULTS_DIR="${RESULTS_DIR:-./demo-results-$(date +%s)}"
mkdir -p "$RESULTS_DIR"

api() {
    local method=$1
    local endpoint=$2
    local role=$3
    local output=$4
    local data=${5:-}
    local curl_args=(
        --fail-with-body
        --silent
        --show-error
        -X "$method"
        -H "Authorization: Bearer $DEMO_TOKEN"
        -H "X-User-Id: demo-reviewer@example.com"
        -H "X-User-Role: $role"
        -H "Content-Type: application/json"
    )

    if [[ -n "$data" ]]; then
        curl_args+=(--data "$data")
    fi

    local curl_status=0
    curl "${curl_args[@]}" "$BASE_URL$endpoint" | tee "$RESULTS_DIR/$output" || curl_status=$?
    printf '\n'
    return "$curl_status"
}

curl --fail-with-body --silent --show-error "$BASE_URL/health" | tee "$RESULTS_DIR/01-health.json"
printf '\n'

ALERT_ID="demo-lodash-$(date +%s)"
ALERT_PAYLOAD=$(cat <<EOF
{
  "externalAlertId": "$ALERT_ID",
  "cveId": "CVE-2021-23337",
  "packageName": "lodash",
  "installedVersion": "4.17.20",
  "fixedVersion": "4.17.21",
  "providerSeverity": "high",
  "description": "Synthetic Dependabot-style demo alert for lodash prototype pollution.",
  "isDirectDependency": true,
  "isExploitable": false,
  "repositoryIdentifier": "securefix-demo",
  "advisoryUrl": "https://github.com/advisories/GHSA-35jh-r3h4-6jhm"
}
EOF
)

api POST /api/v1/alerts Developer 02-alert-ingested.json "$ALERT_PAYLOAD"
WORKFLOW_ID=$(jq -er '.workflowId' "$RESULTS_DIR/02-alert-ingested.json")

api GET "/api/v1/workflows/$WORKFLOW_ID" Developer 03-pending-approval.json

# This must fail: recommendations require an approved workflow.
if api POST "/api/v1/workflows/$WORKFLOW_ID/remediate" Developer 04-remediation-blocked.json; then
    echo "Expected remediation to be blocked until approval." >&2
    exit 1
fi

APPROVAL_PAYLOAD='{
  "reviewer": "demo-reviewer@example.com",
  "reviewerRole": "SecurityReviewer",
  "decision": "approved",
  "reason": "Synthetic demo alert reviewed by a security reviewer."
}'
api POST "/api/v1/workflows/$WORKFLOW_ID/approve" SecurityReviewer 05-approved.json "$APPROVAL_PAYLOAD"
api POST "/api/v1/workflows/$WORKFLOW_ID/remediate" SecurityReviewer 06-remediation.json
RECOMMENDATION_ID=$(jq -er '.id' "$RESULTS_DIR/06-remediation.json")
api POST "/api/v1/workflows/$RECOMMENDATION_ID/proposal" SecurityReviewer 07-draft-proposal.json
api GET "/api/v1/workflows/$WORKFLOW_ID" SecurityReviewer 08-final-workflow.json
api GET "/api/v1/workflows/$WORKFLOW_ID/audit-events" SecurityReviewer 09-audit-events.json
api GET "/api/v1/workflows/$WORKFLOW_ID/governance-report" SecurityReviewer 10-governance-report.json
api GET /metrics Admin 11-metrics.json

echo "Demo completed successfully. Evidence is in $RESULTS_DIR"
