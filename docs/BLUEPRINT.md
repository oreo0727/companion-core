# Companion Core Blueprint

## Purpose

Companion Core is the persistent backend for a private AI companion. The system is meant to remember durable facts, preserve conversation continuity, surface practical work, and keep the user in control when real-world actions are involved.

The current phase is the first useful companion loop. It is still intentionally deterministic, but it now behaves like a Chief Of Staff backbone rather than a raw CRUD service.

## Architectural Direction

### Core Principles

- Persistence first: companion state should survive across requests and sessions
- Human control: risky actions must surface as approvals before execution
- Layered orchestration: the system can compose smaller services without becoming an opaque framework
- Replaceable intelligence: deterministic logic should be easy to swap for real AI later
- Intentional simplicity: every current behavior should be inspectable and safe to evolve

### Current Project Structure

- `Companion.Core`
  Entities, enums, command/result models, and service contracts
- `Companion.Infrastructure`
  EF Core persistence, migrations, deterministic application services, and startup wiring
- `Companion.Api`
  HTTP endpoints for chat, conversations, memories, tasks, goals, projects, open loops, approvals, agent runs, briefing, and dashboard
- `Companion.Worker`
  Background worker that processes pending `AgentRun` records

## Brain Spine

The Phase 2 "brain spine" is the synchronous request-time loop behind `POST /api/chat`.

For each user message, the runtime:

- persists the message
- loads recent conversation history
- searches relevant memories
- applies deterministic rules to detect memories, tasks, and approval-worthy actions
- invokes the Chief Of Staff layer to capture open loops and planning suggestions
- persists any new artifacts
- writes a structured assistant reply back into the conversation

This gives the platform a durable conversational center even before model-based reasoning exists.

## Chief Of Staff Layer

The new planning layer extends the brain spine with a deterministic `ChiefOfStaffService`.

It analyzes:

- memories
- tasks
- approvals
- goals
- projects
- open loops
- recent conversations

It produces:

- `CompanionInsight` items for focus, blockers, stale work, and deadline pressure
- `GoalSuggestion` records when messages express intent
- `ProjectSuggestion` records when recent messages repeatedly reference the same topic
- `OpenLoop` records when messages describe unresolved commitments

The design goal is to help Companion identify what matters, what is blocked, what is being forgotten, and what should happen next without depending on a language model yet.

## Conversation Model

Conversations now track:

- `LastMessageUtc`
- `ActiveTopic`

Messages now carry:

- `MetadataJson`
- `TokensEstimate`

That metadata creates room for future summaries, ranking, retrieval hints, and model-facing context windows without changing the API shape.

## Agent Model

There are now two execution paths:

### Synchronous chat runtime

The deterministic chat pipeline runs inside the request and returns a structured result immediately.

### Background specialist runtime

`AgentRun` remains the durable abstraction for asynchronous specialist work:

- API-created runs start as `Pending`
- the worker polls every 30 seconds
- runs move through `Running`, `Completed`, or `Failed`
- timestamps, output, and errors are stored

This split keeps chat responsive while preserving a clean path toward future queued agents.

## Planning Model

Planning is now first-class through:

- `Goal`
- `Project`
- `OpenLoop`
- `GoalSuggestion`
- `ProjectSuggestion`

Suggestions are intentionally reviewable instead of auto-promoted. This keeps the deterministic layer useful while preserving user control over what becomes durable planning state.

## Approval Model

Approvals act as the safety boundary around risky actions. Requests now capture:

- user linkage
- conversation linkage
- source message linkage
- type
- reason
- payload
- risk level
- review status and timestamps

Approvals are no longer abstract records; they are now anchored to the conversation event that caused them.

## Near-Term Evolution

Expected next layers include:

- richer memory ranking and compaction
- better task parsing and scheduling
- stronger project and goal linking between messages, tasks, and open loops
- approval policies tied to identity and trust
- structured action payloads for future connectors
- model-based reasoning behind the same service interfaces

## Operational Model

- API and worker share the same infrastructure layer and PostgreSQL database
- migrations are applied automatically on startup
- seed data provides a ready-to-use local user and starter state
- Docker Compose remains the one-command development bootstrap
