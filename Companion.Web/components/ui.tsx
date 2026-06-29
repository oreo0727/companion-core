import clsx from "clsx";
import type { ReactNode } from "react";

export function Panel({
  children,
  className
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <section
      className={clsx(
        "rounded-md border border-line/80 bg-surface-raised/90 shadow-soft backdrop-blur",
        className
      )}
    >
      {children}
    </section>
  );
}

export function SectionHeader({
  title,
  description,
  action
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-3 border-b border-line px-4 py-4 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h2 className="text-base font-semibold">{title}</h2>
        {description ? (
          <p className="mt-1 text-sm text-ink-muted">{description}</p>
        ) : null}
      </div>
      {action}
    </div>
  );
}

export function Badge({
  children,
  tone = "neutral"
}: {
  children: ReactNode;
  tone?: "neutral" | "good" | "warn" | "critical";
}) {
  const styles = {
    neutral: "border-line bg-surface-muted text-ink-muted",
    good: "border-emerald-500/30 bg-emerald-500/10 text-emerald-600 dark:text-emerald-300",
    warn: "border-amber-500/30 bg-amber-500/10 text-amber-700 dark:text-amber-300",
    critical: "border-rose-500/30 bg-rose-500/10 text-rose-700 dark:text-rose-300"
  };

  return (
    <span
      className={clsx(
        "inline-flex items-center rounded-md border px-2 py-0.5 text-xs font-medium",
        styles[tone]
      )}
    >
      {children}
    </span>
  );
}

export function EmptyState({ text }: { text: string }) {
  return <div className="px-4 py-10 text-center text-sm text-ink-muted">{text}</div>;
}
