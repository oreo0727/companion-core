"use client";

import { FormEvent, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Save, TestTube2 } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { Badge, EmptyState, Panel, SectionHeader } from "@/components/ui";
import { StatusBadge } from "@/components/data-page";

type AiProvider = {
  id: string;
  provider: string;
  model: string;
  apiBaseUrl: string;
  isEnabled: boolean;
  temperature: number;
  maxTokens: number;
  timeoutSeconds: number;
  hasApiKey: boolean;
  updatedUtc: string;
};

export default function AiSettingsPage() {
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<AiProvider | null>(null);
  const [testResult, setTestResult] = useState<string | null>(null);
  const providers = useQuery({
    queryKey: ["ai-settings"],
    queryFn: () => apiFetch<AiProvider[]>("/api/settings/ai")
  });
  const mutation = useMutation({
    mutationFn: (payload: AiProvider & { apiKey?: string }) =>
      apiFetch<AiProvider>("/api/settings/ai", {
        method: "PUT",
        body: JSON.stringify(payload)
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ai-settings"] });
      setSelected(null);
    }
  });
  const testMutation = useMutation({
    mutationFn: (provider: string) =>
      apiFetch<{
        provider: string;
        model?: string;
        status: string;
        latencyMs?: number;
        reply?: string;
        error?: string;
      }>(`/api/settings/ai/${encodeURIComponent(provider)}/test`, {
        method: "POST"
      }),
    onSuccess: (result) => {
      setTestResult(
        result.status === "Succeeded"
          ? `${result.provider} responded in ${result.latencyMs ?? 0}ms: ${result.reply ?? "ok"}`
          : `${result.provider} failed: ${result.error ?? "unknown error"}`
      );
    },
    onError: (error) => setTestResult(error instanceof Error ? error.message : "Provider test failed")
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) {
      return;
    }

    const data = new FormData(event.currentTarget);
    mutation.mutate({
      ...selected,
      model: String(data.get("model") ?? selected.model),
      apiBaseUrl: String(data.get("apiBaseUrl") ?? selected.apiBaseUrl),
      apiKey: String(data.get("apiKey") ?? ""),
      isEnabled: data.get("isEnabled") === "on",
      temperature: Number(data.get("temperature") ?? selected.temperature),
      maxTokens: Number(data.get("maxTokens") ?? selected.maxTokens),
      timeoutSeconds: Number(data.get("timeoutSeconds") ?? selected.timeoutSeconds)
    });
  }

  return (
    <div className="grid gap-5 xl:grid-cols-[1fr_420px]">
      <Panel>
        <SectionHeader title="AI Settings" description="Provider configuration for reasoning and extraction." />
        {providers.isLoading ? (
          <EmptyState text="Loading providers" />
        ) : providers.isError ? (
          <EmptyState text={(providers.error as Error).message} />
        ) : (
          <div className="divide-y divide-line">
            {(providers.data ?? []).map((provider) => (
              <button
                key={provider.id}
                type="button"
                onClick={() => {
                  setSelected(provider);
                  setTestResult(null);
                }}
                className="flex w-full flex-col gap-3 px-4 py-4 text-left hover:bg-surface-muted sm:flex-row sm:items-center sm:justify-between"
              >
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-medium">{provider.provider}</span>
                    <StatusBadge value={provider.isEnabled ? "Enabled" : "Disabled"} />
                    {provider.hasApiKey ? <Badge>API key stored</Badge> : null}
                  </div>
                  <div className="mt-1 text-sm text-ink-muted">
                    {provider.model} at {provider.apiBaseUrl}
                  </div>
                </div>
                <span className="text-sm text-ink-muted">{new Date(provider.updatedUtc).toLocaleString()}</span>
              </button>
            ))}
          </div>
        )}
      </Panel>

      <Panel>
        <SectionHeader title="Edit Provider" description="Select a provider to update runtime settings." />
        {selected ? (
          <form onSubmit={submit} className="space-y-4 p-4">
            <Field name="provider" label="Provider" value={selected.provider} readOnly />
            <Field name="model" label="Model" value={selected.model} />
            <Field name="apiBaseUrl" label="API base URL" value={selected.apiBaseUrl} />
            <Field name="apiKey" label="API key" value="" placeholder="Leave blank to keep existing key" />
            <label className="flex items-center gap-2 text-sm">
              <input name="isEnabled" type="checkbox" defaultChecked={selected.isEnabled} />
              Enabled
            </label>
            <div className="grid grid-cols-3 gap-3">
              <Field name="temperature" label="Temp" value={String(selected.temperature)} type="number" step="0.01" />
              <Field name="maxTokens" label="Tokens" value={String(selected.maxTokens)} type="number" />
              <Field name="timeoutSeconds" label="Timeout" value={String(selected.timeoutSeconds)} type="number" />
            </div>
            {mutation.isError ? (
              <p className="rounded-md border border-rose-500/30 bg-rose-500/10 px-3 py-2 text-sm text-rose-600 dark:text-rose-300">
                {(mutation.error as Error).message}
              </p>
            ) : null}
            {testResult ? (
              <p className="rounded-md border border-line bg-surface px-3 py-2 text-sm text-ink-muted">
                {testResult}
              </p>
            ) : null}
            <div className="flex flex-wrap gap-2">
              <button
                type="submit"
                className="inline-flex h-10 items-center gap-2 rounded-md bg-accent px-4 text-sm font-medium text-white hover:bg-accent-strong"
              >
                <Save className="h-4 w-4" />
                Save
              </button>
              <button
                type="button"
                onClick={() => testMutation.mutate(selected.provider)}
                disabled={testMutation.isPending}
                className="inline-flex h-10 items-center gap-2 rounded-md border border-line px-4 text-sm font-medium text-ink-muted hover:bg-surface-muted hover:text-ink disabled:opacity-60"
              >
                <TestTube2 className="h-4 w-4" />
                {testMutation.isPending ? "Testing" : "Test"}
              </button>
            </div>
          </form>
        ) : (
          <EmptyState text="Select a provider" />
        )}
      </Panel>
    </div>
  );
}

function Field({
  name,
  label,
  value,
  readOnly,
  placeholder,
  type = "text",
  step
}: {
  name: string;
  label: string;
  value: string;
  readOnly?: boolean;
  placeholder?: string;
  type?: string;
  step?: string;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium">{label}</span>
      <input
        name={name}
        defaultValue={value}
        readOnly={readOnly}
        placeholder={placeholder}
        type={type}
        step={step}
        className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm outline-none focus:border-accent read-only:text-ink-muted"
      />
    </label>
  );
}
