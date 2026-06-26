# Companion Operating System

Phase 20 brings the platform together as a durable operating layer.

## Routine Runs

`OperatingSystemRun` records generated routines:

- daily briefing
- morning startup
- evening recap
- weekly review
- monthly review
- long-term planning
- goal forecast
- project forecast
- memory pruning review
- context optimization
- conversation summarization
- continuous context update

Each run stores:

- summary
- insights JSON
- actions JSON
- forecast JSON
- period start/end
- scheduled follow-up `AgentRun`

## API

- `GET /api/os/runs`
- `POST /api/os/routines/{routineType}/generate`
- `POST /api/os/context/optimize`

## Explainability

The operating-system layer does not silently delete memories, complete tasks, or execute risky actions. It produces auditable routine records and schedules AgentRuns for follow-up. Dangerous actions still go through the tool runtime and approval system.

## Context Optimization

Context optimization reports pressure from:

- memories
- conversations needing summarization
- important recent email
- pending approvals
- overdue tasks

Memory pruning is recommendation-based. Low-importance old memories are surfaced as candidates for review; they are not automatically deleted.

## Audit

Every generated run writes `OperatingSystemRunGenerated`.
