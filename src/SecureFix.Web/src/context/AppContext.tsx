import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { SecureFixApiClient } from "../api/client";
import type {
  AlertIngestionRequest,
  AuditEvent,
  GovernanceReport,
  Identity,
  PullRequestProposal,
  RemediationRecommendation,
  Role,
  Severity,
  Workflow,
  WorkflowStatus,
} from "../api/types";
import { createAuthAdapter } from "../auth/auth";
import {
  createDemoAudit,
  createDemoGovernance,
  createDemoProposal,
  createDemoRemediation,
  demoWorkflows,
} from "../data/demo";

interface AppContextValue {
  identity: Identity;
  setRole: (role: Role) => void;
  workflows: Workflow[];
  dataLoading: boolean;
  dataError: string;
  getWorkflow: (id: string) => Workflow | undefined;
  ingest: (payload: AlertIngestionRequest) => Promise<Workflow>;
  decide: (id: string, decision: "approved" | "rejected", reason: string) => Promise<Workflow>;
  generateRemediation: (id: string) => Promise<RemediationRecommendation>;
  generateProposal: (id: string) => Promise<PullRequestProposal>;
  getAudit: (id: string) => Promise<AuditEvent[]>;
  getGovernance: (id: string) => Promise<GovernanceReport>;
  remediations: Record<string, RemediationRecommendation>;
  proposals: Record<string, PullRequestProposal>;
  isDemo: boolean;
}

const AppContext = createContext<AppContextValue | null>(null);

const statusName = (status: Workflow["status"]): WorkflowStatus => {
  if (typeof status === "string") return status;
  return (["Received", "Received", "Validated", "Assessed", "Recommended", "PendingApproval", "Approved", "Rejected", "Failed"][status] ?? "Received") as WorkflowStatus;
};

