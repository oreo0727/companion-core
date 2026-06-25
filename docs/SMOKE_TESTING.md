# Smoke Testing

## Script

Run the repeatable smoke test from the repo root:

```bash
./scripts/smoke-test.sh
```

The script fails fast and verifies:

- API health
- Swagger availability
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
- `python3`

## Provider Scenarios Covered

The smoke test exercises:

1. no provider enabled
2. Ollama configured but unreachable
3. Ollama configured against a mock success endpoint
4. malformed provider JSON
5. provider timeout

The script uses `scripts/mock-ai-provider.py` to simulate successful, malformed, and slow provider behavior without adding product code for fake providers.

## Logs

The script writes temporary logs to `/tmp`:

- API log
- worker log
- mock provider log

When a check fails, the script prints the relevant tail output before exiting.
