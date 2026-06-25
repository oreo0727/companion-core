# AI Failure Modes

## Guardrails

Phase 4B hardens the reasoning path around five failure classes:

1. no provider enabled
2. provider endpoint unreachable
3. provider returns malformed JSON
4. provider times out
5. extraction output is malformed or incomplete

## Expected Behavior

### No Provider Enabled

- chat returns a deterministic fallback reply
- `usedFallback` is `true`
- `AgentRun.Provider` and `AgentRun.Model` stay empty
- heuristic suggestion extraction still runs

### Provider Unreachable

- chat returns a deterministic fallback reply
- attempted provider/model are still written to `AgentRun`
- `AgentRun.Error` stores the connection failure message
- suggestion extraction falls back safely instead of writing corrupt data

### Malformed JSON

- reasoning falls back instead of returning raw malformed payload text
- `AgentRun.Error` stores a clear malformed-JSON message
- extraction falls back to heuristics

### Provider Timeout

- provider timeout is driven by `AiProviderConfiguration.TimeoutSeconds`
- timeout raises a clear error message
- chat still returns a fallback reply
- `AgentRun.Error` records the timeout

## Suggestion Safety Rule

AI output never writes directly into:

- `MemoryEntry`
- `Goal`
- `Project`
- `TaskItem`

AI-generated candidates must first be stored as pending suggestions. Only explicit approval can materialize them into durable entities.

Deterministic flows that already existed outside the AI extraction path can still create first-class records directly when that was the intended pre-AI behavior.
