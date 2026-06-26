# Companion Core

Companion Core is the backend spine of a private AI companion platform. It is intentionally not a chatbot demo and not a general-purpose agent framework. Phase 18 adds catalog-backed specialist agents coordinated through durable, auditable `AgentRun` records.

## What This Phase Includes

- .NET 8 solution using ASP.NET Core Web API, Entity Framework Core, and PostgreSQL
- Clean split across `Companion.Core`, `Companion.Infrastructure`, `Companion.Api`, and `Companion.Worker`
- Persistent domain model for profiles, conversations, messages, memories, tasks, goals, projects, open loops, approvals, agent runs, connector accounts, provider configurations, and suggestion records
- `POST /api/chat` pipeline that persists the user message, builds bounded context, runs the Chief Of Staff reasoning engine, extracts candidate memories/goals/projects/tasks, stores them as suggestions, creates approval requests and open loops when needed, and returns a structured assistant reply
- Provider abstraction with `OpenAI`, `Anthropic`, and `Ollama` implementations behind `IAIProvider`
- Provider configuration persistence through `AiProviderConfiguration` plus `/api/settings/ai`
- Suggestion approval flow through `/api/suggestions`
- Timeout-aware provider execution and fallback telemetry on `AgentRun`
- ASP.NET Core Identity with `ApplicationUser` plus `UserProfile` linkage
- JWT bearer authentication with `User` and `Administrator` roles
- `UserPreference`, encrypted `StoredSecret`, and `AuditEvent` persistence
- Internal tool runtime with `ToolDefinition`, `ToolPermission`, `ToolExecution`, and approval-aware execution
- Knowledge import, chunking, keyword retrieval, and context injection
- Read-only connector framework with connector definitions, user connections, sync runs, calendar snapshots, and email snapshots
- OAuth foundation for Google and Microsoft connector consent, encrypted token storage, scope tracking, and connection lifecycle
- Production read connectors for Google Calendar, Google Drive, Gmail, Microsoft Calendar, OneDrive, and Outlook Mail
- Voice platform architecture with STT/TTS abstractions, sessions, interruption, wake sessions, conversation history, and streaming-ready responses
- React Native Expo mobile app for chat, voice, dashboard, tasks, approvals, notifications, briefings, offline cache, and biometric local unlock
- Desktop automation tool runtime for approved file, screenshot, clipboard, terminal, keyboard, mouse, and application-launch actions
- Home automation connector architecture for LocalHome, Home Assistant, Hue, SmartThings, Shelly, ESPHome, and MQTT snapshots with approved actions
- Multi-agent orchestration with catalog-backed Chief of Staff, Planner, Research, Coder, Writer, Travel, Finance, Health, and Home specialists
- Adaptive learning events for suggestion outcomes, tool usage, ratings, completions, and preference evolution
- Internal notification and reminder engine with worker processing
- Background worker that processes pending `AgentRun` records every 30 seconds
- Next.js web console with JWT login, dark mode, responsive navigation, search, pagination, Markdown chat rendering, and SignalR-ready client architecture
- Swagger-enabled API and Docker Compose bootstrap with an optional Ollama profile

## Solution Layout

- `Companion.Core`
  Entities, enums, command/result models, and service interfaces
- `Companion.Infrastructure`
  EF Core persistence, migrations, provider implementations, reasoning/context services, and startup extensions
- `Companion.Api`
  REST API for chat, conversations, memories, tasks, goals, projects, open loops, approvals, agent runs, settings, suggestions, briefing, and dashboard
- `Companion.Worker`
  Background processor for pending specialist-agent runs
- `Companion.Web`
  React, TypeScript, Next.js, Tailwind, and TanStack Query admin console

## Quick Start

### Docker

```bash
docker compose up --build
```

Start the optional Ollama service with:

```bash
docker compose --profile ollama up --build
```

Once the containers are running:

- Web console: `http://localhost:3000`
- Swagger UI: `http://localhost:8080/swagger`
- PostgreSQL remains internal to Docker Compose; the API and worker connect to it over the compose network
- Local development admin: `local.user@companion-core.local` / `CompanionDev123!`

### Local Development

1. Start PostgreSQL locally or through Docker.
2. Update the `DefaultConnection` string if needed.
3. Build the solution:

```bash
dotnet clean Companion.Core.sln
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

6. Run the web console:

```bash
cd Companion.Web
npm install
npm run dev
```

7. Run the smoke test:

```bash
./scripts/smoke-test.sh
```

Useful local URLs:

- Web console: `http://localhost:3000`
- API Swagger: `http://localhost:8080/swagger`
- Tailscale web console from another device: `http://100.71.8.121:3000` when the dev server is bound to `0.0.0.0`

