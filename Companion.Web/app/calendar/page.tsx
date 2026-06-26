"use client";

import { DataPage } from "@/components/data-page";

export default function CalendarPage() {
  return (
    <DataPage
      title="Calendar"
      description="Upcoming read-only calendar event snapshots."
      endpoint="/api/calendar/events"
      columns={[
        { key: "title", label: "Event" },
        { key: "startUtc", label: "Starts" },
        { key: "endUtc", label: "Ends" },
        { key: "location", label: "Location" },
        { key: "connectorDisplayName", label: "Connector" }
      ]}
    />
  );
}
