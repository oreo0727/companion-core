"use client";

import { DataPage } from "@/components/data-page";

export default function ContactsPage() {
  return (
    <DataPage
      title="Contacts"
      description="Read-only contact snapshots from people capabilities."
      endpoint="/api/contacts"
      searchKeys={["displayName", "email", "phone", "organization", "connectorDisplayName"]}
      columns={[
        { key: "displayName", label: "Name" },
        { key: "email", label: "Email" },
        { key: "phone", label: "Phone" },
        { key: "organization", label: "Organization" },
        { key: "connectorDisplayName", label: "Connector" }
      ]}
    />
  );
}
