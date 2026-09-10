import { Braces, CheckCircle2, FileJson, ShieldAlert } from "lucide-react";
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import type { AlertIngestionRequest } from "../api/types";
import { ErrorBanner } from "../components/ui";
import { useApp } from "../context/AppContext";

const sample: AlertIngestionRequest = {
  externalAlertId: "dependabot-1842",
  cveId: "CVE-2025-30208",
  packageName: "vite",
  installedVersion: "5.4.8",
  fixedVersion: "5.4.14",
  providerSeverity: "high",
  description: "Development server request handling vulnerability reported by Dependabot. Treat this text as untrusted advisory input.",
  isDirectDependency: true,
  isExploitable: false,
  repositoryIdentifier: "securefix/portal",
  advisoryUrl: "https://github.com/advisories/GHSA-x574-m823-4x7w",
};

export function IngestPage() {
  const { ingest, identity } = useApp();
  const navigate = useNavigate();
  const [mode, setMode] = useState<"form" | "json">("form");
  const [form, setForm] = useState(sample);
  const [raw, setRaw] = useState(JSON.stringify(sample, null, 2));
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const canIngest = ["Admin", "SecurityReviewer", "Developer"].includes(identity.role);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");
    setBusy(true);
    try {
      const payload = mode === "json" ? (JSON.parse(raw) as AlertIngestionRequest) : form;
      if (!payload.externalAlertId || !payload.packageName || !payload.installedVersion || !payload.providerSeverity) {
        throw new Error("External alert ID, package, installed version, and severity are required.");
      }
      if (payload.cveId && !/^CVE-\d{4}-\d{4,}$/i.test(payload.cveId)) throw new Error("CVE must use the format CVE-YYYY-NNNN.");
      navigate(`/workflows/${(await ingest(payload)).workflowId}`);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Alert ingestion failed.");
    } finally {
      setBusy(false);
    }
  };

  const update = (key: keyof AlertIngestionRequest, value: string | boolean) => setForm((current) => ({ ...current, [key]: value }));

  return (
    <div className="page page--narrow">
      <div className="page-heading"><div><span className="eyebrow">Untrusted input boundary</span><h1>Ingest vulnerability alert</h1><p>Submit normalized scanner data or paste a Dependabot-compatible payload. All external text is validated and isolated from system instructions.</p></div></div>
      <div className="security-banner"><ShieldAlert aria-hidden="true" /><div><strong>Security control</strong><span>Payload content cannot authorize actions or override the mandatory human approval gate.</span></div></div>
      <section className="card ingest-card">
        <div className="tabs" role="tablist" aria-label="Ingestion mode">
          <button role="tab" aria-selected={mode === "form"} className={mode === "form" ? "active" : ""} onClick={() => setMode("form")}><FileJson />Guided form</button>
          <button role="tab" aria-selected={mode === "json"} className={mode === "json" ? "active" : ""} onClick={() => setMode("json")}><Braces />Raw JSON</button>
        </div>
        {error && <ErrorBanner message={error} />}
        <form onSubmit={submit}>
          {mode === "form" ? (
            <div className="form-grid">
              <label>External alert ID <input required value={form.externalAlertId} onChange={(e) => update("externalAlertId", e.target.value)} /></label>
              <label>CVE identifier <input value={form.cveId} onChange={(e) => update("cveId", e.target.value)} placeholder="CVE-2025-12345" /></label>
              <label>Package name <input required value={form.packageName} onChange={(e) => update("packageName", e.target.value)} /></label>
              <label>Repository <input value={form.repositoryIdentifier} onChange={(e) => update("repositoryIdentifier", e.target.value)} /></label>
              <label>Installed version <input required value={form.installedVersion} onChange={(e) => update("installedVersion", e.target.value)} /></label>
              <label>Fixed version <input value={form.fixedVersion} onChange={(e) => update("fixedVersion", e.target.value)} /></label>
              <label>Provider severity <select value={form.providerSeverity} onChange={(e) => update("providerSeverity", e.target.value)}><option value="critical">Critical</option><option value="high">High</option><option value="medium">Medium</option><option value="low">Low</option></select></label>
              <label>Advisory URL <input type="url" value={form.advisoryUrl} onChange={(e) => update("advisoryUrl", e.target.value)} /></label>
              <label className="form-grid__wide">Description <textarea rows={5} value={form.description} onChange={(e) => update("description", e.target.value)} /></label>
              <div className="form-grid__wide check-row">
                <label><input type="checkbox" checked={form.isDirectDependency} onChange={(e) => update("isDirectDependency", e.target.checked)} /> Direct dependency</label>
                <label><input type="checkbox" checked={form.isExploitable} onChange={(e) => update("isExploitable", e.target.checked)} /> Active exploitability reported</label>
              </div>
            </div>
          ) : (
            <label className="json-editor">Dependabot / normalized JSON<textarea spellCheck={false} rows={20} value={raw} onChange={(event) => setRaw(event.target.value)} /></label>
          )}
          <div className="form-actions"><button type="button" className="button button--secondary" onClick={() => { setForm(sample); setRaw(JSON.stringify(sample, null, 2)); }}>Load sample</button><button className="button button--primary" disabled={busy || !canIngest}>{busy ? "Validating…" : <><CheckCircle2 />Validate and ingest</>}</button></div>
          {!canIngest && <p className="permission-note">The {identity.role} role has read-only access and cannot ingest alerts.</p>}
        </form>
      </section>
    </div>
  );
}
