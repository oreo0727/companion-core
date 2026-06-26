# Multi-Agent Orchestration

Phase 18 adds a catalog-backed specialist agent layer.

## Agent Catalog

`AgentDefinition` stores:

- stable agent name and display name
- specialist prompt
- allowed tool names
- context policy JSON
- memory weighting
- enabled state

Seeded agents:

- `ChiefOfStaff`
- `Planner`
- `Research`
- `Coder`
- `Writer`
- `Travel`
- `Finance`
- `Health`
- `Home`

The catalog is available at `GET /api/agents`.

## Agent Runs

`AgentRun` now records:

- `AgentDefinitionId`
- `ParentAgentRunId`
- `DelegationReason`
- started/completed timestamps
- telemetry and fallback fields from earlier phases

The worker processes pending runs through `IMultiAgentOrchestrator`. A `ChiefOfStaff` run can create child `AgentRun` records for matching specialists. Child runs are processed by the same worker loop, which keeps collaboration durable and auditable.

## Safety

Specialist agents do not bypass the tool runtime. Any tool execution still flows through permissions, approval checks, audit logging, and persisted `ToolExecution` records.

High-risk tools remain approval-gated even when requested by a specialist agent.

## Audit

The system records:

- `AgentRunQueued`
- `AgentRunDelegated`
- `AgentRunCompleted`
- `AgentRunFailed`

These events make delegation and background collaboration inspectable without relying on opaque model traces.
