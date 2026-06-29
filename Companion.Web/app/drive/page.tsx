"use client";

import { DataPage } from "@/components/data-page";

export default function DrivePage() {
  return (
    <DataPage
      title="Drive"
      description="Read-only file document snapshots from file capabilities."
      endpoint="/api/files/documents"
      columns={[
        { key: "name", label: "Name" },
        { key: "mimeType", label: "Type" },
        { key: "modifiedUtc", label: "Modified" },
        { key: "previewText", label: "Preview" },
        { key: "connectorDisplayName", label: "Connector" }
      ]}
    />
  );
}
