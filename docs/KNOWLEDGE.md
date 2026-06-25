# Knowledge Layer

Phase 7 adds an internal knowledge pipeline for user-owned documents.

## Model

The knowledge layer is persisted through four entities:

- `KnowledgeSource`
  A user-owned origin such as a manual notebook, markdown reference, or imported JSON source.
- `KnowledgeDocument`
  A document stored under a source with its original content and mime type.
- `KnowledgeChunk`
  A chunked segment of a document used for retrieval.
- `KnowledgeCollection`
  A user-owned grouping primitive reserved for future organization workflows.

## Supported Imports

The importer currently accepts:

- `text/plain`
- `text/markdown`
- `application/json`

JSON content is normalized before chunking so retrieval works against a stable representation.

## Ownership

Every knowledge source belongs to a single `UserProfile`.

- no cross-user reads
- no shared collections
- no connector-backed imports yet

## Audit

The system writes audit events for:

- `KnowledgeDocumentImported`
- `KnowledgeSearchPerformed`

Tool execution audit remains separate and still records when the `KnowledgeSearch` tool itself runs.
