"use client";

import { DataPage, StatusBadge } from "@/components/data-page";

export default function MemoriesPage() {
  return (
    <DataPage
      title="Memories"
      description="Saved memory entries for the authenticated user."
      endpoint="/api/memories"
      columns={[
        { key: "summary", label: "Summary" },
        { key: "type", label: "Type", render: (item) => <StatusBadge value={item.type} /> },
        { key: "importance", label: "Importance" },
        { key: "source", label: "Source" },
        { key: "createdUtc", label: "Created" }
      ]}
    />
  );
}
