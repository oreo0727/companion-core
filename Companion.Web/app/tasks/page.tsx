"use client";

import { DataPage, StatusBadge } from "@/components/data-page";

export default function TasksPage() {
  return (
    <DataPage
      title="Tasks"
      description="Tracked work items, priorities, and due dates."
      endpoint="/api/tasks"
      columns={[
        { key: "title", label: "Task" },
        { key: "status", label: "Status", render: (item) => <StatusBadge value={item.status} /> },
        { key: "priority", label: "Priority", render: (item) => <StatusBadge value={item.priority} /> },
        { key: "dueDateUtc", label: "Due" },
        { key: "createdUtc", label: "Created" }
      ]}
    />
  );
}
