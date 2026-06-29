"use client";

import { ChangeEvent, useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Activity, DatabaseBackup, Download, FileUp, RefreshCw, Server, TerminalSquare } from "lucide-react";
import { apiFetch, type JsonRecord } from "@/lib/api";
import { Badge, EmptyState, Panel, SectionHeader } from "@/components/ui";
import { StatusBadge } from "@/components/data-page";

type Health = {
  status: string;
  generatedUtc: string;
  uptimeSeconds: number;
  environmentName: string;
  databaseOk: boolean;
  pendingAgentRuns: number;
  failedAgentRuns: number;
  pendingApprovals: number;
  unreadNotifications: number;
  enabledAiProviders: number;
  connectedConnectors: number;
  recentFailures: string[];
};

type Diagnostics = {
  environmentName: string;
  machineName: string;
  dotnetVersion: string;
  processId: number;
  workingDirectory: string;
  databaseProvider: string;
  counts: Record<string, number>;
  providers: {
    provider: string;
    model: string;
    apiBaseUrl: string;
    isEnabled: boolean;
    timeoutSeconds: number;
    updatedUtc: string;
  }[];
  connectors: {
    id: string;
    name: string;
    provider: string;
    category: string;
    enabled: boolean;
    supportsOAuth: boolean;
    riskLevel: string;
    connectionCount: number;
    lastSyncStatus?: string;
  }[];
};

type LogEntry = {
  timestampUtc: string;
  source: string;
  level: string;
  message: string;
  error?: string;
};

type SmokeStatus = {
  scriptFound: boolean;
  scriptPath: string;
  recommendedCommand: string;
  status: string;
  notes: string;
};

