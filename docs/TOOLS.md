# Tool Runtime

Phase 6 adds an internal action framework so Companion can perform bounded, auditable actions without reaching into external systems.

## Core Model

The runtime is built around three persisted records:

- `ToolDefinition`
  Describes a built-in capability, its category, and its risk posture.
- `ToolPermission`
  Grants or denies a specific user access to a specific tool.
- `ToolExecution`
  Captures each execution attempt, approval state, output, and failure details.

## Runtime Interfaces

- `ITool`
  Implements a single executable tool.
- `IToolRegistry`
  Registers tools in the runtime and exposes what is available.
- `IToolExecutor`
  Applies permission checks, approval checks, execution, persistence, and audit logging.

## Built-In Tools

- `MemorySearch`
  Searches the authenticated user's saved memories.
- `KnowledgeSearch`
  Searches the authenticated user's imported knowledge documents.
- `CreateTask`
  Creates a task for the authenticated user after approval.
- `GetBriefing`
  Returns the current companion briefing for the authenticated user.

These tools are intentionally internal-only. They operate on Companion Core state and do not call Gmail, Calendar, or other external APIs.

## Risk Levels

- `Low`
  Can execute immediately after permission checks.
- `Medium`
  Creates an `ApprovalRequest` before execution.
- `High`
  Always creates an `ApprovalRequest` before execution.

The current policy is strict: any tool marked `RequiresApproval` or any tool above `Low` risk is held for approval first.

## Persistence And Telemetry

Each `ToolExecution` records:

- user
- tool definition
- related `AgentRun` when the request came from chat reasoning
- status
- input payload
- output payload
- error
- start and completion timestamps

Every execution attempt also writes an `AuditEvent`.

## API

- `GET /api/tools`
  Returns the enabled tools the current user is allowed to execute.
- `GET /api/tools/executions`
  Returns the current user's execution history.
- `POST /api/tools/{id}/execute`
  Executes immediately for low-risk tools or returns `202 Accepted` with an `ApprovalRequest` for gated tools.
