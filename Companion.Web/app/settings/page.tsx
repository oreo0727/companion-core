"use client";

import { DataPage } from "@/components/data-page";

export default function SettingsPage() {
  return (
    <DataPage
      title="Settings"
      description="User preferences and personalization settings."
      endpoint="/api/preferences"
      columns={[
        { key: "preferenceType", label: "Preference" },
        { key: "value", label: "Value" },
        { key: "updatedUtc", label: "Updated" }
      ]}
    />
  );
}
