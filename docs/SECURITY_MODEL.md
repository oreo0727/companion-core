# Security Model

## Current Status

Authentication is intentionally out of scope for this first phase. The system assumes a single local user profile seeded into the database for development.

That does not mean security is ignored. The current code establishes the boundaries that later security layers will harden, especially around approvals and action provenance.

## Approval-Centered Control

The most important control in this phase is explicit approval tracking.

`ApprovalRequest` captures:

- which user requested the action
- which conversation the request came from
- which source message triggered it
- what kind of action is being requested
- why the action is being requested
- the payload associated with the action
- the assessed risk level
- whether the request is pending, approved, or rejected
- when it was created and reviewed

This creates a durable approval trail before real integrations are introduced.

Current deterministic approval triggers include:

- `send`
- `delete`
- `purchase`
- `schedule`

The first three are treated as `High` risk and `schedule` is treated as `Medium` risk.

## Service Boundaries

The following interfaces exist specifically so future security and policy behavior can be inserted without rewriting the API surface:

- `IConversationService`
- `IApprovalService`
- `IMemoryService`
- `ITaskService`
- `IAgentRuntime`
- `IConnectorManager`

Each can later host authorization checks, policy enforcement, auditing, rate limiting, and richer risk analysis.

## Why Deterministic Logic Still Helps Security

Using deterministic placeholder logic before real AI integration is a security feature, not just an implementation shortcut.

Right now it provides:

- predictable triggering behavior
- easy-to-audit approval creation
- no hidden outbound model calls
- a stable baseline for later regression testing

This makes it much easier to reason about system behavior before more powerful decision-making is introduced.

## Future Security Layers

Planned expansions include:

- user authentication and session management
- per-user data isolation
- role-aware or policy-aware approvals
- audit logging for external actions
- secret storage for connector credentials
- encrypted sensitive memory payloads
- risk scoring before agent execution or connector use
- approval policies that vary by action type, connector, or confidence

## Trust Assumptions

For now, the trusted boundary is the local development environment plus the private PostgreSQL instance created by Docker Compose.

Before internet-facing or multi-user deployment, the next required steps are:

- real authentication
- HTTPS and reverse-proxy hardening
- structured authorization
- connector credential protection
- stronger audit and observability coverage