### Authentication

The API is authenticated by default.

- Register with `POST /api/auth/register`
- Or log into the seeded local admin with `POST /api/auth/login`
- Send the returned bearer token as `Authorization: Bearer <token>`

### Web Console

The web app lives in `Companion.Web` and calls the API through `NEXT_PUBLIC_API_BASE_URL`, defaulting to `http://localhost:8080`.

```bash
cd Companion.Web
npm ci
npm run typecheck
npm run build
```

To expose the development server to another machine on the LAN or Tailscale:

```bash
npm run dev -- --hostname 0.0.0.0
```

## API Surface

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `POST /api/chat`
- `GET /api/settings/ai`
- `PUT /api/settings/ai`
- `GET /api/preferences`
- `PUT /api/preferences/{preferenceType}`
- `GET /api/audit`
- `GET /api/suggestions`
- `POST /api/suggestions/{id}/approve`
- `POST /api/suggestions/{id}/reject`
- `GET /healthz`
- `GET /api/conversations`
- `GET /api/conversations/{id}/messages`
- `GET /api/memories`
- `POST /api/memories`
- `PUT /api/memories/{id}/archive`
- `GET /api/tasks`
- `GET /api/tasks/open`
- `POST /api/tasks`
- `PUT /api/tasks/{id}`
- `GET /api/goals`
- `GET /api/goals/suggestions`
- `POST /api/goals`
- `PUT /api/goals/{id}`
- `POST /api/goals/suggestions/{id}/approve`
- `POST /api/goals/suggestions/{id}/reject`
- `GET /api/projects`
- `GET /api/projects/suggestions`
- `POST /api/projects`
- `PUT /api/projects/{id}`
- `POST /api/projects/suggestions/{id}/approve`
- `POST /api/projects/suggestions/{id}/reject`
- `GET /api/open-loops`
- `POST /api/open-loops`
- `POST /api/open-loops/{id}/close`
- `GET /api/approvals`
- `POST /api/approvals`
- `POST /api/approvals/{id}/approve`
- `POST /api/approvals/{id}/reject`
- `GET /api/agent-runs`
- `POST /api/agent-runs`
- `GET /api/notifications`
- `POST /api/notifications/{id}/read`
- `GET /api/reminders`
- `POST /api/reminders`
- `GET /api/tools`
- `GET /api/tools/executions`
- `POST /api/tools/{id}/execute`
- `GET /api/connectors`
- `GET /api/oauth/providers`
- `GET /api/oauth/connections`
- `POST /api/oauth/{provider}/authorize`
- `POST /api/oauth/{provider}/callback`
- `DELETE /api/oauth/connections/{connectionId}`
- `POST /api/connectors/local-calendar/import`
- `POST /api/connectors/local-email/import`
- `POST /api/connectors/{id}/sync`
- `GET /api/calendar/events`
- `GET /api/email/messages`
- `GET /api/email/search`
- `GET /api/files/documents`
- `GET /api/voice/providers`
- `POST /api/voice/wake`
- `POST /api/voice/sessions`
- `POST /api/voice/sessions/{id}/transcribe`
- `POST /api/voice/sessions/{id}/conversation`
- `POST /api/voice/sessions/{id}/speak`
- `POST /api/voice/sessions/{id}/interrupt`
- `POST /api/voice/sessions/{id}/end`
- `GET /api/voice/sessions/{id}/history`
- `POST /api/knowledge/import`
- `GET /api/knowledge/search`
- `GET /api/knowledge/sources`
- `GET /api/companion/briefing`
- `GET /api/companion/dashboard`

## Chat V2

The current chat loop is provider-driven with a deterministic fallback.

- Context is assembled from recent messages, memories, tasks, goals, projects, open loops, approvals, and planning insights.
- The enabled provider receives a structured prompt through `IAIProvider`.
- The reasoning payload may include internal `toolRequests`.
- Tool requests are executed only after permission and approval checks.
- Upcoming calendar events and important recent email are retrieved from connector snapshots and added to the bounded prompt context.
- Upcoming reminders and unread notifications are included in context.
- Relevant knowledge chunks are retrieved and added to the bounded prompt context.
- The extraction pass creates candidate memories, goals, projects, and tasks.
- Candidates are stored as suggestions and require approval before they become first-class entities.
- High-risk action language still produces `ApprovalRequest` records.
- If the provider fails or returns unusable output, the chat pipeline falls back to a bounded deterministic reply.

AI-generated memories, goals, projects, and tasks are never written directly to first-class tables. They must enter the system as pending suggestions first and only become durable entities after an explicit approval step.

Tool executions are recorded separately from suggestions. Built-in tools can read or mutate first-class data only through the audited tool runtime.

