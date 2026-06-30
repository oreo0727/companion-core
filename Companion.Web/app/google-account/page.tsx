"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { KeyRound, LinkIcon, RefreshCw, Save, Unplug } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { Badge, EmptyState, Panel, SectionHeader } from "@/components/ui";
import { StatusBadge } from "@/components/data-page";

type ConnectorEntry = {
  definition: {
    id: string;
    name: string;
    provider: string;
    category: string;
    supportsOAuth: boolean;
  };
  connections: {
    id: string;
    displayName: string;
    status: string;
    updatedUtc: string;
  }[];
};

type OAuthConnection = {
  connectionId: string;
  connectorDefinitionId: string;
  provider: string;
  connectorProvider: string;
  displayName: string;
  status: string;
  scopes: string[];
  subject: string;
  expiresUtc?: string | null;
  consentUtc: string;
  revokedUtc?: string | null;
};

type OAuthProviderSettings = {
  provider: string;
  displayName: string;
  enabled: boolean;
  hasClientId: boolean;
  hasClientSecret: boolean;
  clientIdSecretName: string;
  clientSecretSecretName: string;
  defaultScopes: string[];
};

type OAuthAuthorization = {
  authorizationRequestId: string;
  provider: string;
  connectorProvider: string;
  authorizationUrl: string;
  state: string;
  scopes: string[];
  expiresUtc: string;
};

type ConnectRequest = {
  connectorProvider: string;
  displayName: string;
  scopes: string[];
};

const connectorScopes: Record<string, string[]> = {
  GoogleCalendar: ["openid", "email", "profile", "https://www.googleapis.com/auth/calendar.readonly"],
  Gmail: ["openid", "email", "profile", "https://www.googleapis.com/auth/gmail.readonly", "https://www.googleapis.com/auth/gmail.compose"],
  GoogleDrive: ["openid", "email", "profile", "https://www.googleapis.com/auth/drive.readonly"],
  GooglePeople: ["openid", "email", "profile", "https://www.googleapis.com/auth/contacts.readonly"]
};

