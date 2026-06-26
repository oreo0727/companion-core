"use client";

import { DataPage, StatusBadge } from "@/components/data-page";

export default function ProjectsPage() {
  return (
    <DataPage
      title="Projects"
      description="Project portfolio and planning status."
      endpoint="/api/projects"
      columns={[
        { key: "title", label: "Project" },
        { key: "status", label: "Status", render: (item) => <StatusBadge value={item.status} /> },
        { key: "priority", label: "Priority", render: (item) => <StatusBadge value={item.priority} /> },
        { key: "description", label: "Description" },
        { key: "updatedUtc", label: "Updated" }
      ]}
    />
  );
}
