# Actions And Approval Flow

Companion action execution follows a fixed path:

`AI -> Tool Request -> Permission Check -> Approval Check -> Execution -> Result -> Audit`

## Sources Of Actions

Actions can enter the runtime in two ways:

1. Direct API execution through `POST /api/tools/{id}/execute`
2. AI reasoning output through `toolRequests` in the chat response envelope

In both cases, the same executor path is used. There is no fast path that skips permission or approval checks.

## Approval Rules

- Low-risk tools execute immediately after permission checks.
- Medium-risk tools create an `ApprovalRequest`.
- High-risk tools create an `ApprovalRequest`.
- Explicit `RequiresApproval = true` always wins.

Approving a tool-related `ApprovalRequest` resumes the pending `ToolExecution`. Rejecting it marks the execution as rejected and writes an audit entry.

## Audit Requirements

Every tool execution lifecycle step must leave a durable trace:

- approval requested
- execution completed
- execution failed
- execution rejected

Audit descriptions include the tool name, execution status, input summary, and result or error summary.

## AI Safety Boundary

AI-generated tool requests are proposals, not authority.

The model may suggest:

```json
{
  "message": "I can capture that as a task.",
  "toolRequests": [
    {
      "tool": "CreateTask",
      "input": {
        "title": "Follow up on launch deck"
      }
    }
  ]
}
```

The runtime still checks:

- whether the tool exists
- whether the user is permitted to use it
- whether approval is required
- whether the tool executed successfully

## Durable Data Safety

AI-generated suggestions are still not promoted directly into first-class tables.

- `MemoryEntry`
- `Goal`
- `Project`
- `TaskItem`

Model output must remain in pending suggestion state unless it comes from an existing deterministic rule or a separately approved tool execution path.
