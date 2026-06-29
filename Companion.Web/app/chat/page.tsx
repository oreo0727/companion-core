"use client";

import { FormEvent, KeyboardEvent, useEffect, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import clsx from "clsx";
import { BrainCircuit, Send, Sparkles } from "lucide-react";
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
  const [error, setError] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [messages, loading]);

  async function send(event?: FormEvent<HTMLFormElement>) {
    event?.preventDefault();
    const message = input.trim();
    if (!message || loading) {
      return;
    }

    setInput("");
    setError(null);
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
    } catch (err) {
      setError(err instanceof Error ? err.message : "Message failed");
    } finally {
      setLoading(false);
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key !== "Enter" || event.shiftKey || event.nativeEvent.isComposing) {
      return;
    }

    event.preventDefault();
    send();
  }

  return (
    <Panel className="scanline flex min-h-[calc(100vh-12rem)] flex-col overflow-hidden">
      <SectionHeader
        title="Chat"
        description="Conversation surface backed by memory, tools, approvals, and provider fallback."
        action={<Badge tone={loading ? "warn" : "good"}>{loading ? "Thinking" : "Ready"}</Badge>}
      />
      <div className="flex-1 space-y-4 overflow-y-auto p-4">
        {messages.length === 0 ? (
          <div className="flex h-full min-h-80 flex-col items-center justify-center text-center text-ink-muted">
            <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-md border border-accent/35 bg-accent/10 text-accent shadow-[0_0_36px_rgb(var(--panel-glow)/0.2)]">
              <Sparkles className="h-7 w-7" />
            </div>
            <p className="max-w-md text-sm leading-6">
              Ask Companion about your briefing, tasks, approvals, calendar,
              email, knowledge, or reminders.
            </p>
          </div>
        ) : (
          messages.map((message, index) => (
            <ChatBubble key={`${message.role}-${index}`} message={message} />
          ))
        )}
        {loading ? <ThinkingBubble /> : null}
        {error ? (
          <div className="max-w-3xl rounded-md border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-700 dark:text-rose-300">
            {error}
          </div>
        ) : null}
        <div ref={messagesEndRef} />
      </div>
      <form onSubmit={send} className="border-t border-line p-4">
        <div className="flex items-end gap-3 rounded-md border border-line/80 bg-surface/70 p-2 backdrop-blur">
          <textarea
            value={input}
            onChange={(event) => setInput(event.target.value)}
            onKeyDown={handleKeyDown}
            rows={2}
            className="min-h-12 flex-1 resize-none rounded-md border border-transparent bg-transparent px-3 py-2 text-sm outline-none focus:border-accent/50"
            placeholder="Message Companion"
          />
          <button
            type="submit"
            disabled={loading || !input.trim()}
            className="flex h-12 w-12 shrink-0 items-center justify-center rounded-md border border-accent/40 bg-accent text-white shadow-[0_0_24px_rgb(var(--panel-glow)/0.22)] hover:bg-accent-strong disabled:opacity-50"
            title="Send"
          >
            <Send className="h-5 w-5" />
          </button>
        </div>
        <div className="mt-2 text-xs text-ink-muted">Enter sends. Shift+Enter adds a new line.</div>
      </form>
    </Panel>
  );
}

function ChatBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === "user";

  return (
    <article
      className={clsx(
        "max-w-3xl rounded-md px-4 py-3 text-sm shadow-soft",
        isUser
          ? "ml-auto border border-accent/40 bg-accent/90 text-white"
          : "border border-line/80 bg-surface-raised/90 backdrop-blur"
      )}
    >
      <ReactMarkdown className={clsx("prose prose-sm max-w-none", isUser ? "prose-invert" : "dark:prose-invert")}>
        {message.content}
      </ReactMarkdown>
      {!isUser ? (
        <div className="mt-3 flex flex-wrap gap-2">
          {message.provider ? <Badge>{message.provider}</Badge> : null}
          {message.model ? <Badge>{message.model}</Badge> : null}
          {message.fallback ? <Badge tone="warn">Fallback</Badge> : null}
        </div>
      ) : null}
    </article>
  );
}

function ThinkingBubble() {
  return (
    <article className="max-w-3xl rounded-md border border-line/80 bg-surface-raised/90 px-4 py-3 text-sm shadow-soft backdrop-blur">
      <div className="flex items-center gap-3 text-ink-muted">
        <div className="flex h-8 w-8 items-center justify-center rounded-md border border-accent/30 bg-accent/10 text-accent">
          <BrainCircuit className="h-4 w-4" />
        </div>
        <div className="flex items-center gap-1.5">
          <span className="typing-dot h-2 w-2 rounded-full bg-accent" />
          <span className="typing-dot h-2 w-2 rounded-full bg-accent" />
          <span className="typing-dot h-2 w-2 rounded-full bg-accent" />
        </div>
      </div>
    </article>
  );
}
