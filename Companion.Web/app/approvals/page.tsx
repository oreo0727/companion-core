"use client";

import { DataPage, StatusBadge } from "@/components/data-page";

export default function ApprovalsPage() {
  return (
    <DataPage
      title="Approvals"
      description="Pending and reviewed approval requests."
      endpoint="/api/approvals"
      columns={[
        { key: "type", label: "Type" },
        { key: "reason", label: "Reason" },
        { key: "riskLevel", label: "Risk", render: (item) => <StatusBadge value={item.riskLevel} /> },
        { key: "status", label: "Status", render: (item) => <StatusBadge value={item.status} /> },
        { key: "createdUtc", label: "Created" }
      ]}
    />
  );
}
