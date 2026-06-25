# Retrieval

Phase 7 uses keyword retrieval first and keeps the interfaces ready for future vector search.

## Current Flow

`KnowledgeSource -> KnowledgeDocument -> KnowledgeChunk -> Keyword Search -> Context Builder`

## Chunking

The chunking service:

- preserves source title and mime type in `MetadataJson`
- tracks chunk order with `ChunkIndex`
- keeps markdown heading hints when available
- splits oversized paragraphs into bounded segments

## Search Ranking

`IKnowledgeSearchService` scores results using:

- exact query phrase matches
- individual term matches
- title matches
- source-name matches

Search results return:

- source
- document
- chunk
- relevance score

## Context Integration

The reasoning context now prioritizes:

1. conversation
2. memories
3. goals
4. projects
5. knowledge
6. tasks

Knowledge retrieval is included in the prompt as relevant document chunks, not as a full-document dump.

## Tooling

`KnowledgeSearch` is a low-risk internal tool. It uses the same retrieval service as the knowledge API and returns matching source/document/chunk references.