export default function AdminHealthPage() {
  const [restoreMessage, setRestoreMessage] = useState<string | null>(null);
  const health = useQuery({ queryKey: ["system-health"], queryFn: () => apiFetch<Health>("/api/system/health") });
  const diagnostics = useQuery({
    queryKey: ["system-diagnostics"],
    queryFn: () => apiFetch<Diagnostics>("/api/system/diagnostics")
  });
  const logs = useQuery({ queryKey: ["system-logs"], queryFn: () => apiFetch<LogEntry[]>("/api/system/logs") });
  const smoke = useQuery({
    queryKey: ["smoke-status"],
    queryFn: () => apiFetch<SmokeStatus>("/api/system/smoke-test/status")
  });
  const restore = useMutation({
    mutationFn: (payload: JsonRecord) =>
      apiFetch<{ importedUtc: string; importedCounts: Record<string, number> }>("/api/system/backup/import", {
        method: "POST",
        body: JSON.stringify(payload)
      }),
    onSuccess: (result) => {
      setRestoreMessage(
        `Imported ${Object.entries(result.importedCounts)
          .map(([key, value]) => `${value} ${key}`)
          .join(", ") || "no new records"}.`
      );
      health.refetch();
      diagnostics.refetch();
      logs.refetch();
    },
    onError: (error) => setRestoreMessage(error instanceof Error ? error.message : "Restore failed")
  });

  const counts = useMemo(() => Object.entries(diagnostics.data?.counts ?? {}), [diagnostics.data?.counts]);

  async function downloadBackup() {
    const backup = await apiFetch<JsonRecord>("/api/system/backup/export");
    const url = URL.createObjectURL(new Blob([JSON.stringify(backup, null, 2)], { type: "application/json" }));
    const link = document.createElement("a");
    link.href = url;
    link.download = `companion-backup-${new Date().toISOString().slice(0, 10)}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  async function restoreBackup(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    try {
      setRestoreMessage(null);
      restore.mutate(JSON.parse(await file.text()) as JsonRecord);
    } catch (error) {
      setRestoreMessage(error instanceof Error ? error.message : "Backup file could not be read");
    } finally {
      event.target.value = "";
    }
  }

  function refreshAll() {
    health.refetch();
    diagnostics.refetch();
    logs.refetch();
    smoke.refetch();
  }

  return (
    <div className="space-y-5">
      <Panel>
        <SectionHeader
          title="Admin Health"
          description="Daily-use diagnostics, smoke status, backups, and recent operational logs."
          action={
            <button
              type="button"
              onClick={refreshAll}
              className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink"
            >
              <RefreshCw className="h-4 w-4" />
              Refresh
            </button>
          }
        />
        {health.isLoading ? (
          <EmptyState text="Loading health" />
        ) : health.isError ? (
          <EmptyState text={(health.error as Error).message} />
        ) : (
          <div className="grid gap-4 p-4 sm:grid-cols-2 xl:grid-cols-6">
            <Metric label="Status" value={health.data?.status ?? "-"} tone={health.data?.status === "Healthy" ? "good" : "warn"} />
            <Metric label="Database" value={health.data?.databaseOk ? "Ready" : "Down"} tone={health.data?.databaseOk ? "good" : "critical"} />
            <Metric label="Pending runs" value={health.data?.pendingAgentRuns ?? 0} />
            <Metric label="Failed runs" value={health.data?.failedAgentRuns ?? 0} tone={health.data?.failedAgentRuns ? "critical" : "good"} />
            <Metric label="Approvals" value={health.data?.pendingApprovals ?? 0} />
            <Metric label="Unread" value={health.data?.unreadNotifications ?? 0} />
          </div>
        )}
      </Panel>

      <div className="grid gap-5 xl:grid-cols-[1fr_420px]">
        <Panel>
          <SectionHeader title="System Diagnostics" description="Runtime, counts, providers, and connector status." />
          {diagnostics.isLoading ? (
            <EmptyState text="Loading diagnostics" />
          ) : diagnostics.isError ? (
            <EmptyState text={(diagnostics.error as Error).message} />
          ) : (
            <div className="space-y-4 p-4">
              <div className="grid gap-3 md:grid-cols-2">
                <Info icon={Server} label="Machine" value={`${diagnostics.data?.machineName} · pid ${diagnostics.data?.processId}`} />
                <Info icon={Activity} label="Runtime" value={`.NET ${diagnostics.data?.dotnetVersion} · ${diagnostics.data?.environmentName}`} />
              </div>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5">
                {counts.map(([key, value]) => (
                  <div key={key} className="rounded-md border border-line bg-surface px-3 py-2">
                    <div className="text-xs text-ink-muted">{key}</div>
                    <div className="mt-1 text-lg font-semibold">{value}</div>
                  </div>
                ))}
              </div>
              <div className="grid gap-4 lg:grid-cols-2">
                <StatusList
                  title="Providers"
                  items={(diagnostics.data?.providers ?? []).map((provider) => ({
                    key: provider.provider,
                    title: provider.provider,
                    subtitle: `${provider.model} · ${provider.apiBaseUrl}`,
                    status: provider.isEnabled ? "Enabled" : "Disabled"
                  }))}
                />
                <StatusList
                  title="Connectors"
                  items={(diagnostics.data?.connectors ?? []).slice(0, 12).map((connector) => ({
                    key: connector.id,
                    title: connector.name,
                    subtitle: `${connector.provider} · ${connector.connectionCount} connection(s)`,
                    status: connector.enabled ? connector.lastSyncStatus ?? "Enabled" : "Disabled"
                  }))}
                />
              </div>
            </div>
          )}
        </Panel>

        <div className="space-y-5">
          <Panel>
            <SectionHeader title="Backup / Restore" description="Export and import user-owned daily-use data." />
            <div className="space-y-3 p-4">
              <button
                type="button"
                onClick={downloadBackup}
                className="inline-flex h-10 w-full items-center justify-center gap-2 rounded-md bg-accent px-4 text-sm font-medium text-white hover:bg-accent-strong"
              >
                <Download className="h-4 w-4" />
                Export backup
              </button>
              <label className="inline-flex h-10 w-full cursor-pointer items-center justify-center gap-2 rounded-md border border-line px-4 text-sm font-medium text-ink-muted hover:bg-surface-muted hover:text-ink">
                <FileUp className="h-4 w-4" />
                Restore backup
                <input type="file" accept="application/json" onChange={restoreBackup} className="hidden" />
              </label>
              {restoreMessage ? <p className="text-sm text-ink-muted">{restoreMessage}</p> : null}
            </div>
          </Panel>

          <Panel>
            <SectionHeader title="Smoke Test" description="Script status and the recommended local command." />
            {smoke.isLoading ? (
              <EmptyState text="Loading smoke status" />
            ) : smoke.isError ? (
              <EmptyState text={(smoke.error as Error).message} />
            ) : (
              <div className="space-y-3 p-4">
                <div className="flex items-center justify-between gap-3">
                  <span className="text-sm font-medium">{smoke.data?.status}</span>
                  <StatusBadge value={smoke.data?.scriptFound ? "Ready" : "Missing"} />
                </div>
                <div className="rounded-md border border-line bg-surface p-3 font-mono text-xs text-ink-muted">
                  {smoke.data?.recommendedCommand}
                </div>
                <p className="text-sm text-ink-muted">{smoke.data?.notes}</p>
              </div>
            )}
          </Panel>
        </div>
      </div>

      <Panel>
        <SectionHeader title="Recent Logs" description="Audit events and recent failed worker/connector runs." />
        {logs.isLoading ? (
          <EmptyState text="Loading logs" />
        ) : logs.isError ? (
          <EmptyState text={(logs.error as Error).message} />
        ) : logs.data?.length ? (
          <div className="divide-y divide-line">
            {logs.data.map((entry, index) => (
              <div key={`${entry.timestampUtc}-${index}`} className="grid gap-2 p-4 md:grid-cols-[180px_120px_1fr]">
                <span className="text-xs text-ink-muted">{new Date(entry.timestampUtc).toLocaleString()}</span>
                <Badge>{entry.source}</Badge>
                <div>
                  <div className="text-sm font-medium">{entry.level}</div>
                  <p className="mt-1 text-sm text-ink-muted">{entry.message}</p>
                  {entry.error ? <p className="mt-1 text-sm text-rose-600 dark:text-rose-300">{entry.error}</p> : null}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <EmptyState text="No logs yet" />
        )}
      </Panel>
    </div>
  );
}

function Metric({
  label,
  value,
  tone = "neutral"
}: {
  label: string;
  value: string | number;
  tone?: "neutral" | "good" | "warn" | "critical";
}) {
  return (
    <div className="rounded-md border border-line bg-surface p-3">
      <div className="mb-2 text-xs text-ink-muted">{label}</div>
      <Badge tone={tone}>{value}</Badge>
    </div>
  );
}

function Info({
  icon: Icon,
  label,
  value
}: {
  icon: typeof Server;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-center gap-3 rounded-md border border-line bg-surface p-3">
      <Icon className="h-4 w-4 text-accent" />
      <div>
        <div className="text-xs text-ink-muted">{label}</div>
        <div className="text-sm font-medium">{value}</div>
      </div>
    </div>
  );
}

function StatusList({
  title,
  items
}: {
  title: string;
  items: { key: string; title: string; subtitle: string; status: string }[];
}) {
  return (
    <div className="rounded-md border border-line">
      <div className="border-b border-line px-3 py-2 text-sm font-medium">{title}</div>
      <div className="divide-y divide-line">
        {items.map((item) => (
          <div key={item.key} className="flex items-center justify-between gap-3 px-3 py-2">
            <div className="min-w-0">
              <div className="truncate text-sm font-medium">{item.title}</div>
              <div className="truncate text-xs text-ink-muted">{item.subtitle}</div>
            </div>
            <StatusBadge value={item.status} />
          </div>
        ))}
      </div>
    </div>
  );
}
