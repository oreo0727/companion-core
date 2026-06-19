# Memory Model

## Goal

The memory system is meant to give the companion durable recall without forcing every piece of information into a conversation transcript.

`MemoryEntry` is the persistent abstraction for that layer, and in this phase it is also part of the live chat pipeline.

## Current Shape

Each memory stores:

- `UserProfileId`
- `Type`
- `Summary`
- `Content`
- `Confidence`
- `Source`
- `CreatedUtc`
- `LastReferencedUtc`
- `Importance`
- `Sensitivity`
- `ExpiresUtc`
- `IsArchived`

## How Memory Is Used Today

When a message arrives at `POST /api/chat`, the runtime performs a deterministic search across non-archived, non-expired memories for the local user. Matching is currently simple and case-insensitive:

- the full message is checked against memory `Summary` and `Content`
- individual message terms are also checked
- more important memories rank higher

When a memory is selected, `LastReferencedUtc` is updated. That makes memory usage visible and gives later ranking systems useful behavioral history.

The Chief Of Staff layer also uses memories as planning evidence. Memories can influence:

- whether a project looks active or stale
- which topics appear repeatedly important
- which durable preferences should shape planning outputs

## Design Intent

### `Type`

`Type` is string-based on purpose so the model can evolve without forcing enum migrations for every new memory category. Early examples include:

- preference
- biography
- project context
- recurring workflow
- relationship context

### `Summary` and `Content`

`Summary` is the short retrieval surface. `Content` is the fuller payload. This allows future ranking and UI flows to scan a compact representation first and expand only when needed.

### `Confidence`

`Confidence` is a decimal from `0` to `1`. It represents how reliable the system believes the memory is. In later phases, confidence can be influenced by:

- explicit user confirmation
- repeated mentions
- source trust level
- recency
- conflict detection

### `Source`

`Source` preserves provenance. A future system can distinguish between:

- direct user statements
- imported connector data
- inferred summaries
- agent-produced notes
- manual edits

### `LastReferencedUtc`

This field is now actively used by the deterministic recall flow. As retrieval and ranking mature, the system can use it to:

- prefer active memories
- decay stale context
- audit which memories influence answers or actions

### `Importance`

`Importance` is a simple integer from `1` to `5`. It gives the platform a lightweight ranking signal before embeddings or learned scoring exist.

Current uses:

- prioritize which memories are returned for a chat turn
- distinguish low-value notes from durable preferences or project anchors

### `Sensitivity`

`Sensitivity` is string-based so the safety vocabulary can evolve without constant enum migrations.

Early examples:

- `Normal`
- `High`
- `Private`

This field is not enforcing access policy yet, but it establishes the data needed for later redaction and approval behavior.

### `ExpiresUtc`

Some memories should not live forever. `ExpiresUtc` creates a path for:

- temporary reminders
- short-lived context
- imported connector data with natural staleness

Expired memories are excluded from the deterministic search path.

### `IsArchived`

Archiving is the first user-controlled way to remove a memory from active recall without deleting it. Archived memories:

- remain in storage
- are excluded from recall
- can still be inspected later if the API grows richer memory management surfaces

## Future Memory Directions

- semantic retrieval over `Summary` and `Content`
- conflict resolution between competing memories
- short-term versus long-term memory tiers
- stronger links between memory records and projects, goals, and open loops
- user-visible memory editing and pinning
- memory compaction and summarization pipelines

The current implementation is deliberately simple and deterministic, but the schema is now shaped for real recall behavior rather than passive storage alone.
