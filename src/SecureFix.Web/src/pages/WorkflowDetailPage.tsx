import { ArrowLeft, Bot, Check, ClipboardCheck, Download, FileCode2, LockKeyhole, ShieldCheck, X } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import type { AuditEvent, GovernanceReport } from "../api/types";
import { Badge, EmptyState, ErrorBanner, LoadingState, approvalName, severityName, severityTone, statusName } from "../components/ui";
import { useApp } from "../context/AppContext";

const steps = ["Received", "Assessed", "Recommended", "Human decision", "Draft proposal"];

export function WorkflowDetailPage() {
  const { id = "" } = useParams();
  const { getWorkflow, identity, decide, generateRemediation, generateProposal, remediations, proposals, getAudit, getGovernance } = useApp();
  const workflow = getWorkflow(id);
  const [tab, setTab] = useState<"overview" | "audit" | "governance">("overview");
  const [audit, setAudit] = useState<AuditEvent[]>([]);
  const [governance, setGovernance] = useState<GovernanceReport>();
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");
  const canReview = ["Admin", "SecurityReviewer"].includes(identity.role);
  const remediation = remediations[id];
  const proposal = proposals[id];

  useEffect(() => {
    if (!workflow || !canReview) return;
    setAudit([]);
    void getAudit(id).then(setAudit).catch((caught) => setError(caught instanceof Error ? caught.message : "Could not load audit events."));
  }, [canReview, getAudit, id, workflow?.updatedAt]);

  if (!workflow) return <div className="page"><EmptyState title="Workflow not found" detail="The workflow may have been removed or the identifier is invalid." action={<Link className="button button--secondary" to="/workflows">Return to workflows</Link>} /></div>;

  const severity = severityName(workflow.riskAssessment?.normalizedSeverity);
  const status = statusName(workflow.status);
  const approval = approvalName(workflow.approval?.status);
  const completedStep = proposal ? 5 : approval === "Approved" ? (workflow.remediation || remediation ? 4 : 3) : status === "PendingApproval" ? 3 : status === "Rejected" ? 4 : workflow.remediation ? 3 : 2;

  const run = async (name: string, action: () => Promise<unknown>) => {
    setError("");
    setBusy(name);
    try { await action(); } catch (caught) { setError(caught instanceof Error ? caught.message : "Action failed."); } finally { setBusy(""); }
  };

  const downloadGovernance = async () => {
    let report = governance;
    if (!report) {
      report = await getGovernance(id);
      setGovernance(report);
    }
    const url = URL.createObjectURL(new Blob([JSON.stringify(report, null, 2)], { type: "application/json" }));
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `governance-report-${id}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="page">
      <Link className="back-link" to="/workflows"><ArrowLeft />All workflows</Link>
      <div className="detail-heading">
        <div><div className="detail-heading__badges"><Badge tone={severityTone(severity)}>{severity}</Badge><Badge tone={status === "Approved" ? "success" : status === "Rejected" ? "danger" : "neutral"}>{status.replace(/([a-z])([A-Z])/g, "$1 $2")}</Badge></div><h1>{workflow.alert?.cveId}: {workflow.alert?.packageName}</h1><p>{workflow.repository} · {workflow.workflowId} · <span className="mono">{workflow.correlationId}</span></p></div>
        {canReview && <button className="button button--secondary" onClick={() => void run("download", downloadGovernance)} disabled={busy === "download"}><Download />Governance JSON</button>}
      </div>

      <ol className="stepper" aria-label="Workflow progress">
        {steps.map((step, index) => <li key={step} className={index < completedStep ? "complete" : index === completedStep ? "current" : ""}><span>{index < completedStep ? <Check /> : index + 1}</span><strong>{step}</strong></li>)}
      </ol>

      {error && <ErrorBanner message={error} />}
      <div className="detail-tabs" role="tablist"><button role="tab" aria-selected={tab === "overview"} className={tab === "overview" ? "active" : ""} onClick={() => setTab("overview")}>Workflow overview</button>{canReview && <button role="tab" aria-selected={tab === "audit"} className={tab === "audit" ? "active" : ""} onClick={() => setTab("audit")}>Audit trail</button>}{canReview && <button role="tab" aria-selected={tab === "governance"} className={tab === "governance" ? "active" : ""} onClick={() => { setTab("governance"); if (!governance) void getGovernance(id).then(setGovernance).catch((caught) => setError(caught instanceof Error ? caught.message : "Could not load governance report.")); }}>Governance</button>}</div>

      {tab === "overview" && (
        <div className="detail-grid">
          <div className="detail-stack">
            <section className="card"><div className="card__header"><div><span className="eyebrow">Alert</span><h2>Vulnerability context</h2></div><ShieldCheck /></div><dl className="data-grid"><div><dt>Package</dt><dd>{workflow.alert?.packageName}</dd></div><div><dt>Installed</dt><dd className="mono">{workflow.alert?.installedVersion}</dd></div><div><dt>Fixed version</dt><dd className="mono">{workflow.alert?.fixedVersion}</dd></div><div><dt>Risk score</dt><dd>{workflow.riskAssessment?.riskScore}/100</dd></div></dl><p className="description">{workflow.alert?.description}</p><div className="factor-list">{workflow.riskAssessment?.riskFactors.map((factor) => <span key={factor}><ShieldCheck />{factor}</span>)}</div></section>
            <section className="card"><div className="card__header"><div><span className="eyebrow">AI recommendation</span><h2>Remediation guidance</h2></div><Bot /></div>
              {!workflow.remediation && !remediation ? <EmptyState title="No recommendation yet" detail={approval === "Approved" ? "Generate advisory remediation guidance using the configured provider." : "Human approval is required before remediation generation."} action={approval === "Approved" && canReview ? <button className="button button--primary" disabled={!!busy} onClick={() => void run("remediation", () => generateRemediation(id))}>{busy === "remediation" ? "Generating…" : "Generate recommendation"}</button> : undefined} /> : (() => { const item = remediation; return <div className="recommendation"><div className="recommendation__lead"><strong>{item?.recommendedAction || workflow.remediation?.recommendedAction}</strong><Badge tone="success">{Math.round((item?.confidenceScore || workflow.remediation?.confidenceScore || 0) * 100)}% confidence</Badge></div><p>{item?.explanation || `Upgrade to ${workflow.remediation?.targetVersion} using a minimal dependency-only change.`}</p><dl className="data-grid"><div><dt>Target version</dt><dd className="mono">{item?.targetVersion || workflow.remediation?.targetVersion}</dd></div><div><dt>Model</dt><dd>{item?.modelIdentifier || workflow.remediation?.modelIdentifier}</dd></div><div><dt>Prompt version</dt><dd>{item?.promptVersion || "remediation-v1.3"}</dd></div><div><dt>Human review</dt><dd>Required</dd></div></dl><div className="advisory-note"><LockKeyhole /><p>AI output is advisory and cannot authorize, merge, deploy, or release this change.</p></div></div>; })()}
            </section>
            <section className="card"><div className="card__header"><div><span className="eyebrow">Draft artifact</span><h2>Pull request proposal</h2></div><FileCode2 /></div>{!proposal ? <EmptyState title="No proposal generated" detail="Generate a review-ready draft after an approved remediation recommendation exists." action={(workflow.remediation || remediation) && canReview ? <button className="button button--primary" disabled={!!busy} onClick={() => void run("proposal", () => generateProposal(id))}>{busy === "proposal" ? "Preparing…" : "Generate draft proposal"}</button> : undefined} /> : <div className="proposal"><Badge tone="success">Ready for review</Badge><h3>{proposal.proposedTitle}</h3><p>{proposal.proposedDescription.replaceAll("#", "").replaceAll("*", "").replaceAll("`", "")}</p><div className="proposal__columns"><div><strong>Dependency change</strong>{proposal.dependencyChanges.map((item) => <code key={item}>{item}</code>)}</div><div><strong>Validation</strong>{proposal.validationCommands.map((item) => <code key={item}>{item}</code>)}</div></div><p><strong>Rollback:</strong> {proposal.rollbackGuidance}</p></div>}</section>
          </div>
          <aside className="detail-aside">
            <section className="card approval-card"><div className="card__header"><div><span className="eyebrow">Mandatory control</span><h2>Human approval</h2></div><ClipboardCheck /></div><div className={`decision-state decision-state--${approval.toLowerCase()}`}><strong>{approval}</strong><span>{approval === "Pending" ? "Awaiting authorized reviewer" : `${workflow.approval?.reviewer}`}</span></div>{approval === "Pending" ? <>{canReview ? <><label>Decision rationale<textarea rows={4} value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Record the evidence behind this decision…" /></label><div className="decision-actions"><button className="button button--approve" disabled={!!busy} onClick={() => void run("approve", () => decide(id, "approved", reason || "Reviewed risk assessment and approved remediation planning."))}><Check />Approve</button><button className="button button--reject" disabled={!!busy || reason.trim().length < 8} onClick={() => void run("reject", () => decide(id, "rejected", reason))}><X />Reject</button></div><small>Rejection requires a recorded rationale.</small></> : <p className="permission-note">Switch to SecurityReviewer or Admin to make a decision.</p>}</> : <dl className="decision-details"><div><dt>Reviewer</dt><dd>{workflow.approval?.reviewer}</dd></div><div><dt>Decision time</dt><dd>{workflow.approval?.decidedAt && new Date(workflow.approval.decidedAt).toLocaleString()}</dd></div><div><dt>Rationale</dt><dd>{workflow.approval?.reason}</dd></div></dl>}</section>
            <section className="card trace-card"><span className="eyebrow">Traceability</span><h2>Workflow metadata</h2><dl><div><dt>Created</dt><dd>{new Date(workflow.createdAt).toLocaleString()}</dd></div><div><dt>Updated</dt><dd>{new Date(workflow.updatedAt).toLocaleString()}</dd></div><div><dt>Approval level</dt><dd>{workflow.riskAssessment?.requiredApprovalLevel}</dd></div><div><dt>Next step</dt><dd>{workflow.nextStep}</dd></div></dl></section>
          </aside>
        </div>
      )}

      {tab === "audit" && <section className="card"><div className="card__header"><div><span className="eyebrow">Immutable evidence</span><h2>Audit timeline</h2></div><span className="card__meta">{audit.length} events</span></div>{audit.length === 0 ? <LoadingState label="Loading audit trail" /> : <ol className="timeline">{audit.slice().reverse().map((event) => <li key={event.id}><span className={`timeline__dot timeline__dot--${event.level.toLowerCase()}`} /><div><div><Badge tone={event.level === "Warning" ? "warning" : "neutral"}>{event.eventType}</Badge><time>{new Date(event.timestamp).toLocaleString()}</time></div><h3>{event.summary}</h3>{event.details && <p>{event.details}</p>}<small>{event.actor} · {event.service} · <span className="mono">{event.correlationId}</span></small></div></li>)}</ol>}</section>}
      {tab === "governance" && <section className="card governance-card"><div className="card__header"><div><span className="eyebrow">Reviewable record</span><h2>Governance report</h2></div><button className="button button--secondary" onClick={() => void run("download", downloadGovernance)}><Download />Download JSON</button></div>{!governance ? <LoadingState label="Generating governance view" /> : <><div className="governance-summary"><ShieldCheck /><div><strong>{governance.approvalStatus}</strong><span>Required level: {governance.requiredApprovalLevel}</span></div></div><pre>{JSON.stringify(governance, null, 2)}</pre></>}</section>}
    </div>
  );
}
