"use client";

import { DataPage, StatusBadge } from "@/components/data-page";

export default function OpenLoopsPage() {
  return (
    <DataPage
      title="Open Loops"
      description="Open and waiting loops tracked by the Chief Of Staff layer."
      endpoint="/api/open-loops"
      columns={[
        { key: "title", label: "Loop" },
        { key: "status", label: "Status", render: (item) => <StatusBadge value={item.status} /> },
        { key: "description", label: "Description" },
        { key: "createdUtc", label: "Created" },
        { key: "closedUtc", label: "Closed" }
      ]}
    />
  );
}
