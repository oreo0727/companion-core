"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { CheckCircle2, Loader2, XCircle } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { Panel, SectionHeader } from "@/components/ui";

type CallbackState = {
  displayName?: string;
  scopes?: string[];
};

type OAuthConnection = {
  connectionId: string;
  displayName: string;
  status: string;
};

export default function GoogleOAuthCallbackPage() {
  const [status, setStatus] = useState<"working" | "succeeded" | "failed">("working");
  const [message, setMessage] = useState("Completing Google connection.");

  useEffect(() => {
    let cancelled = false;

    async function completeAuthorization() {
      const searchParams = new URLSearchParams(window.location.search);
      const error = searchParams.get("error");
      const state = searchParams.get("state");
      const code = searchParams.get("code");

      if (error) {
        throw new Error(searchParams.get("error_description") ?? error);
      }

      if (!state || !code) {
        throw new Error("Google did not return the OAuth state and code.");
      }

      const storedRaw = window.localStorage.getItem(`companion.oauth.${state}`);
      const stored = parseStoredState(storedRaw);
      const scopeParam = searchParams.get("scope");
      const scopes = scopeParam?.split(" ").filter(Boolean) ?? stored?.scopes ?? [];

      const connection = await apiFetch<OAuthConnection>("/api/oauth/Google/callback", {
        method: "POST",
        body: JSON.stringify({
          state,
          code,
          displayName: stored?.displayName,
          scopes
        })
      });

      window.localStorage.removeItem(`companion.oauth.${state}`);
      if (!cancelled) {
        setStatus("succeeded");
        setMessage(`${connection.displayName} is ${connection.status.toLowerCase()}.`);
      }
    }

    completeAuthorization().catch((error: unknown) => {
      if (!cancelled) {
        setStatus("failed");
        setMessage(error instanceof Error ? error.message : "Google connection failed.");
      }
    });

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <Panel>
      <SectionHeader title="Google OAuth" description="Completing the Google account connection." />
      <div className="p-6">
        <div className="flex items-center gap-3 rounded-md border border-line bg-surface p-4">
          {status === "working" ? <Loader2 className="h-5 w-5 animate-spin text-accent" /> : null}
          {status === "succeeded" ? <CheckCircle2 className="h-5 w-5 text-emerald-500" /> : null}
          {status === "failed" ? <XCircle className="h-5 w-5 text-red-500" /> : null}
          <div>
            <h2 className="font-medium">{status === "working" ? "Connecting Google" : status === "succeeded" ? "Google connected" : "Connection failed"}</h2>
            <p className="mt-1 text-sm text-ink-muted">{message}</p>
          </div>
        </div>
        <Link
          href="/google-account"
          className="mt-4 inline-flex h-9 items-center rounded-md border border-line px-3 text-sm text-ink-muted hover:bg-surface-muted hover:text-ink"
        >
          Back to Google Account
        </Link>
      </div>
    </Panel>
  );
}

function parseStoredState(value: string | null) {
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as CallbackState;
  } catch {
    return null;
  }
}
