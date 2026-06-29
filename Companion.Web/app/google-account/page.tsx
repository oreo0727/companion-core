"use client";

import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { RefreshCw, Unplug } from "lucide-react";
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

export default function GoogleAccountPage() {
  const [message, setMessage] = useState<string | null>(null);
  const connectors = useQuery({
    queryKey: ["connectors", "google"],
    queryFn: () => apiFetch<ConnectorEntry[]>("/api/connectors")
  });
  const oauthConnections = useQuery({
    queryKey: ["oauth-connections", "google"],
    queryFn: () => apiFetch<OAuthConnection[]>("/api/oauth/connections")
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
            }}
            className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink"
          >
            <RefreshCw className="h-4 w-4" />
            Refresh
          </button>
        }
      />
      {message ? <div className="border-b border-line px-4 py-3 text-sm text-ink-muted">{message}</div> : null}
      {connectors.isLoading || oauthConnections.isLoading ? (
        <EmptyState text="Loading Google account status" />
      ) : connectors.isError || oauthConnections.isError ? (
        <EmptyState text={(connectors.error as Error | undefined)?.message ?? (oauthConnections.error as Error | undefined)?.message ?? "Failed to load Google account status"} />
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
                <div className="mt-4 flex gap-2 border-t border-line pt-3">
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
