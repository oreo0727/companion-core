"use client";

import { DataPage, StatusBadge } from "@/components/data-page";

export default function GoalsPage() {
  return (
    <DataPage
      title="Goals"
      description="Active and historical user goals."
      endpoint="/api/goals"
      columns={[
        { key: "title", label: "Goal" },
        { key: "status", label: "Status", render: (item) => <StatusBadge value={item.status} /> },
        { key: "priority", label: "Priority", render: (item) => <StatusBadge value={item.priority} /> },
        { key: "targetDateUtc", label: "Target" },
        { key: "updatedUtc", label: "Updated" }
      ]}
    />
  );
}
