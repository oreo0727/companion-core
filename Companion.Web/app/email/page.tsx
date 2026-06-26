"use client";

import { DataPage, StatusBadge } from "@/components/data-page";

export default function EmailPage() {
  return (
    <DataPage
      title="Email"
      description="Read-only recent email snapshots."
      endpoint="/api/email/messages"
      columns={[
        { key: "subject", label: "Subject" },
        { key: "fromAddress", label: "From" },
        { key: "receivedUtc", label: "Received" },
        { key: "isRead", label: "Read", render: (item) => <StatusBadge value={item.isRead ? "Read" : "Unread"} /> },
        { key: "hasAttachments", label: "Attachments" }
      ]}
    />
  );
}
