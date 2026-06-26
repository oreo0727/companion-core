"use client";

import { useQuery } from "@tanstack/react-query";
import type { ComponentType } from "react";
import { AlertCircle, Bell, CheckSquare, FolderKanban, Goal, Timer } from "lucide-react";
import { apiFetch } from "@/lib/api";
import { Badge, EmptyState, Panel, SectionHeader } from "@/components/ui";

type Dashboard = {
  activeProjects: number;
  activeGoals: number;
  openLoops: number;
  pendingApprovals: number;
  unreadNotifications: number;
  upcomingReminders: number;
  topInsights: { category: string; message: string; priority: number }[];
};

type Briefing = {
  overdueTasks: { id: string; title: string; dueDateUtc?: string }[];
  upcomingReminders: { id: string; title: string; dueUtc: string; status: string }[];
  pendingApprovals: { id: string; type: string; reason: string; riskLevel: string }[];
};

export default function DashboardPage() {
  const dashboard = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => apiFetch<Dashboard>("/api/companion/dashboard")
  });
  const briefing = useQuery({
    queryKey: ["briefing"],
    queryFn: () => apiFetch<Briefing>("/api/companion/briefing")
  });

  const data = dashboard.data;

  return (
    <div className="space-y-5">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-6">
        <Metric icon={FolderKanban} label="Projects" value={data?.activeProjects} />
        <Metric icon={Goal} label="Goals" value={data?.activeGoals} />
        <Metric icon={AlertCircle} label="Open loops" value={data?.openLoops} />
        <Metric icon={CheckSquare} label="Approvals" value={data?.pendingApprovals} />
        <Metric icon={Bell} label="Unread" value={data?.unreadNotifications} />
        <Metric icon={Timer} label="Reminders" value={data?.upcomingReminders} />
      </div>

      <div className="grid gap-5 xl:grid-cols-[1.2fr_0.8fr]">
        <Panel>
          <SectionHeader title="Chief Of Staff Insights" description="Prioritized signals from the current platform state." />
          {dashboard.isLoading ? (
            <EmptyState text="Loading insights" />
          ) : dashboard.isError ? (
            <EmptyState text={(dashboard.error as Error).message} />
          ) : data?.topInsights.length ? (
            <div className="divide-y divide-line">
              {data.topInsights.map((insight, index) => (
                <div key={`${insight.message}-${index}`} className="p-4">
                  <div className="mb-2 flex items-center justify-between gap-3">
                    <Badge tone={insight.priority >= 85 ? "warn" : "neutral"}>{insight.category}</Badge>
                    <span className="text-xs text-ink-muted">Priority {insight.priority}</span>
                  </div>
                  <p className="text-sm leading-6">{insight.message}</p>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState text="No insights yet" />
          )}
        </Panel>

        <Panel>
          <SectionHeader title="Needs Attention" description="Overdue work, reminders, and approvals." />
          <div className="divide-y divide-line">
            <AttentionList
              title="Overdue tasks"
              items={briefing.data?.overdueTasks.map((x) => x.title) ?? []}
            />
            <AttentionList
              title="Upcoming reminders"
              items={briefing.data?.upcomingReminders.map((x) => x.title) ?? []}
            />
            <AttentionList
              title="Pending approvals"
              items={briefing.data?.pendingApprovals.map((x) => `${x.type}: ${x.reason}`) ?? []}
            />
          </div>
        </Panel>
      </div>
    </div>
  );
}

function Metric({
  icon: Icon,
  label,
  value
}: {
  icon: ComponentType<{ className?: string }>;
  label: string;
  value?: number;
}) {
  return (
    <Panel className="p-4">
      <div className="flex items-center justify-between">
        <span className="text-sm text-ink-muted">{label}</span>
        <Icon className="h-4 w-4 text-accent" />
      </div>
      <div className="mt-3 text-2xl font-semibold">{value ?? "-"}</div>
    </Panel>
  );
}

function AttentionList({ title, items }: { title: string; items: string[] }) {
  return (
    <div className="p-4">
      <h3 className="mb-2 text-sm font-medium">{title}</h3>
      {items.length ? (
        <ul className="space-y-2">
          {items.slice(0, 4).map((item) => (
            <li key={item} className="line-clamp-2 text-sm text-ink-muted">
              {item}
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-ink-muted">Nothing waiting.</p>
      )}
    </div>
  );
}
