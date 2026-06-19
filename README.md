# Companion Core

Companion Core is the backend spine of a private AI companion platform. It is intentionally not a chatbot demo and not a general-purpose agent framework. The current phase turns the foundation into the first useful assistant loop: persistent conversation continuity, deterministic memory recall, task extraction, approval-gated risky actions, and a daily-style briefing surface.

## What This Phase Includes

- .NET 8 solution using ASP.NET Core Web API, Entity Framework Core, and PostgreSQL
- Clean split across `Companion.Core`, `Companion.Infrastructure`, `Companion.Api`, and `Companion.Worker`
- Persistent domain model for profiles, conversations, messages, memories, tasks, approvals, agent runs, and connector accounts
- Deterministic `POST /api/chat` pipeline that:
  persists the user message, loads recent context, searches memories, creates memories/tasks/approvals when rules match, and returns a structured assistant reply
- Background worker that processes pending `AgentRun` records every 30 seconds
- Swagger-enabled API and Docker Compose bootstrap

## Solution Layout

- `Companion.Core`
  Entities, enums, command/result models, and service interfaces
- `Companion.Infrastructure`
  EF Core persistence, migrations, deterministic service implementations, and startup extensions
- `Companion.Api`
  REST API for chat, conversations, memories, tasks, approvals, agent runs, and briefing
- `Companion.Worker`
  Background processor for pending specialist-agent runs

## Quick Start

### Docker

```bash
docker compose up --build
```

Once the containers are running:

- Swagger UI: `http://localhost:8080/swagger`
- PostgreSQL remains internal to Docker Compose; the API and worker connect to it over the compose network

### Local Development

1. Start PostgreSQL locally or through Docker.
2. Update the `DefaultConnection` string if needed.
3. Build the solution:

```bash
dotnet build Companion.Core.sln
```

4. Run the API:

```bash
dotnet run --project Companion.Api
```

5. Run the worker in another terminal:

```bash
dotnet run --project Companion.Worker
```

## API Surface

- `POST /api/chat`
- `GET /api/conversations`
- `GET /api/conversations/{id}/messages`
- `GET /api/memories`
- `POST /api/memories`
- `PUT /api/memories/{id}/archive`
- `GET /api/tasks`
- `GET /api/tasks/open`
- `POST /api/tasks`
- `PUT /api/tasks/{id}`
- `GET /api/approvals`
- `POST /api/approvals`
- `POST /api/approvals/{id}/approve`
- `POST /api/approvals/{id}/reject`
- `GET /api/agent-runs`
- `POST /api/agent-runs`
- `GET /api/companion/briefing`

## Chat Rules

The current chat loop is deterministic on purpose. It does not call OpenAI or any external model yet.

- Memory capture triggers:
  `remember`, `from now on`, `note that`
- Task creation triggers:
  `remind me`, `todo`, `task`, `I need to`
- Approval triggers:
  `send`, `delete`, `purchase`, `schedule`

These placeholders let the platform validate persistence, orchestration, and approval boundaries before real AI reasoning is introduced.

## Seed Data

The migrations seed:

- 1 `UserProfile` named `Local User`
- 1 starter `Conversation`
- 3 `MemoryEntry` records
- 3 `TaskItem` records

## Current Constraints

- No authentication yet
- No external AI providers yet
- No voice, mobile, email, calendar, or desktop control yet
- Memory recall, task extraction, and approval detection are deterministic placeholder rules

## Additional Docs

- [Architecture Blueprint](docs/BLUEPRINT.md)
- [Chat Pipeline](docs/CHAT_PIPELINE.md)
- [Memory Model](docs/MEMORY_MODEL.md)
- [Security Model](docs/SECURITY_MODEL.md)
