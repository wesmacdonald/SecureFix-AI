#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://127.0.0.1:8888}"
DEMO_TOKEN="${SECUREFIX_DEMO_TOKEN:-securefix-demo-token}"
RESULTS_DIR="${RESULTS_DIR:-./demo-results-$(date +%s)}"
mkdir -p "$RESULTS_DIR"

# Set by api(); holds the HTTP status code of the most recent call.
HTTP_STATUS=0

api() {
    local method=$1
    local endpoint=$2
    local role=$3
    local output=$4
    local data=${5:-}
    local curl_args=(
        --silent
        --show-error
        --write-out '%{http_code}'
        --output "$RESULTS_DIR/$output"
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
    HTTP_STATUS=$(curl "${curl_args[@]}" "$BASE_URL$endpoint") || curl_status=$?

    if [[ $curl_status -ne 0 ]]; then
        printf 'curl transport failure (exit %s) for %s %s\n' "$curl_status" "$method" "$endpoint" >&2
        return 1
    fi

    cat "$RESULTS_DIR/$output"
    printf '\nHTTP %s\n' "$HTTP_STATUS"

    [[ $HTTP_STATUS -lt 400 ]]
}

# Asserts the endpoint returns a specific HTTP status (e.g. 403 for a blocked action).
api_expect() {
    local expected=$1
    shift
    api "$@" || true

    if [[ "$HTTP_STATUS" != "$expected" ]]; then
        printf 'Expected HTTP %s but got HTTP %s\n' "$expected" "$HTTP_STATUS" >&2
        exit 1
    fi
    printf 'Confirmed expected HTTP %s\n' "$expected"
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
api_expect 403 POST "/api/v1/workflows/$WORKFLOW_ID/remediate" Developer 04-remediation-blocked.json

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
