import type {
  AuditEvent,
  GovernanceReport,
  PullRequestProposal,
  RemediationRecommendation,
  Severity,
  Workflow,
  WorkflowStatus,
} from "../api/types";

const hoursAgo = (hours: number) => new Date(Date.now() - hours * 3_600_000).toISOString();

const seed = [
  ["wf-8f21", "lodash", "4.17.20", "4.17.21", "CVE-2021-23337", "Critical", 96, "PendingApproval", "commerce/api", 2],
  ["wf-7ca9", "Microsoft.Identity.Web", "2.16.0", "3.2.0", "CVE-2024-21319", "High", 82, "Approved", "identity/service", 7],
  ["wf-1dd4", "requests", "2.31.0", "2.32.3", "CVE-2024-35195", "High", 78, "Recommended", "automation/worker", 13],
  ["wf-94b3", "vite", "5.4.8", "5.4.14", "CVE-2025-30208", "Medium", 58, "Rejected", "portal/web", 22],
  ["wf-35af", "System.Text.Json", "8.0.3", "8.0.5", "CVE-2024-43485", "Critical", 92, "Approved", "payments/api", 31],
  ["wf-a131", "axios", "1.6.7", "1.7.4", "CVE-2024-39338", "Medium", 54, "Assessed", "mobile/gateway", 45],
  ["wf-b290", "cryptography", "41.0.3", "43.0.1", "CVE-2024-6119", "High", 86, "Failed", "ml/inference", 62],
  ["wf-c422", "minimist", "1.2.5", "1.2.8", "CVE-2021-44906", "Low", 27, "Received", "tooling/cli", 80],
] as const;

export const demoWorkflows: Workflow[] = seed.map(
  ([id, packageName, installedVersion, fixedVersion, cveId, severity, score, status, repository, age]) => ({
    workflowId: id,
    correlationId: `corr-${id.slice(3)}-2026`,
    status: status as WorkflowStatus,
    repository,
    alert: {
      id,
      packageName,
      installedVersion,
      fixedVersion,
      providerSeverity: severity.toLowerCase(),
      cveId,
      description: `${packageName} contains a known vulnerability that may affect confidentiality or service integrity. External advisory text is treated as untrusted input.`,
    },
    riskAssessment: {
      riskScore: score,
      normalizedSeverity: severity as Severity,
      confidenceScore: 0.91,
      requiredApprovalLevel: severity === "Critical" ? "Admin or SecurityReviewer" : "SecurityReviewer",
      riskFactors: [
        severity === "Critical" ? "Critical provider severity" : "Published security advisory",
        "Direct production dependency",
        score > 80 ? "High exploitability signal" : "Fixed version available",
      ],
    },
    approval: {
      status: status === "Approved" ? "Approved" : status === "Rejected" ? "Rejected" : "Pending",
      reviewer: status === "Approved" ? "alex.chen@contoso.com" : status === "Rejected" ? "priya.shah@contoso.com" : undefined,
      reason: status === "Approved" ? "Validated upgrade path and test coverage." : status === "Rejected" ? "Upgrade requires a breaking framework migration." : undefined,
      decidedAt: status === "Approved" || status === "Rejected" ? hoursAgo(age - 1) : undefined,
    },
    remediation:
      ["Recommended", "Approved"].includes(status)
        ? {
            recommendedAction: "upgrade_dependency",
            targetVersion: fixedVersion,
            confidenceScore: 0.92,
            modelIdentifier: "azure-openai/gpt-4.1",
          }
        : undefined,
    nextStep:
      status === "PendingApproval"
        ? "Human approval required"
        : status === "Approved"
          ? "Generate or review draft proposal"
          : status === "Rejected"
            ? "No action permitted"
            : status === "Failed"
              ? "Human review required"
              : "Continue security workflow",
    createdAt: hoursAgo(age),
    updatedAt: hoursAgo(Math.max(age - 1, 0)),
  }),
);