## AI Providers

- `OpenAI` and `Anthropic` are seeded disabled.
- `Ollama` is seeded enabled with `llama3`.
- Providers are switched through data, not code changes.
- API keys can live in the database row or config/environment settings.
- Provider timeouts are stored per configuration row through `TimeoutSeconds` and default to `30`.
- If Ollama is unavailable, chat still completes through fallback behavior.

### Fallback Testing

- Disable every provider in `/api/settings/ai` to test pure fallback mode.
- Point `Ollama` at an unreachable base URL to test unavailable-provider fallback.
- Run `./scripts/smoke-test.sh` to exercise unavailable, malformed, timeout, and mock-success provider scenarios automatically.

## Planning Rules

The deterministic planning layer is still used for dashboard insights, open loop capture, approval detection, and fallback behavior.

- Goal suggestion triggers:
  `I want to`, `My goal is`, `I am trying to`
- Open loop triggers:
  `Need to`, `Still haven't`, `Waiting on`
- Project suggestion rule:
  repeated references to the same title-cased topic across recent user messages create a `ProjectSuggestion`

Briefings and the dashboard synthesize:

- open tasks
- pending approvals
- recent memories
- active goals
- active projects
- open loops
- pending suggestions
- planning insights

## Internal Tools

The current built-in tool set is intentionally narrow:

- `MemorySearch`
- `KnowledgeSearch`
- `CalendarEvents`
- `EmailSearch`
- `CreateReminder`
- `ListNotifications`
- `CreateTask`
- `GetBriefing`

Risk is enforced centrally:

- low-risk tools can execute immediately
- medium/high-risk tools create approval requests first
- every execution writes audit data

## Seed Data

The migrations seed:

- 1 development `ApplicationUser` / `UserProfile` pair named `Local User`
- 2 roles: `User` and `Administrator`
- 1 starter `Conversation`
- 3 `MemoryEntry` records
- 3 `TaskItem` records
- 1 `Goal`
- 1 `Project`
- 1 `OpenLoop`
- 3 `UserPreference` records
- 3 `AiProviderConfiguration` records

## Current Constraints

- No SMS, push notifications, email sending, email deleting, or email archiving yet
- Desktop automation exists behind tool permissions, approvals, allowed-root path controls, and audited dry-run defaults for process/input execution
- Voice architecture exists, and the mobile app uses simulated transcript input until real device audio capture is wired in
- The web app is an admin console; the Expo app is the mobile client
- No destructive connector actions; current connectors are read-only local calendar and local email importers
- Google and Microsoft connectors are read-only; they sync snapshots and do not modify external data
- No external knowledge connectors yet; imports are direct API submissions only
- Provider calls use plain `HttpClient` and require external model availability plus valid configuration
- Suggestion approval boundaries remain in place before new durable user data is persisted
- The seeded local admin is a development bootstrap only

## Additional Docs

- [Architecture Blueprint](docs/BLUEPRINT.md)
- [AI Architecture](docs/AI_ARCHITECTURE.md)
- [AI Failure Modes](docs/AI_FAILURE_MODES.md)
- [Agents](docs/AGENTS.md)
- [Actions](docs/ACTIONS.md)
- [Authentication](docs/AUTHENTICATION.md)
- [Context Builder](docs/CONTEXT_BUILDER.md)
- [Data Ownership](docs/DATA_OWNERSHIP.md)
- [Developer Notes](docs/DEV_NOTES.md)
- [Desktop Automation](docs/DESKTOP_AUTOMATION.md)
- [Connectors](docs/CONNECTORS.md)
- [Calendar Connector](docs/CALENDAR_CONNECTOR.md)
- [Email Connector](docs/EMAIL_CONNECTOR.md)
- [Home Automation](docs/HOME_AUTOMATION.md)
- [Knowledge Layer](docs/KNOWLEDGE.md)
- [Learning](docs/LEARNING.md)
- [Notifications](docs/NOTIFICATIONS.md)
- [Mobile Application](docs/MOBILE.md)
- [OAuth Foundation](docs/OAUTH.md)
- [Production Connectors](docs/PRODUCTION_CONNECTORS.md)
- [Tool Runtime](docs/TOOLS.md)
- [Voice Platform](docs/VOICE.md)
- [Web Application](docs/WEB_APPLICATION.md)
- [Retrieval](docs/RETRIEVAL.md)
- [Chat Pipeline](docs/CHAT_PIPELINE.md)
- [Memory Model](docs/MEMORY_MODEL.md)
- [Provider Model](docs/PROVIDER_MODEL.md)
- [Security Model](docs/SECURITY_MODEL.md)
- [Smoke Testing](docs/SMOKE_TESTING.md)
