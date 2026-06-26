"use client";

import { DataPage } from "@/components/data-page";

export default function KnowledgePage() {
  return (
    <DataPage
      title="Knowledge"
      description="Imported knowledge sources and chunk counts."
      endpoint="/api/knowledge/sources"
      columns={[
        { key: "name", label: "Source" },
        { key: "type", label: "Type" },
        { key: "description", label: "Description" },
        { key: "documentCount", label: "Docs" },
        { key: "chunkCount", label: "Chunks" },
        { key: "createdUtc", label: "Created" }
      ]}
    />
  );
}
