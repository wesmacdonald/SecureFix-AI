import { ArrowRight, Bot, CheckCircle2, Clock3, ShieldAlert, TimerReset } from "lucide-react";
import { Link } from "react-router-dom";
import { Badge, ErrorBanner, LoadingState, ProgressBar, severityName, severityTone, statusName } from "../components/ui";
import { useApp } from "../context/AppContext";

export function OverviewPage() {
  const { workflows, dataError, dataLoading } = useApp();
  const pending = workflows.filter((item) => statusName(item.status) === "PendingApproval").length;
  const critical = workflows.filter((item) => severityName(item.riskAssessment?.normalizedSeverity) === "Critical").length;
  const approved = workflows.filter((item) => statusName(item.status) === "Approved").length;
  const approvalRate = Math.round((approved / Math.max(workflows.filter((item) => ["Approved", "Rejected"].includes(statusName(item.status))).length, 1)) * 100);
  const severityCounts = (["Critical", "High", "Medium", "Low"] as const).map((severity) => ({
    severity,
    count: workflows.filter((item) => severityName(item.riskAssessment?.normalizedSeverity) === severity).length,
  }));
  const statusCounts = (["PendingApproval", "Approved", "Recommended", "Rejected", "Failed"] as const).map((status) => ({
    status,
    count: workflows.filter((item) => statusName(item.status) === status).length,
  }));

  return (
    <div className="page">
      <div className="page-heading">
        <div><span className="eyebrow">Operational overview</span><h1>Security release control center</h1><p>Monitor vulnerability workflows, human decisions, and AI-assisted remediation from one governed workspace.</p></div>
        <Link className="button button--primary" to="/ingest"><ShieldAlert aria-hidden="true" />Ingest alert</Link>
      </div>
      {dataError && <ErrorBanner message={dataError} />}
      {dataLoading && <LoadingState label="Loading dashboard workflows" />}

      <section className="kpi-grid" aria-label="Key performance indicators">
        <article className="kpi-card"><span className="kpi-card__icon"><ShieldAlert /></span><div><span>Open vulnerabilities</span><strong>{workflows.filter((item) => !["Approved", "Rejected"].includes(statusName(item.status))).length}</strong><small><b>{critical} critical</b> requiring attention</small></div></article>
        <article className="kpi-card"><span className="kpi-card__icon"><Clock3 /></span><div><span>Awaiting approval</span><strong>{pending}</strong><small>Human review remains mandatory</small></div></article>
        <article className="kpi-card"><span className="kpi-card__icon"><CheckCircle2 /></span><div><span>Approval rate</span><strong>{approvalRate}%</strong><small>Across completed decisions</small></div></article>
        <article className="kpi-card"><span className="kpi-card__icon"><TimerReset /></span><div><span>AI guidance ready</span><strong>{workflows.filter((item) => item.remediation).length}</strong><small>Advisory recommendations generated</small></div></article>
      </section>

      <section className="dashboard-grid">
        <article className="card">
          <div className="card__header"><div><span className="eyebrow">Risk posture</span><h2>Severity distribution</h2></div><span className="card__meta">{workflows.length} total</span></div>
          <div className="distribution">
            {severityCounts.map(({ severity, count }) => (
              <div className="distribution__row" key={severity}>
                <div><Badge tone={severityTone(severity)}>{severity}</Badge><strong>{count}</strong></div>
                <ProgressBar value={(count / Math.max(workflows.length, 1)) * 100} label={`${severity} vulnerabilities`} />
              </div>
            ))}
          </div>
        </article>
        <article className="card">
          <div className="card__header"><div><span className="eyebrow">Pipeline</span><h2>Workflow state</h2></div><Bot aria-hidden="true" /></div>
          <div className="status-bars">
            {statusCounts.map(({ status, count }) => (
              <div key={status}><span>{status === "PendingApproval" ? "Pending approval" : status}</span><strong>{count}</strong><ProgressBar value={(count / Math.max(workflows.length, 1)) * 100} label={`${status} workflows`} /></div>
            ))}
          </div>
          <div className="advisory-note"><Bot aria-hidden="true" /><p><strong>Advisory automation only.</strong> SecureFix can recommend and prepare draft artifacts, but cannot approve, merge, deploy, or release.</p></div>
        </article>
      </section>

      <section className="card recent-card">
        <div className="card__header"><div><span className="eyebrow">Live queue</span><h2>Recent workflow activity</h2></div><Link className="text-link" to="/workflows">View all <ArrowRight /></Link></div>
        <div className="table-wrap">
          <table>
            <thead><tr><th>Vulnerability</th><th>Repository</th><th>Severity</th><th>Status</th><th>Risk score</th><th>Updated</th><th><span className="sr-only">Open</span></th></tr></thead>
            <tbody>
              {workflows.slice(0, 5).map((workflow) => {
                const severity = severityName(workflow.riskAssessment?.normalizedSeverity);
                return (
                  <tr key={workflow.workflowId}>
                    <td><Link className="table-primary" to={`/workflows/${workflow.workflowId}`}>{workflow.alert?.cveId}<span>{workflow.alert?.packageName} {workflow.alert?.installedVersion}</span></Link></td>
                    <td>{workflow.repository || "Not specified"}</td>
                    <td><Badge tone={severityTone(severity)}>{severity}</Badge></td>
                    <td><Badge tone={statusName(workflow.status) === "Approved" ? "success" : statusName(workflow.status) === "Failed" ? "danger" : "neutral"}>{statusName(workflow.status).replace(/([a-z])([A-Z])/g, "$1 $2")}</Badge></td>
                    <td><strong>{workflow.riskAssessment?.riskScore}</strong>/100</td>
                    <td>{new Intl.RelativeTimeFormat("en", { numeric: "auto" }).format(-Math.max(1, Math.round((Date.now() - new Date(workflow.updatedAt).getTime()) / 3_600_000)), "hour")}</td>
                    <td><Link className="row-link" to={`/workflows/${workflow.workflowId}`} aria-label={`Open ${workflow.alert?.cveId}`}><ArrowRight /></Link></td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
