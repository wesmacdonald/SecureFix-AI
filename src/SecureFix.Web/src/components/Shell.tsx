import {
  Activity,
  Bell,
  ChevronDown,
  FileCheck2,
  LayoutDashboard,
  Menu,
  Plus,
  ShieldCheck,
  X,
} from "lucide-react";
import { useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import type { Role } from "../api/types";
import { useApp } from "../context/AppContext";

const navigation = [
  { to: "/", label: "Overview", icon: LayoutDashboard },
  { to: "/workflows", label: "Workflows", icon: Activity },
  { to: "/ingest", label: "Ingest alert", icon: Plus },
];

export function Shell() {
  const { identity, setRole, isDemo } = useApp();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [identityOpen, setIdentityOpen] = useState(false);

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">Skip to main content</a>
      <aside className={`sidebar ${mobileOpen ? "sidebar--open" : ""}`} aria-label="Primary navigation">
        <div className="brand">
          <div className="brand__mark"><ShieldCheck aria-hidden="true" /></div>
          <div><strong>SecureFix</strong><span>AI release engineer</span></div>
          <button className="icon-button sidebar__close" onClick={() => setMobileOpen(false)} aria-label="Close navigation"><X /></button>
        </div>
        <nav className="nav-list">
          {navigation.map(({ to, label, icon: Icon }) => (
            <NavLink key={to} to={to} end={to === "/"} onClick={() => setMobileOpen(false)}>
              <Icon aria-hidden="true" />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar__footer">
          <div className="guardrail">
            <FileCheck2 aria-hidden="true" />
            <div><strong>Human approval enforced</strong><span>AI actions remain advisory</span></div>
          </div>
          <span className="environment-pill">{isDemo ? "Demo environment" : "Live API"}</span>
        </div>
      </aside>
      <div className="app-main">
        <header className="topbar">
          <button className="icon-button mobile-menu" onClick={() => setMobileOpen(true)} aria-label="Open navigation"><Menu /></button>
          <div className="topbar__context">
            <span className="eyebrow">Security operations</span>
            <strong>Release assurance</strong>
          </div>
          <div className="topbar__actions">
            <button className="icon-button notification-button" aria-label="Notifications"><Bell /><span aria-hidden="true" /></button>
            <div className="identity">
              <button className="identity__button" onClick={() => setIdentityOpen((open) => !open)} aria-expanded={identityOpen}>
                <span className="avatar" aria-hidden="true">{identity.name.split(" ").map((part) => part[0]).join("")}</span>
                <span className="identity__copy"><strong>{identity.name}</strong><small>{identity.role}</small></span>
                <ChevronDown aria-hidden="true" />
              </button>
              {identityOpen && (
                <div className="identity__menu">
                  <span className="eyebrow">{identity.authMode === "demo" ? "Demo identity role" : "Microsoft Entra identity"}</span>
                  <p>{identity.email}</p>
                  {identity.authMode === "demo" ? (
                    <>
                      <label htmlFor="role-switcher">Act as</label>
                      <select id="role-switcher" value={identity.role} onChange={(event) => setRole(event.target.value as Role)}>
                        <option>Admin</option>
                        <option>SecurityReviewer</option>
                        <option>Developer</option>
                        <option>Viewer</option>
                      </select>
                      <small>Demo roles are self-asserted and must never be enabled in production.</small>
                    </>
                  ) : (
                    <small>Role assignments are read from Microsoft Entra app-role claims.</small>
                  )}
                </div>
              )}
            </div>
          </div>
        </header>
        <main id="main-content"><Outlet /></main>
      </div>
      {mobileOpen && <button className="sidebar-backdrop" onClick={() => setMobileOpen(false)} aria-label="Close navigation overlay" />}
    </div>
  );
}
