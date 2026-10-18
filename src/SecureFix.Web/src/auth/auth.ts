import {
  PublicClientApplication,
  type AccountInfo,
  type AuthenticationResult,
  type Configuration,
} from "@azure/msal-browser";
import type { Identity, Role } from "../api/types";

export interface AuthAdapter {
  initialize(): Promise<Identity>;
  getAccessToken(): Promise<string>;
  signIn(): Promise<Identity>;
  signOut(): Promise<void>;
}

const configuredRole = (role: string | undefined): Role =>
  ["Admin", "SecurityReviewer", "Developer", "Viewer"].includes(role ?? "")
    ? (role as Role)
    : "Developer";

export class DemoAuthAdapter implements AuthAdapter {
  constructor(private identity: Identity) {}

  async initialize() {
    return this.identity;
  }

  async getAccessToken() {
    return import.meta.env.VITE_DEMO_TOKEN || "securefix-demo-token";
  }

  async signIn() {
    return this.identity;
  }

  async signOut() {}
}

export class EntraMsalAuthAdapter implements AuthAdapter {
  private readonly client: PublicClientApplication;
  private account?: AccountInfo;
  private readonly scope: string;

  constructor() {
    const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID;
    const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID;
    this.scope = import.meta.env.VITE_ENTRA_API_SCOPE;
    if (!clientId || !tenantId || !this.scope) {
      throw new Error("Entra auth requires VITE_ENTRA_CLIENT_ID, VITE_ENTRA_TENANT_ID, and VITE_ENTRA_API_SCOPE.");
    }

    const config: Configuration = {
      auth: {
        clientId,
        authority: `https://login.microsoftonline.com/${tenantId}`,
        redirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI || window.location.origin,
        postLogoutRedirectUri: window.location.origin,
      },
      cache: { cacheLocation: "sessionStorage" },
    };
    this.client = new PublicClientApplication(config);
  }

  async initialize() {
    await this.client.initialize();
    const redirect = await this.client.handleRedirectPromise();
    this.account = redirect?.account ?? this.client.getAllAccounts()[0];
    return this.account ? this.toIdentity(this.account, redirect) : this.signIn();
  }

  async signIn() {
    const result = await this.client.loginPopup({ scopes: [this.scope] });
    this.account = result.account;
    return this.toIdentity(result.account, result);
  }

  async getAccessToken() {
    if (!this.account) throw new Error("No signed-in Entra account.");
    try {
      return (await this.client.acquireTokenSilent({ account: this.account, scopes: [this.scope] })).accessToken;
    } catch {
      return (await this.client.acquireTokenPopup({ account: this.account, scopes: [this.scope] })).accessToken;
    }
  }

  async signOut() {
    await this.client.logoutPopup({ account: this.account });
  }

  private toIdentity(account: AccountInfo, result?: AuthenticationResult | null): Identity {
    const claims = (result?.idTokenClaims ?? account.idTokenClaims) as { roles?: string[] } | undefined;
    return {
      name: account.name || account.username,
      email: account.username,
      role: configuredRole(claims?.roles?.[0]),
      authMode: "entra",
    };
  }
}

export const createAuthAdapter = (identity: Identity): AuthAdapter =>
  import.meta.env.VITE_AUTH_MODE === "entra"
    ? new EntraMsalAuthAdapter()
    : new DemoAuthAdapter(identity);
