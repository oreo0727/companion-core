"use client";

import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Bot, CheckCircle2, LogIn, UserPlus } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { apiFetch, register, setSession } from "@/lib/api";
import { Badge, EmptyState, Panel, SectionHeader } from "@/components/ui";

type SetupStatus = {
  isFirstRun: boolean;
  userCount: number;
  hasAdministrator: boolean;
  environmentName: string;
  apiBaseUrl: string;
  webBaseUrl: string;
  seededLocalAdminEmail: string;
  checks: { key: string; status: string; message: string }[];
};

export default function SetupPage() {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const status = useQuery({
    queryKey: ["setup-status"],
    queryFn: () => apiFetch<SetupStatus>("/api/setup/status")
  });

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!status.data?.isFirstRun) {
      return;
    }

    setLoading(true);
    setError(null);

    const data = new FormData(event.currentTarget);
    try {
      const session = await register(
        String(data.get("email") ?? ""),
        String(data.get("displayName") ?? ""),
        String(data.get("password") ?? "")
      );
      setSession(session.accessToken, session.me);
      router.replace("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Registration failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="min-h-screen bg-surface px-4 py-6 text-ink sm:px-6">
      <div className="mx-auto max-w-5xl space-y-5">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-md bg-accent text-white">
              <Bot className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-semibold">Companion setup</h1>
              <p className="text-sm text-ink-muted">First-run readiness and local daily-use checks.</p>
            </div>
          </div>
          <Link
            href="/login"
            className="inline-flex h-9 items-center gap-2 rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink"
          >
            <LogIn className="h-4 w-4" />
            Sign in
          </Link>
        </div>

        {status.isLoading ? (
          <Panel>
            <EmptyState text="Checking setup status" />
          </Panel>
        ) : status.isError ? (
          <Panel>
            <EmptyState text={(status.error as Error).message} />
          </Panel>
        ) : (
          <div className="grid gap-5 lg:grid-cols-[1fr_360px]">
            <Panel>
              <SectionHeader
                title={status.data?.isFirstRun ? "Create the first account" : "Setup is ready"}
                description={`API ${status.data?.apiBaseUrl} · ${status.data?.environmentName}`}
              />
              <div className="divide-y divide-line">
                {(status.data?.checks ?? []).map((check) => (
                  <div key={check.key} className="flex items-start gap-3 p-4">
                    <CheckCircle2 className="mt-0.5 h-4 w-4 text-accent" />
                    <div className="min-w-0 flex-1">
                      <div className="mb-1 flex flex-wrap items-center gap-2">
                        <span className="font-medium capitalize">{check.key}</span>
                        <Badge tone={check.status === "Ready" ? "good" : check.status === "Warning" ? "warn" : "critical"}>
                          {check.status}
                        </Badge>
                      </div>
                      <p className="text-sm text-ink-muted">{check.message}</p>
                    </div>
                  </div>
                ))}
              </div>
              <div className="border-t border-line p-4 text-sm text-ink-muted">
                Existing users: {status.data?.userCount ?? 0}. Seeded local administrator:{" "}
                <span className="font-medium text-ink">{status.data?.seededLocalAdminEmail}</span>.
              </div>
            </Panel>

            <Panel>
              <SectionHeader title="Account" description="Register when the database has no users." />
              <form onSubmit={submit} className="space-y-4 p-4">
                <label className="block">
                  <span className="mb-1 block text-sm font-medium">Display name</span>
                  <input
                    name="displayName"
                    placeholder="Your name"
                    className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm outline-none focus:border-accent"
                  />
                </label>
                <label className="block">
                  <span className="mb-1 block text-sm font-medium">Email</span>
                  <input
                    name="email"
                    type="email"
                    placeholder="you@example.com"
                    className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm outline-none focus:border-accent"
                  />
                </label>
                <label className="block">
                  <span className="mb-1 block text-sm font-medium">Password</span>
                  <input
                    name="password"
                    type="password"
                    placeholder="At least 8 characters"
                    autoComplete="new-password"
                    className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm outline-none focus:border-accent"
                  />
                </label>
                {error ? (
                  <div className="rounded-md border border-rose-500/30 bg-rose-500/10 px-3 py-2 text-sm text-rose-600 dark:text-rose-300">
                    {error}
                  </div>
                ) : null}
                <button
                  type="submit"
                  disabled={loading || !status.data?.isFirstRun}
                  className="inline-flex h-10 w-full items-center justify-center gap-2 rounded-md bg-accent px-4 text-sm font-medium text-white hover:bg-accent-strong disabled:opacity-60"
                >
                  <UserPlus className="h-4 w-4" />
                  {status.data?.isFirstRun ? (loading ? "Creating account" : "Create account") : "Account already exists"}
                </button>
                {!status.data?.isFirstRun ? (
                  <Link
                    href="/login"
                    className="inline-flex h-10 w-full items-center justify-center gap-2 rounded-md border border-line px-4 text-sm font-medium text-ink-muted hover:bg-surface-muted hover:text-ink"
                  >
                    <LogIn className="h-4 w-4" />
                    Continue to login
                  </Link>
                ) : null}
              </form>
            </Panel>
          </div>
        )}
      </div>
    </main>
  );
}
