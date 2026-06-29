# Smoke Testing

## Script

Run the repeatable smoke test from the repo root:

```bash
./scripts/smoke-test.sh
```

The script fails fast and verifies:

- API health
- Swagger availability
- first-run setup readiness
- seeded local administrator authentication
- daily-use health, diagnostics, logs, smoke status, backup export, and backup import
- tool discovery
- immediate low-risk tool execution
- approval-gated tool execution
- failed tool execution telemetry
- knowledge import
- knowledge search
- connector discovery
- connector test status
- OAuth provider discovery
- OAuth authorization, callback, disconnect, and audit
- production Google/Microsoft read connector sync into snapshots
- voice provider discovery and session lifecycle
- mobile Expo dependency installation and typecheck
- desktop automation tool discovery, low-risk execution, and approval-gated write execution
- home automation connector discovery, local import, snapshot reads, status tool, and approval-gated action execution
- specialist agent catalog discovery and Chief of Staff delegation to child AgentRuns
- adaptive learning events, conversation ratings, and profile aggregation
- operating-system routine generation, context optimization, scheduled AgentRuns, and audit
- provider test status against the mock Ollama endpoint
- local calendar import
- calendar event retrieval
- local email import
- email message retrieval
- email search
- reminder creation
- worker-created in-app notifications
- notification read flow
- `POST /api/chat`
- persisted user and assistant messages
- memory, task, goal, and project suggestion creation
- approval request creation for risky text
- briefing and dashboard endpoints
- queued `AgentRun` processing by the worker
- fallback/provider hardening scenarios

## Requirements

- local PostgreSQL reachable at `localhost:5432`
- database `companion_core`
- user `postgres`
- password `postgres`
- `curl`
- `dotnet`
- `npm`
- `python3`

The script also runs the Phase 11 web verification:

```bash
npm --prefix Companion.Web ci
npm --prefix Companion.Web run typecheck
npm --prefix Companion.Web run build
```

## Provider Scenarios Covered

The smoke test exercises:

1. no provider enabled
2. Ollama configured but unreachable
3. Ollama configured against a mock success endpoint
4. malformed provider JSON
5. provider timeout

It also exercises:

6. low-risk `GetBriefing` execution
7. approval-gated `CreateTask` execution
8. failed `MemorySearch` execution with stored error state
9. knowledge import and retrieval through API and tool paths
10. local calendar import, briefing inclusion, sync recording, and calendar tool retrieval
11. local email import, briefing inclusion, search, and email tool retrieval
12. scheduled reminders, worker-created notifications, notification read audit, and notification tools
13. Next.js web console typecheck and production build
14. OAuth provider discovery, consent grant, encrypted-token connection, disconnect, and audit
15. Google Calendar, Google Drive, Gmail, Microsoft Calendar, OneDrive, and Outlook Mail snapshot sync
16. mobile Expo app installs and typechecks
17. desktop automation tools, screenshot execution, and approved file write
18. voice wake/session, transcription, conversation chunks, speech synthesis, interruption, and history
19. multi-agent catalog discovery, Chief of Staff delegation, specialist child run completion, and audit
20. adaptive learning profile updates from ratings, ignored suggestions, and tool usage
21. operating-system morning/evening/weekly/context runs with scheduled background AgentRuns
22. first-run setup status, admin health, diagnostics, logs, smoke status, backup export/import, connector test, and provider test endpoints

The script uses `scripts/mock-ai-provider.py` to simulate successful, malformed, and slow provider behavior without adding product code for fake providers.

## Seeded Admin

The smoke script logs in through the seeded local administrator account:

- email: `local.user@companion-core.local`
- password: `CompanionDev123!`

## Logs

The script writes temporary logs to `/tmp`:

- API log
- worker log
- mock provider log

When a check fails, the script prints the relevant tail output before exiting.
