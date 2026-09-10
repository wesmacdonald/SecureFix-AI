import type {
  AlertIngestionRequest,
  AlertIngestionResponse,
  ApiErrorShape,
  AuditEvent,
  DashboardSummary,
  GovernanceReport,
  Identity,
  PullRequestProposal,
  RemediationRecommendation,
  Workflow,
  WorkflowListResponse,
} from "./types";
import type { AuthAdapter } from "../auth/auth";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly details?: ApiErrorShape,
  ) {
    super(message);
  }
}

export class SecureFixApiClient {
  constructor(
    private readonly auth: AuthAdapter,
    private readonly getIdentity: () => Identity,
    private readonly baseUrl = import.meta.env.VITE_API_BASE_URL || "",
  ) {}

  getWorkflow(id: string) {
    return this.request<Workflow>(`/api/v1/workflows/${encodeURIComponent(id)}`);
  }

  getWorkflows(page = 1, pageSize = 100) {
    return this.request<WorkflowListResponse>(`/api/v1/workflows?page=${page}&pageSize=${pageSize}`);
  }

  getDashboardSummary() {
    return this.request<DashboardSummary>("/api/v1/dashboard/summary");
  }

  ingestAlert(payload: AlertIngestionRequest) {
    return this.request<AlertIngestionResponse>("/api/v1/alerts", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  }

  decideWorkflow(id: string, decision: "approved" | "rejected", reason?: string) {
    const identity = this.getIdentity();
    return this.request<Workflow>(`/api/v1/workflows/${encodeURIComponent(id)}/${decision === "approved" ? "approve" : "reject"}`, {
      method: "POST",
      body: JSON.stringify({
        reviewer: identity.email,
        reviewerRole: identity.role,
        decision,
        reason,
      }),
    });
  }

  generateRemediation(id: string) {
    return this.request<RemediationRecommendation>(`/api/v1/workflows/${encodeURIComponent(id)}/remediate`, {
      method: "POST",
    });
  }

  getRemediation(id: string) {
    return this.request<RemediationRecommendation>(`/api/v1/workflows/${encodeURIComponent(id)}/remediate`);
  }

  generateProposal(id: string) {
    return this.request<PullRequestProposal>(`/api/v1/workflows/${encodeURIComponent(id)}/proposal`, {
      method: "POST",
    });
  }

  getProposal(id: string) {
    return this.request<PullRequestProposal>(`/api/v1/workflows/${encodeURIComponent(id)}/proposal`);
  }

  getAuditEvents(id: string) {
    return this.request<AuditEvent[]>(`/api/v1/workflows/${encodeURIComponent(id)}/audit-events`);
  }

  getGovernanceReport(id: string) {
    return this.request<GovernanceReport>(`/api/v1/workflows/${encodeURIComponent(id)}/governance-report`);
  }

  private async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const identity = this.getIdentity();
    const token = await this.auth.getAccessToken();
    const headers = new Headers(init.headers);
    headers.set("Accept", "application/json");
    headers.set("Content-Type", "application/json");
    headers.set("Authorization", `Bearer ${token}`);
    if (identity.authMode === "demo") {
      headers.set("X-User-Id", identity.email);
      headers.set("X-User-Role", identity.role);
    }
    const response = await fetch(`${this.baseUrl}${path}`, {
      ...init,
      headers,
    });
    if (!response.ok) {
      const details = (await response.json().catch(() => undefined)) as ApiErrorShape | undefined;
      throw new ApiError(details?.detail || details?.message || details?.title || `Request failed (${response.status})`, response.status, details);
    }
    return (await response.json()) as T;
  }
}
