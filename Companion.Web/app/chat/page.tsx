"use client";

import { FormEvent, useState } from "react";
import ReactMarkdown from "react-markdown";
import { Send, Sparkles } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { Badge, Panel, SectionHeader } from "@/components/ui";

type ChatMessage = {
  role: "user" | "assistant";
  content: string;
  provider?: string | null;
  model?: string | null;
  fallback?: boolean;
};

type ChatResponse = {
  conversationId: string;
  reply: string;
  provider?: string | null;
  model?: string | null;
  usedFallback: boolean;
  generatedInsights: { category: string; message: string; priority: number }[];
  toolExecutions: unknown[];
};

export default function ChatPage() {
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(false);

  async function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const message = input.trim();
    if (!message || loading) {
      return;
    }

    setInput("");
    setMessages((current) => [...current, { role: "user", content: message }]);
    setLoading(true);

    try {
      const response = await apiFetch<ChatResponse>("/api/chat", {
        method: "POST",
        body: JSON.stringify({ message, conversationId })
      });
      setConversationId(response.conversationId);
      setMessages((current) => [
        ...current,
        {
          role: "assistant",
          content: response.reply,
          provider: response.provider,
          model: response.model,
          fallback: response.usedFallback
        }
      ]);
    } finally {
      setLoading(false);
    }
  }

  return (
    <Panel className="flex min-h-[calc(100vh-12rem)] flex-col">
      <SectionHeader
        title="Chat"
        description="Streaming-ready conversation surface backed by the existing Chat V2 API."
        action={<Badge tone="neutral">HTTP now, stream later</Badge>}
      />
      <div className="flex-1 space-y-4 overflow-y-auto p-4">
        {messages.length === 0 ? (
          <div className="flex h-full min-h-80 flex-col items-center justify-center text-center text-ink-muted">
            <Sparkles className="mb-3 h-8 w-8 text-accent" />
            <p className="max-w-md text-sm leading-6">
              Ask Companion about your briefing, tasks, approvals, calendar,
              email, knowledge, or reminders.
            </p>
          </div>
        ) : (
          messages.map((message, index) => (
            <article
              key={`${message.role}-${index}`}
              className={
                message.role === "user"
                  ? "ml-auto max-w-3xl rounded-md bg-accent px-4 py-3 text-sm text-white"
                  : "max-w-3xl rounded-md border border-line bg-surface px-4 py-3 text-sm"
              }
            >
              <ReactMarkdown className="prose prose-sm max-w-none dark:prose-invert">
                {message.content}
              </ReactMarkdown>
              {message.role === "assistant" ? (
                <div className="mt-3 flex flex-wrap gap-2">
                  {message.provider ? <Badge>{message.provider}</Badge> : null}
                  {message.model ? <Badge>{message.model}</Badge> : null}
                  {message.fallback ? <Badge tone="warn">Fallback</Badge> : null}
                </div>
              ) : null}
            </article>
          ))
        )}
      </div>
      <form onSubmit={send} className="border-t border-line p-4">
        <div className="flex gap-3">
          <textarea
            value={input}
            onChange={(event) => setInput(event.target.value)}
            rows={2}
            className="min-h-12 flex-1 resize-none rounded-md border border-line bg-surface px-3 py-2 text-sm outline-none focus:border-accent"
            placeholder="Message Companion"
          />
          <button
            type="submit"
            disabled={loading || !input.trim()}
            className="flex h-12 w-12 shrink-0 items-center justify-center rounded-md bg-accent text-white hover:bg-accent-strong disabled:opacity-50"
            title="Send"
          >
            <Send className="h-5 w-5" />
          </button>
        </div>
      </form>
    </Panel>
  );
}
