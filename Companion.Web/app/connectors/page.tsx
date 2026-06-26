"use client";

import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Plug, RefreshCw, Search } from "lucide-react";
import { apiFetch, type JsonRecord } from "@/lib/api";
import { Badge, EmptyState, Panel, SectionHeader } from "@/components/ui";
import { StatusBadge } from "@/components/data-page";

type ConnectorEntry = {
  definition: {
    id: string;
    name: string;
    provider: string;
    description: string;
    category: string;
    supportsOAuth: boolean;
    riskLevel: string;
    enabled: boolean;
  };
  connections: JsonRecord[];
};

export default function ConnectorsPage() {
  const [query, setQuery] = useState("");
  const result = useQuery({
    queryKey: ["connectors"],
    queryFn: () => apiFetch<ConnectorEntry[]>("/api/connectors")
  });

  const entries = useMemo(() => result.data ?? [], [result.data]);
  const filtered = useMemo(() => {
    const needle = query.trim().toLowerCase();
    if (!needle) {
      return entries;
    }

    return entries.filter((entry) =>
      `${entry.definition.name} ${entry.definition.provider} ${entry.definition.category}`
        .toLowerCase()
        .includes(needle)
    );
  }, [entries, query]);

  return (
    <Panel>
      <SectionHeader
        title="Connectors"
        description="Read-only connector definitions and user connections."
        action={
          <button
            onClick={() => result.refetch()}
            className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink"
          >
            <RefreshCw className="h-4 w-4" />
            Refresh
          </button>
        }
      />
      <div className="border-b border-line p-4">
        <label className="relative block max-w-sm">
          <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-ink-muted" />
          <input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            className="h-9 w-full rounded-md border border-line bg-surface px-9 text-sm outline-none focus:border-accent"
            placeholder="Search connectors"
          />
        </label>
      </div>
      {result.isLoading ? (
        <EmptyState text="Loading connectors" />
      ) : result.isError ? (
        <EmptyState text={(result.error as Error).message} />
      ) : (
        <div className="grid gap-4 p-4 lg:grid-cols-2">
          {filtered.map((entry) => (
            <article key={entry.definition.id} className="rounded-md border border-line bg-surface p-4">
              <div className="mb-3 flex items-start justify-between gap-3">
                <div className="flex gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-md bg-accent/10 text-accent">
                    <Plug className="h-5 w-5" />
                  </div>
                  <div>
                    <h3 className="font-medium">{entry.definition.name}</h3>
                    <p className="text-sm text-ink-muted">{entry.definition.description}</p>
                  </div>
                </div>
                <StatusBadge value={entry.definition.enabled ? "Enabled" : "Disabled"} />
              </div>
              <div className="mb-4 flex flex-wrap gap-2">
                <Badge>{entry.definition.provider}</Badge>
                <Badge>{entry.definition.category}</Badge>
                <Badge tone={entry.definition.riskLevel === "Low" ? "good" : "warn"}>
                  {entry.definition.riskLevel}
                </Badge>
              </div>
              <div className="text-sm">
                <div className="mb-2 font-medium">Connections</div>
                {entry.connections.length ? (
                  <ul className="space-y-2">
                    {entry.connections.map((connection) => (
                      <li
                        key={String(connection.id)}
                        className="flex items-center justify-between gap-3 rounded-md bg-surface-muted px-3 py-2"
                      >
                        <span>{String(connection.displayName ?? "Connection")}</span>
                        <StatusBadge value={connection.status} />
                      </li>
                    ))}
                  </ul>
                ) : (
                  <p className="text-ink-muted">No connection for this user.</p>
                )}
              </div>
            </article>
          ))}
        </div>
      )}
    </Panel>
  );
}
