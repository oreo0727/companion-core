"use client";

import { useMemo, useState } from "react";
import type { ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight, RefreshCw, Search } from "lucide-react";
import { apiFetch, type PaginatedItem } from "@/lib/api";
import { Badge, EmptyState, Panel, SectionHeader } from "@/components/ui";

export type DataColumn = {
  key: string;
  label: string;
  render?: (item: PaginatedItem) => ReactNode;
};

export function DataPage({
  title,
  description,
  endpoint,
  columns,
  searchKeys = ["title", "name", "subject", "summary", "description", "type", "status"],
  pageSize = 12
}: {
  title: string;
  description: string;
  endpoint: string;
  columns: DataColumn[];
  searchKeys?: string[];
  pageSize?: number;
}) {
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const result = useQuery({
    queryKey: [endpoint],
    queryFn: () => apiFetch<PaginatedItem[]>(endpoint)
  });

  const items = useMemo(() => result.data ?? [], [result.data]);
  const filtered = useMemo(() => {
    const needle = query.trim().toLowerCase();
    if (!needle) {
      return items;
    }

    return items.filter((item) =>
      searchKeys.some((key) =>
        String(item[key] ?? "")
          .toLowerCase()
          .includes(needle)
      )
    );
  }, [items, query, searchKeys]);

  const pageCount = Math.max(Math.ceil(filtered.length / pageSize), 1);
  const safePage = Math.min(page, pageCount);
  const visible = filtered.slice((safePage - 1) * pageSize, safePage * pageSize);

  return (
    <Panel>
      <SectionHeader
        title={title}
        description={description}
        action={
          <button
            type="button"
            onClick={() => result.refetch()}
            className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink"
          >
            <RefreshCw className="h-4 w-4" />
            Refresh
          </button>
        }
      />
      <div className="flex flex-col gap-3 border-b border-line p-4 sm:flex-row sm:items-center sm:justify-between">
        <label className="relative w-full sm:max-w-sm">
          <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-ink-muted" />
          <input
            value={query}
            onChange={(event) => {
              setQuery(event.target.value);
              setPage(1);
            }}
            className="h-9 w-full rounded-md border border-line bg-surface px-9 text-sm outline-none focus:border-accent"
            placeholder={`Search ${title.toLowerCase()}`}
          />
        </label>
        <div className="text-sm text-ink-muted">
          {filtered.length} result{filtered.length === 1 ? "" : "s"}
        </div>
      </div>

      {result.isLoading ? (
        <EmptyState text="Loading data" />
      ) : result.isError ? (
        <EmptyState text={(result.error as Error).message} />
      ) : visible.length === 0 ? (
        <EmptyState text="No records found" />
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full table-fixed text-left text-sm">
            <thead className="bg-surface-muted text-xs uppercase text-ink-muted">
              <tr>
                {columns.map((column) => (
                  <th key={column.key} className="px-4 py-3 font-medium">
                    {column.label}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-line">
              {visible.map((item, index) => (
                <tr key={String(item.id ?? `${endpoint}-${index}`)}>
                  {columns.map((column) => (
                    <td key={column.key} className="max-w-xs px-4 py-3 align-top">
                      {column.render ? column.render(item) : formatValue(item[column.key])}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex items-center justify-between border-t border-line px-4 py-3 text-sm text-ink-muted">
        <span>
          Page {safePage} of {pageCount}
        </span>
        <div className="flex gap-2">
          <button
            type="button"
            disabled={safePage <= 1}
            onClick={() => setPage((value) => Math.max(value - 1, 1))}
            className="flex h-8 w-8 items-center justify-center rounded-md border border-line disabled:opacity-40"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <button
            type="button"
            disabled={safePage >= pageCount}
            onClick={() => setPage((value) => Math.min(value + 1, pageCount))}
            className="flex h-8 w-8 items-center justify-center rounded-md border border-line disabled:opacity-40"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>
    </Panel>
  );
}

export function StatusBadge({ value }: { value: unknown }) {
  const text = String(value ?? "Unknown");
  const lower = text.toLowerCase();
  const tone =
    lower.includes("failed") || lower.includes("rejected") || lower.includes("overdue")
      ? "critical"
      : lower.includes("pending") || lower.includes("waiting") || lower.includes("scheduled")
        ? "warn"
        : lower.includes("completed") || lower.includes("approved") || lower.includes("connected")
          ? "good"
          : "neutral";

  return <Badge tone={tone}>{text}</Badge>;
}

export function formatValue(value: unknown) {
  if (value === null || value === undefined || value === "") {
    return <span className="text-ink-muted">-</span>;
  }

  if (typeof value === "boolean") {
    return <StatusBadge value={value ? "Yes" : "No"} />;
  }

  if (typeof value === "string" && value.match(/^\d{4}-\d{2}-\d{2}T/)) {
    return new Date(value).toLocaleString();
  }

  if (typeof value === "object") {
    return (
      <code className="line-clamp-3 whitespace-pre-wrap rounded bg-surface-muted px-2 py-1 text-xs">
        {JSON.stringify(value)}
      </code>
    );
  }

  return <span className="line-clamp-3">{String(value)}</span>;
}
