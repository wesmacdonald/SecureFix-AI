export type Role = "Admin" | "SecurityReviewer" | "Developer" | "Viewer";
export type Severity = "Critical" | "High" | "Medium" | "Low";
export type WorkflowStatus =
  | "Received"
  | "Validated"
  | "Assessed"
  | "Recommended"
  | "PendingApproval"
  | "Approved"
  | "Rejected"
  | "Failed";
export type ApprovalStatus = "Pending" | "Approved" | "Rejected";

export interface Identity {
  name: string;
  email: string;
  role: Role;
  authMode: "demo" | "entra";
}

export interface AlertIngestionRequest {
  externalAlertId: string;
  cveId?: string;
  packageName: string;
  installedVersion: string;
  fixedVersion?: string;
  providerSeverity: string;
  description?: string;
  isDirectDependency?: boolean;
  isExploitable?: boolean;
  repositoryIdentifier?: string;
  advisoryUrl?: string;
}

export interface RiskAssessment {
  id?: string;
  alertId?: string;
  riskScore: number;
  normalizedSeverity: Severity | number;
  confidenceScore?: number;
  requiredApprovalLevel: string;
  riskFactors: string[];
}

export interface AlertIngestionResponse {
  workflowId: string;
  correlationId: string;
  isAccepted: boolean;
  assessment: RiskAssessment;
  nextStep: string;
  message?: string;
  receivedAt: string;
}

export interface Workflow {
  workflowId: string;
  correlationId: string;
  status: WorkflowStatus | number;
  alert?: {
    id: string;
    packageName: string;
    installedVersion: string;
    fixedVersion?: string;
    providerSeverity: string;
    cveId?: string;
    description?: string;
  };
  riskAssessment?: RiskAssessment;
  approval?: {
    status: ApprovalStatus | number;
    reviewer?: string;
    reason?: string;
    decidedAt?: string;
  };
  remediation?: {
    recommendedAction: string;
    targetVersion?: string;
    confidenceScore: number;
    modelIdentifier: string;
  };
  nextStep?: string;
  createdAt: string;
  updatedAt: string;
  repository?: string;
}

export interface WorkflowListItem {
  workflowId: string;
  correlationId: string;
  externalAlertId: string;
  cveId?: string;
  packageName: string;
  installedVersion: string;
  fixedVersion?: string;
  repositoryIdentifier?: string;
  severity?: Severity | number;
  riskScore?: number;
  status: WorkflowStatus | number;
  recommendedAction?: string;
  reviewer?: string;
  receivedAt: string;
  updatedAt: string;
}

export interface WorkflowListResponse {
  items: WorkflowListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface DashboardSummary {
  totalWorkflows: number;
  pendingApprovalWorkflows: number;
  approvedWorkflows: number;
  rejectedWorkflows: number;
  criticalWorkflows: number;
  highWorkflows: number;
  mediumWorkflows: number;
  lowWorkflows: number;
  workflowsWithRecommendations: number;
  averageRiskScore: number;
  generatedAt: string;
}

export interface RemediationRecommendation {
  id: string;
  alertId: string;
  riskAssessmentId: string;
  correlationId: string;
  recommendedAction: string;
  targetVersion?: string;
  explanation: string;
  assumptions?: string;
  confidenceScore: number;
  modelIdentifier: string;
  promptVersion?: string;
  requiresHumanReview: boolean;
  potentialRisks?: string;
  alternativeActions: string[];
  generatedAt: string;
}

export interface PullRequestProposal {
  id: string;
  recommendationId: string;
  alertId: string;
  correlationId: string;
  proposedTitle: string;
  proposedDescription: string;
  filesForReview: string[];
  dependencyChanges: string[];
  validationCommands: string[];
  rollbackGuidance?: string;
  knownLimitations?: string;
  resourceLinks: string[];
  estimatedEffort?: string;
  isReadyForReview: boolean;
  rawProposalJson?: string;
  generatedAt: string;
}

export interface AuditEvent {
  id: string;
  correlationId: string;
  workflowId?: string;
  eventType: string;
  level: string;
  summary: string;
  details?: string;
  actor?: string;
  service?: string;
  alertId?: string;
  metadata?: string;
  timestamp: string;
  isSecurityRelevant: boolean;
}

export interface GovernanceReport {
  alertId: string;
  correlationId: string;
  severity: number;
  requiredApprovalLevel: string;
  approvalStatus: string;
  reviewer?: string;
  approvalTimestamp?: string;
  recommendedAction?: string;
  recommendationConfidence?: number;
  modelName?: string;
  promptVersion?: string;
  generatedAt: string;
}

export interface ApiErrorShape {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
  message?: string;
}
