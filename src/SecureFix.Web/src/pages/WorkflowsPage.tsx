import { ArrowRight, ChevronLeft, ChevronRight, Search, SlidersHorizontal } from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Badge, EmptyState, ErrorBanner, LoadingState, severityName, severityTone, statusName } from "../components/ui";
import { useApp } from "../context/AppContext";

const pageSize = 5;

export function WorkflowsPage() {
  const { workflows, dataError, dataLoading } = useApp();
  const [query, setQuery] = useState("");
  const [severity, setSeverity] = useState("All");
  const [status, setStatus] = useState("All");
  const [page, setPage] = useState(1);
  const filtered = useMemo(
    () =>
      workflows.filter((workflow) => {
        const haystack = `${workflow.workflowId} ${workflow.alert?.cveId} ${workflow.alert?.packageName} ${workflow.repository}`.toLowerCase();
        return (
          haystack.includes(query.toLowerCase()) &&
          (severity === "All" || severityName(workflow.riskAssessment?.normalizedSeverity) === severity) &&
          (status === "All" || statusName(workflow.status) === status)
        );
      }),
    [query, severity, status, workflows],
  );
  const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const currentPage = Math.min(page, pages);
  const visible = filtered.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  return (
    <div className="page">
      <div className="page-heading"><div><span className="eyebrow">Governed work queue</span><h1>Vulnerability workflows</h1><p>Trace every alert from ingestion through risk assessment, human decision, and draft remediation.</p></div><Link className="button button--primary" to="/ingest">Ingest alert</Link></div>
      {dataError && <ErrorBanner message={dataError} />}
      {dataLoading && <LoadingState label="Loading workflow queue" />}
      <section className="card">
        <div className="filters" aria-label="Workflow filters">
          <label className="search-field"><Search aria-hidden="true" /><span className="sr-only">Search workflows</span><input value={query} onChange={(event) => { setQuery(event.target.value); setPage(1); }} placeholder="Search CVE, package, repository…" /></label>
          <label><span className="sr-only">Filter by severity</span><select value={severity} onChange={(event) => { setSeverity(event.target.value); setPage(1); }}><option>All</option><option>Critical</option><option>High</option><option>Medium</option><option>Low</option></select></label>
          <label><span className="sr-only">Filter by status</span><select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}><option>All</option><option>PendingApproval</option><option>Approved</option><option>Recommended</option><option>Rejected</option><option>Failed</option></select></label>
          <span className="filter-count"><SlidersHorizontal aria-hidden="true" />{filtered.length} results</span>
        </div>
        {visible.length === 0 ? (
          <EmptyState title="No workflows match" detail="Adjust the search or filters to see security workflows." />
        ) : (
          <>
            <div className="table-wrap">
              <table>
                <thead><tr><th>Workflow</th><th>Package</th><th>Repository</th><th>Severity</th><th>Status</th><th>Human gate</th><th><span className="sr-only">Open</span></th></tr></thead>
                <tbody>
                  {visible.map((workflow) => {
                    const sev = severityName(workflow.riskAssessment?.normalizedSeverity);
                    const state = statusName(workflow.status);
                    return (
                      <tr key={workflow.workflowId}>
                        <td><Link className="table-primary" to={`/workflows/${workflow.workflowId}`}>{workflow.alert?.cveId}<span>{workflow.workflowId}</span></Link></td>
                        <td><strong>{workflow.alert?.packageName}</strong><span className="table-secondary">{workflow.alert?.installedVersion} → {workflow.alert?.fixedVersion}</span></td>
                        <td>{workflow.repository || "Not specified"}</td>
                        <td><Badge tone={severityTone(sev)}>{sev}</Badge></td>
                        <td><Badge tone={state === "Approved" ? "success" : state === "Rejected" || state === "Failed" ? "danger" : "neutral"}>{state.replace(/([a-z])([A-Z])/g, "$1 $2")}</Badge></td>
                        <td>{workflow.approval?.status === "Pending" ? "Required" : workflow.approval?.reviewer?.split("@")[0]}</td>
                        <td><Link className="row-link" to={`/workflows/${workflow.workflowId}`} aria-label={`Open workflow ${workflow.workflowId}`}><ArrowRight /></Link></td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <div className="pagination">
              <span>Showing {(currentPage - 1) * pageSize + 1}–{Math.min(currentPage * pageSize, filtered.length)} of {filtered.length}</span>
              <div><button className="icon-button" disabled={currentPage === 1} onClick={() => setPage((value) => value - 1)} aria-label="Previous page"><ChevronLeft /></button><span>Page {currentPage} of {pages}</span><button className="icon-button" disabled={currentPage === pages} onClick={() => setPage((value) => value + 1)} aria-label="Next page"><ChevronRight /></button></div>
            </div>
          </>
        )}
      </section>
    </div>
  );
}