export function AppProvider({ children }: { children: ReactNode }) {
  const isDemo = import.meta.env.VITE_DATA_MODE === "demo";
  const [identity, setIdentity] = useState<Identity>({
    name: "Morgan Lee",
    email: "morgan.lee@contoso.com",
    role: "SecurityReviewer",
    authMode: import.meta.env.VITE_AUTH_MODE === "entra" ? "entra" : "demo",
  });
  const [workflows, setWorkflows] = useState<Workflow[]>(isDemo ? demoWorkflows : []);
  const [remediations, setRemediations] = useState<Record<string, RemediationRecommendation>>({});
  const [proposals, setProposals] = useState<Record<string, PullRequestProposal>>({});
  const [dataLoading, setDataLoading] = useState(!isDemo);
  const [dataError, setDataError] = useState("");
  const auth = useMemo(() => createAuthAdapter(identity), []);
  const [authReady, setAuthReady] = useState(identity.authMode === "demo");
  const [authError, setAuthError] = useState("");
  const api = useMemo(() => new SecureFixApiClient(auth, () => identity), [auth, identity]);

  useEffect(() => {
    if (identity.authMode !== "entra") return;
    void auth.initialize()
      .then((authenticatedIdentity) => {
        setIdentity(authenticatedIdentity);
        setAuthReady(true);
      })
      .catch((caught) => setAuthError(caught instanceof Error ? caught.message : "Microsoft Entra sign-in failed."));
  }, [auth, identity.authMode]);

  useEffect(() => {
    if (isDemo || !authReady) return;
    setDataLoading(true);
    void api.getWorkflows()
      .then((response) => setWorkflows(response.items.map((item) => ({
        workflowId: item.workflowId,
        correlationId: item.correlationId,
        status: item.status,
        repository: item.repositoryIdentifier,
        alert: {
          id: item.externalAlertId,
          packageName: item.packageName,
          installedVersion: item.installedVersion,
          fixedVersion: item.fixedVersion,
          providerSeverity: String(item.severity ?? ""),
          cveId: item.cveId,
        },
        riskAssessment: item.riskScore == null || item.severity == null ? undefined : {
          riskScore: item.riskScore,
          normalizedSeverity: item.severity,
          requiredApprovalLevel: "SecurityReviewer",
          riskFactors: [],
        },
        approval: {
          status: statusName(item.status) === "Approved" ? "Approved" : statusName(item.status) === "Rejected" ? "Rejected" : "Pending",
          reviewer: item.reviewer,
        },
        remediation: item.recommendedAction ? {
          recommendedAction: item.recommendedAction,
          confidenceScore: 0,
          modelIdentifier: "See workflow detail",
        } : undefined,
        createdAt: item.receivedAt,
        updatedAt: item.updatedAt,
      }))))
      .catch((caught) => setDataError(caught instanceof Error ? caught.message : "Could not load workflows."))
      .finally(() => setDataLoading(false));
  }, [api, authReady, isDemo]);

  const updateWorkflow = (workflow: Workflow) => {
    const normalized = { ...workflow, status: statusName(workflow.status) };
    setWorkflows((items) => [normalized, ...items.filter((item) => item.workflowId !== normalized.workflowId)]);
    return normalized;
  };

  const ingest = async (payload: AlertIngestionRequest) => {
    if (!isDemo) {
      const response = await api.ingestAlert(payload);
      return updateWorkflow(await api.getWorkflow(response.workflowId));
    }
    const now = new Date().toISOString();
    const severity = payload.providerSeverity.charAt(0).toUpperCase() + payload.providerSeverity.slice(1).toLowerCase();
    const workflow: Workflow = {
      workflowId: `wf-${crypto.randomUUID().slice(0, 6)}`,
      correlationId: crypto.randomUUID(),
      status: "PendingApproval",
      repository: payload.repositoryIdentifier,
      alert: {
        id: payload.externalAlertId,
        packageName: payload.packageName,
        installedVersion: payload.installedVersion,
        fixedVersion: payload.fixedVersion,
        providerSeverity: payload.providerSeverity,
        cveId: payload.cveId,
        description: payload.description,
      },
      riskAssessment: {
        riskScore: severity === "Critical" ? 94 : severity === "High" ? 81 : severity === "Medium" ? 56 : 28,
        normalizedSeverity: severity as Severity,
        confidenceScore: 0.9,
        requiredApprovalLevel: severity === "Critical" ? "Admin or SecurityReviewer" : "SecurityReviewer",
        riskFactors: [payload.isExploitable ? "Exploitability reported" : "Published advisory", payload.isDirectDependency ? "Direct dependency" : "Transitive dependency", "Fixed version available"],
      },
      approval: { status: "Pending" },
      nextStep: "Human approval required",
      createdAt: now,
      updatedAt: now,
    };
    return updateWorkflow(workflow);
  };

  const decide = async (id: string, decision: "approved" | "rejected", reason: string) => {
    if (!isDemo) return updateWorkflow(await api.decideWorkflow(id, decision, reason));
    const workflow = workflows.find((item) => item.workflowId === id);
    if (!workflow) throw new Error("Workflow not found.");
    return updateWorkflow({
      ...workflow,
      status: decision === "approved" ? "Approved" : "Rejected",
      approval: {
        status: decision === "approved" ? "Approved" : "Rejected",
        reviewer: identity.email,
        reason,
        decidedAt: new Date().toISOString(),
      },
      nextStep: decision === "approved" ? "Generate remediation recommendation" : "No action permitted",
      updatedAt: new Date().toISOString(),
    });
  };

  const generateRemediation = async (id: string) => {
    const workflow = workflows.find((item) => item.workflowId === id);
    if (!workflow) throw new Error("Workflow not found.");
    const remediation = isDemo ? createDemoRemediation(workflow) : await api.generateRemediation(id);
    setRemediations((items) => ({ ...items, [id]: remediation }));
    updateWorkflow({
      ...workflow,
      remediation: {
        recommendedAction: remediation.recommendedAction,
        targetVersion: remediation.targetVersion,
        confidenceScore: remediation.confidenceScore,
        modelIdentifier: remediation.modelIdentifier,
      },
      nextStep: "Generate draft pull request proposal",
      updatedAt: new Date().toISOString(),
    });
    return remediation;
  };

  const generateProposal = async (id: string) => {
    const workflow = workflows.find((item) => item.workflowId === id);
    if (!workflow) throw new Error("Workflow not found.");
    const proposal = isDemo ? createDemoProposal(workflow) : await api.generateProposal(id);
    setProposals((items) => ({ ...items, [id]: proposal }));
    return proposal;
  };

  if (authError) {
    return <div className="startup-state" role="alert"><strong>Authentication failed</strong><span>{authError}</span></div>;
  }

  if (!authReady) {
    return <div className="startup-state" role="status"><span className="spinner" aria-hidden="true" /><strong>Signing in with Microsoft Entra ID…</strong></div>;
  }

  return (
    <AppContext.Provider
      value={{
        identity,
        setRole: (role) => setIdentity((current) => ({ ...current, role })),
        workflows,
        dataLoading,
        dataError,
        getWorkflow: (id) => workflows.find((item) => item.workflowId === id),
        ingest,
        decide,
        generateRemediation,
        generateProposal,
        getAudit: async (id) => {
          const workflow = workflows.find((item) => item.workflowId === id);
          if (!workflow) throw new Error("Workflow not found.");
          return isDemo ? createDemoAudit(workflow) : api.getAuditEvents(id);
        },
        getGovernance: async (id) => {
          const workflow = workflows.find((item) => item.workflowId === id);
          if (!workflow) throw new Error("Workflow not found.");
          return isDemo ? createDemoGovernance(workflow) : api.getGovernanceReport(id);
        },
        remediations,
        proposals,
        isDemo,
      }}
    >
      {children}
    </AppContext.Provider>
  );
}

export const useApp = () => {
  const value = useContext(AppContext);
  if (!value) throw new Error("useApp must be used inside AppProvider.");
  return value;
};