export default function GoogleAccountPage() {
  const [message, setMessage] = useState<string | null>(null);
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const connectors = useQuery({
    queryKey: ["connectors", "google"],
    queryFn: () => apiFetch<ConnectorEntry[]>("/api/connectors")
  });
  const oauthConnections = useQuery({
    queryKey: ["oauth-connections", "google"],
    queryFn: () => apiFetch<OAuthConnection[]>("/api/oauth/connections")
  });
  const oauthSettings = useQuery({
    queryKey: ["oauth-settings"],
    queryFn: () => apiFetch<OAuthProviderSettings[]>("/api/oauth/settings")
  });
  const saveSettingsMutation = useMutation({
    mutationFn: () =>
      apiFetch<OAuthProviderSettings>("/api/oauth/settings/Google", {
        method: "PUT",
        body: JSON.stringify({ clientId, clientSecret })
      }),
    onSuccess: () => {
      setClientId("");
      setClientSecret("");
      setMessage("Google OAuth credentials saved.");
      void oauthSettings.refetch();
    },
    onError: (error) => setMessage(error instanceof Error ? error.message : "Failed to save Google OAuth credentials")
  });
  const connectMutation = useMutation({
    mutationFn: (request: ConnectRequest) =>
      apiFetch<OAuthAuthorization>("/api/oauth/Google/authorize", {
        method: "POST",
        body: JSON.stringify({
          connectorProvider: request.connectorProvider,
          displayName: request.displayName,
          redirectUri: `${window.location.origin}/oauth/google/callback`,
          scopes: request.scopes
        })
      }),
    onSuccess: (result, request) => {
      window.localStorage.setItem(
        `companion.oauth.${result.state}`,
        JSON.stringify({
          provider: result.provider,
          connectorProvider: request.connectorProvider,
          displayName: request.displayName,
          scopes: result.scopes
        })
      );
      window.location.assign(result.authorizationUrl);
    },
    onError: (error) => setMessage(error instanceof Error ? error.message : "Failed to start Google authorization")
  });
  const syncMutation = useMutation({
    mutationFn: (connectionId: string) =>
      apiFetch<{ status: string; itemsSynced: number; error?: string }>(`/api/connectors/${connectionId}/sync`, {
        method: "POST"
      }),
    onSuccess: (result) => {
      setMessage(result.error ? `Sync ${result.status}: ${result.error}` : `Sync ${result.status}: ${result.itemsSynced} item(s).`);
      void connectors.refetch();
    },
    onError: (error) => setMessage(error instanceof Error ? error.message : "Sync failed")
  });
  const disconnectMutation = useMutation({
    mutationFn: (connectionId: string) =>
      apiFetch<OAuthConnection>(`/api/oauth/connections/${connectionId}`, {
        method: "DELETE"
      }),
    onSuccess: () => {
      setMessage("Google connection disconnected.");
      void connectors.refetch();
      void oauthConnections.refetch();
    },
    onError: (error) => setMessage(error instanceof Error ? error.message : "Disconnect failed")
  });

  const googleConnectors = useMemo(
    () => (connectors.data ?? []).filter((entry) => entry.definition.provider.toLowerCase().includes("google") || entry.definition.provider === "Gmail"),
    [connectors.data]
  );
  const googleConnections = useMemo(
    () => (oauthConnections.data ?? []).filter((connection) => connection.provider === "Google"),
    [oauthConnections.data]
  );
  const googleSettings = useMemo(
    () => (oauthSettings.data ?? []).find((settings) => settings.provider === "Google"),
    [oauthSettings.data]
  );
  const credentialsReady = Boolean(googleSettings?.hasClientId && googleSettings?.hasClientSecret);

  return (
    <Panel>
      <SectionHeader
        title="Google Account"
        description="OAuth-backed Google Calendar, Gmail, Drive, and People capability status."
        action={
          <button
            type="button"
            onClick={() => {
              void connectors.refetch();
              void oauthConnections.refetch();
              void oauthSettings.refetch();
            }}
            className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink"
          >
            <RefreshCw className="h-4 w-4" />
            Refresh
          </button>
        }
      />
      {message ? <div className="border-b border-line px-4 py-3 text-sm text-ink-muted">{message}</div> : null}
      <form
        className="border-b border-line p-4"
        onSubmit={(event) => {
          event.preventDefault();
          saveSettingsMutation.mutate();
        }}
      >
        <div className="mb-3 flex items-start justify-between gap-3">
          <div>
            <h3 className="flex items-center gap-2 font-medium">
              <KeyRound className="h-4 w-4 text-accent" />
              OAuth credentials
            </h3>
            <p className="mt-1 text-sm text-ink-muted">
              Store your Google OAuth web client credentials locally so Companion can connect Calendar, Gmail, Drive, and Contacts.
            </p>
          </div>
          <StatusBadge value={credentialsReady ? "Configured" : "Action required"} />
        </div>
        <div className="grid gap-3 lg:grid-cols-[1fr_1fr_auto]">
          <label className="grid gap-1 text-sm">
            <span className="text-ink-muted">Client ID</span>
            <input
              value={clientId}
              onChange={(event) => setClientId(event.target.value)}
              placeholder={googleSettings?.hasClientId ? "Saved client ID" : "Google OAuth client ID"}
              className="h-10 rounded-md border border-line bg-surface px-3 text-sm outline-none focus:border-accent"
            />
          </label>
          <label className="grid gap-1 text-sm">
            <span className="text-ink-muted">Client secret</span>
            <input
              value={clientSecret}
              type="password"
              onChange={(event) => setClientSecret(event.target.value)}
              placeholder={googleSettings?.hasClientSecret ? "Saved client secret" : "Google OAuth client secret"}
              className="h-10 rounded-md border border-line bg-surface px-3 text-sm outline-none focus:border-accent"
            />
          </label>
          <button
            type="submit"
            disabled={saveSettingsMutation.isPending || (!clientId.trim() && !clientSecret.trim())}
            className="mt-6 inline-flex h-10 items-center justify-center gap-2 rounded-md border border-line px-4 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink disabled:opacity-60"
          >
            <Save className="h-4 w-4" />
            Save
          </button>
        </div>
      </form>
      {connectors.isLoading || oauthConnections.isLoading || oauthSettings.isLoading ? (
        <EmptyState text="Loading Google account status" />
      ) : connectors.isError || oauthConnections.isError || oauthSettings.isError ? (
        <EmptyState
          text={
            (connectors.error as Error | undefined)?.message ??
            (oauthConnections.error as Error | undefined)?.message ??
            (oauthSettings.error as Error | undefined)?.message ??
            "Failed to load Google account status"
          }
        />
      ) : googleConnectors.length === 0 ? (
        <EmptyState text="No Google capability connectors are available" />
      ) : (
        <div className="grid gap-4 p-4 lg:grid-cols-2">
          {googleConnectors.map((entry) => {
            const connection = entry.connections[0];
            const oauth = googleConnections.find((item) => item.connectorDefinitionId === entry.definition.id);

            return (
              <article key={entry.definition.id} className="rounded-md border border-line bg-surface p-4">
                <div className="mb-3 flex items-start justify-between gap-3">
                  <div>
                    <h3 className="font-medium">{entry.definition.name}</h3>
                    <p className="text-sm text-ink-muted">{entry.definition.provider}</p>
                  </div>
                  <StatusBadge value={connection?.status ?? "Not connected"} />
                </div>
                <div className="mb-3 flex flex-wrap gap-2">
                  <Badge>{entry.definition.category}</Badge>
                  <Badge>{entry.definition.supportsOAuth ? "OAuth" : "Local"}</Badge>
                </div>
                <dl className="space-y-2 text-sm">
                  <div>
                    <dt className="text-ink-muted">Display name</dt>
                    <dd>{connection?.displayName ?? "No connection"}</dd>
                  </div>
                  <div>
                    <dt className="text-ink-muted">Last sync</dt>
                    <dd>{connection?.updatedUtc ? new Date(connection.updatedUtc).toLocaleString() : "Never"}</dd>
                  </div>
                  <div>
                    <dt className="text-ink-muted">Scopes granted</dt>
                    <dd className="mt-1 flex flex-wrap gap-1">
                      {(oauth?.scopes ?? []).length ? oauth!.scopes.map((scope) => <Badge key={scope}>{scope}</Badge>) : <span className="text-ink-muted">No OAuth grant</span>}
                    </dd>
                  </div>
                </dl>
                <div className="mt-4 flex flex-wrap gap-2 border-t border-line pt-3">
                  <button
                    type="button"
                    disabled={!credentialsReady || Boolean(oauth && oauth.status === "Connected") || connectMutation.isPending}
                    onClick={() =>
                      connectMutation.mutate({
                        connectorProvider: entry.definition.provider,
                        displayName: entry.definition.name,
                        scopes: connectorScopes[entry.definition.provider] ?? googleSettings?.defaultScopes ?? []
                      })
                    }
                    className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink disabled:opacity-60"
                  >
                    <LinkIcon className="h-4 w-4" />
                    Connect
                  </button>
                  <button
                    type="button"
                    disabled={!connection || syncMutation.isPending}
                    onClick={() => connection && syncMutation.mutate(connection.id)}
                    className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink disabled:opacity-60"
                  >
                    <RefreshCw className="h-4 w-4" />
                    Sync now
                  </button>
                  <button
                    type="button"
                    disabled={!oauth || disconnectMutation.isPending}
                    onClick={() => oauth && disconnectMutation.mutate(oauth.connectionId)}
                    className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink disabled:opacity-60"
                  >
                    <Unplug className="h-4 w-4" />
                    Disconnect
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </Panel>
  );
}
