# Chat Pipeline

## Goal

`POST /api/chat` is the first useful companion loop. It does not call an external model yet. Instead, it proves out the orchestration and persistence behavior the future companion will rely on.

## Flow

For each incoming message, the runtime performs these steps:

1. Resolve the target conversation.
2. Persist the user message.
3. Load recent conversation history.
4. Search relevant memories.
5. Apply deterministic rules to detect:
   - memories worth saving
   - tasks worth creating
   - actions that should require approval
6. Persist any new artifacts.
7. Generate and persist a structured assistant reply.
8. Return the reply plus the artifacts created or recalled during the turn.

## Memory Selection

Memory search is intentionally simple in this phase:

- only non-archived memories are considered
- expired memories are ignored
- matching is case-insensitive over `Summary` and `Content`
- importance helps rank results
- matched memories receive a new `LastReferencedUtc`

This gives Companion Core a real recall path without depending on embeddings or an external model.

## Deterministic Triggers

### Memory capture

The system saves a memory when the message contains:

- `remember`
- `from now on`
- `note that`

### Task creation

The system creates a task when the message contains:

- `remind me`
- `todo`
- `task`
- `I need to`

### Approval creation

The system creates approval requests when the message contains:

- `send`
- `delete`
- `purchase`
- `schedule`

Risk levels are currently assigned as:

- `send` -> `High`
- `delete` -> `High`
- `purchase` -> `High`
- `schedule` -> `Medium`

## Why Placeholder Logic Is Intentional

The current rules are deliberately deterministic because the goal of this phase is not AI quality. The goal is to validate the backend shape:

- conversation continuity
- memory recall behavior
- task persistence
- approval gating
- structured response contracts

Once these behaviors are stable, a future model can replace or augment the rule layer without requiring a redesign of the storage model or API.