export const createDemoAudit = (workflow: Workflow): AuditEvent[] => {
  const approvalStatus = typeof workflow.approval?.status === "string"
    ? workflow.approval.status
    : workflow.approval?.status === 2
      ? "Approved"
      : workflow.approval?.status === 3
        ? "Rejected"
        : "Pending";
  const events: AuditEvent[] = [
    {
      id: `${workflow.workflowId}-evt-1`,
      correlationId: workflow.correlationId,
      workflowId: workflow.workflowId,
      alertId: workflow.workflowId,
      eventType: "AlertReceived",
      level: "Info",
      summary: "Vulnerability alert received and correlation ID assigned.",
      actor: "dependabot",
      service: "AlertIngestion",
      timestamp: workflow.createdAt,
      isSecurityRelevant: true,
    },
    {
      id: `${workflow.workflowId}-evt-2`,
      correlationId: workflow.correlationId,
      workflowId: workflow.workflowId,
      alertId: workflow.workflowId,
      eventType: "RiskAssessed",
      level: "Info",
      summary: `Risk engine assigned score ${workflow.riskAssessment?.riskScore ?? 0}.`,
      actor: "securefix-risk-engine",
      service: "RiskEngine",
      timestamp: new Date(new Date(workflow.createdAt).getTime() + 42_000).toISOString(),
      isSecurityRelevant: true,
    },
  ];
  if (workflow.remediation) {
    events.push({
      id: `${workflow.workflowId}-evt-3`,
      correlationId: workflow.correlationId,
      workflowId: workflow.workflowId,
      alertId: workflow.workflowId,
      eventType: "RecommendationGenerated",
      level: "Info",
      summary: "Advisory remediation recommendation generated for human review.",
      actor: workflow.remediation.modelIdentifier,
      service: "AIProvider",
      timestamp: hoursAgo(3),
      isSecurityRelevant: true,
    });
  }
  if (approvalStatus !== "Pending") {
    events.push({
      id: `${workflow.workflowId}-evt-4`,
      correlationId: workflow.correlationId,
      workflowId: workflow.workflowId,
      alertId: workflow.workflowId,
      eventType: approvalStatus === "Approved" ? "ApprovalGranted" : "ApprovalDenied",
      level: approvalStatus === "Approved" ? "Info" : "Warning",
      summary: `Workflow ${approvalStatus.toLowerCase()} by an authorized human reviewer.`,
      details: workflow.approval?.reason,
      actor: workflow.approval?.reviewer,
      service: "ApprovalGate",
      timestamp: workflow.approval?.decidedAt ?? workflow.updatedAt,
      isSecurityRelevant: true,
    });
  }
  return events;
};

export const createDemoRemediation = (workflow: Workflow): RemediationRecommendation => ({
  id: `rec-${workflow.workflowId}`,
  alertId: workflow.workflowId,
  riskAssessmentId: `risk-${workflow.workflowId}`,
  correlationId: workflow.correlationId,
  recommendedAction: "upgrade_dependency",
  targetVersion: workflow.alert?.fixedVersion,
  explanation: `Upgrade ${workflow.alert?.packageName} from ${workflow.alert?.installedVersion} to the scanner-provided fixed version ${workflow.alert?.fixedVersion}. Keep the change isolated, run the existing test suite, and verify authentication and authorization paths before release.`,
  assumptions: "The fixed version originates from the trusted advisory metadata and is compatible with the current package manifest.",
  confidenceScore: 0.92,
  modelIdentifier: "azure-openai/gpt-4.1",
  promptVersion: "remediation-v1.3",
  requiresHumanReview: true,
  potentialRisks: "The dependency update may introduce behavioral changes. Automated and reviewer validation remain mandatory.",
  alternativeActions: ["Apply a vendor-supported backport", "Temporarily isolate the affected component", "Accept risk with an expiration date"],
  generatedAt: new Date().toISOString(),
});

export const createDemoProposal = (workflow: Workflow): PullRequestProposal => ({
  id: `proposal-${workflow.workflowId}`,
  recommendationId: `rec-${workflow.workflowId}`,
  alertId: workflow.workflowId,
  correlationId: workflow.correlationId,
  proposedTitle: `security: upgrade ${workflow.alert?.packageName} to ${workflow.alert?.fixedVersion}`,
  proposedDescription: `## Security remediation\n\nUpgrades **${workflow.alert?.packageName}** from \`${workflow.alert?.installedVersion}\` to \`${workflow.alert?.fixedVersion}\` to address ${workflow.alert?.cveId}.\n\nThis artifact is a draft proposal only. It does not create, approve, merge, deploy, or release a pull request.`,
  filesForReview: ["dependency manifest", "dependency lockfile"],
  dependencyChanges: [`${workflow.alert?.packageName}: ${workflow.alert?.installedVersion} -> ${workflow.alert?.fixedVersion}`],
  validationCommands: ["run unit tests", "run dependency scan", "run build verification"],
  rollbackGuidance: `Revert the dependency manifest and lockfile to ${workflow.alert?.installedVersion}, then redeploy the last known-good artifact.`,
  knownLimitations: "Compatibility is inferred from available metadata and must be validated by a human reviewer.",
  resourceLinks: [],
  estimatedEffort: "minimal",
  isReadyForReview: true,
  generatedAt: new Date().toISOString(),
});

export const createDemoGovernance = (workflow: Workflow): GovernanceReport => ({
  alertId: workflow.workflowId,
  correlationId: workflow.correlationId,
  severity: ["Low", "Medium", "High", "Critical"].indexOf(String(workflow.riskAssessment?.normalizedSeverity)) + 1,
  requiredApprovalLevel: workflow.riskAssessment?.requiredApprovalLevel ?? "SecurityReviewer",
  approvalStatus: String(workflow.approval?.status ?? "Pending"),
  reviewer: workflow.approval?.reviewer,
  approvalTimestamp: workflow.approval?.decidedAt,
  recommendedAction: workflow.remediation?.recommendedAction,
  recommendationConfidence: workflow.remediation?.confidenceScore,
  modelName: workflow.remediation?.modelIdentifier,
  promptVersion: workflow.remediation ? "remediation-v1.3" : undefined,
  generatedAt: new Date().toISOString(),
});
