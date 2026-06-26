"use client";

import { FormEvent, useState } from "react";
import { Bot, LogIn } from "lucide-react";
import { useRouter } from "next/navigation";
import { login, setSession } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("local.user@companion-core.local");
  const [password, setPassword] = useState("CompanionDev123!");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const session = await login(email, password);
      setSession(session.accessToken, session.me);
      router.replace("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="grid min-h-screen bg-surface text-ink lg:grid-cols-[1fr_420px]">
      <section className="hidden border-r border-line bg-surface-muted p-10 lg:flex lg:flex-col lg:justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-md bg-accent text-white">
            <Bot className="h-6 w-6" />
          </div>
          <div>
            <div className="font-semibold">Companion Core</div>
            <div className="text-sm text-ink-muted">Admin Web Application</div>
          </div>
        </div>
        <div className="max-w-2xl">
          <h1 className="text-4xl font-semibold leading-tight">
            Operate the companion platform from one focused console.
          </h1>
          <p className="mt-4 text-base leading-7 text-ink-muted">
            Chat, review approvals, monitor tools, inspect memory, manage
            connectors, and keep notification flow visible without leaving the
            trusted backend boundary.
          </p>
        </div>
        <div className="text-sm text-ink-muted">JWT secured. Audit aware. Built for the platform phases ahead.</div>
      </section>
      <section className="flex items-center justify-center p-5">
        <form
          onSubmit={submit}
          className="w-full max-w-sm rounded-md border border-line bg-surface-raised p-6 shadow-soft"
        >
          <div className="mb-6">
            <div className="mb-4 flex h-10 w-10 items-center justify-center rounded-md bg-accent text-white lg:hidden">
              <Bot className="h-5 w-5" />
            </div>
            <h2 className="text-xl font-semibold">Sign in</h2>
            <p className="mt-1 text-sm text-ink-muted">
              Use the local admin account or any registered Companion account.
            </p>
          </div>
          <label className="mb-4 block">
            <span className="mb-1 block text-sm font-medium">Email</span>
            <input
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm outline-none focus:border-accent"
              type="email"
              autoComplete="email"
            />
          </label>
          <label className="mb-5 block">
            <span className="mb-1 block text-sm font-medium">Password</span>
            <input
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="h-10 w-full rounded-md border border-line bg-surface px-3 text-sm outline-none focus:border-accent"
              type="password"
              autoComplete="current-password"
            />
          </label>
          {error ? (
            <div className="mb-4 rounded-md border border-rose-500/30 bg-rose-500/10 px-3 py-2 text-sm text-rose-600 dark:text-rose-300">
              {error}
            </div>
          ) : null}
          <button
            type="submit"
            disabled={loading}
            className="flex h-10 w-full items-center justify-center gap-2 rounded-md bg-accent px-4 text-sm font-medium text-white hover:bg-accent-strong disabled:opacity-60"
          >
            <LogIn className="h-4 w-4" />
            {loading ? "Signing in" : "Sign in"}
          </button>
        </form>
      </section>
    </main>
  );
}
