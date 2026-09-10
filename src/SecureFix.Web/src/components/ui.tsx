import type { ReactNode } from "react";
import type { ApprovalStatus, Severity, WorkflowStatus } from "../api/types";

export const severityName = (value: Severity | number | undefined): Severity => {
  if (typeof value === "string") return value;
  return (["Low", "Low", "Medium", "High", "Critical"][value ?? 1] ?? "Low") as Severity;
};

export const statusName = (value: WorkflowStatus | number): WorkflowStatus => {
  if (typeof value === "string") return value;
  return (["Received", "Received", "Validated", "Assessed", "Recommended", "PendingApproval", "Approved", "Rejected", "Failed"][value] ?? "Received") as WorkflowStatus;
};

export const approvalName = (value: ApprovalStatus | number | undefined): ApprovalStatus => {
  if (typeof value === "string") return value;
  return (["Pending", "Pending", "Approved", "Rejected"][value ?? 1] ?? "Pending") as ApprovalStatus;
};

export function Badge({ children, tone = "neutral" }: { children: ReactNode; tone?: "critical" | "high" | "medium" | "low" | "success" | "danger" | "warning" | "neutral" }) {
  return <span className={`badge badge--${tone}`}>{children}</span>;
}

export const severityTone = (severity: Severity) =>
  severity === "Critical" ? "critical" : severity === "High" ? "high" : severity === "Medium" ? "medium" : "low";

export function EmptyState({ title, detail, action }: { title: string; detail: string; action?: ReactNode }) {
  return (
    <div className="empty-state">
      <div className="empty-state__mark" aria-hidden="true">✓</div>
      <h3>{title}</h3>
      <p>{detail}</p>
      {action}
    </div>
  );
}

export function LoadingState({ label = "Loading secure workflow data" }: { label?: string }) {
  return (
    <div className="loading-state" role="status">
      <span className="spinner" aria-hidden="true" />
      <span>{label}…</span>
    </div>
  );
}

export function ErrorBanner({ message }: { message: string }) {
  return <div className="error-banner" role="alert"><strong>Request failed.</strong> {message}</div>;
}

export function ProgressBar({ value, label }: { value: number; label: string }) {
  return (
    <div className="progress" aria-label={label}>
      <span className="sr-only">{label}: {value}%</span>
      <span className="progress__fill" style={{ width: `${Math.max(0, Math.min(value, 100))}%` }} />
    </div>
  );
}
