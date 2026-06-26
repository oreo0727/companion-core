"use client";

import { DataPage } from "@/components/data-page";

export default function AuditPage() {
  return (
    <DataPage
      title="Audit"
      description="Security and action audit trail."
      endpoint="/api/audit"
      columns={[
        { key: "eventType", label: "Event" },
        { key: "entityType", label: "Entity" },
        { key: "description", label: "Description" },
        { key: "createdUtc", label: "Created" }
      ]}
    />
  );
}
