"use client";

import { DataPage, StatusBadge } from "@/components/data-page";

export default function ToolExecutionsPage() {
  return (
    <DataPage
      title="Tool Executions"
      description="Execution history for audited internal tools."
      endpoint="/api/tools/executions"
      columns={[
        { key: "toolName", label: "Tool" },
        { key: "status", label: "Status", render: (item) => <StatusBadge value={item.status} /> },
        { key: "inputJson", label: "Input" },
        { key: "error", label: "Error" },
        { key: "startedUtc", label: "Started" }
      ]}
    />
  );
}
