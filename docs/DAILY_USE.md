# Daily Use Hardening

Phase 21 focuses on making a single-user Companion Core instance easy to run, inspect, and recover.

## First Run

Start the stack:

```bash
docker compose up --build
```

Open the setup wizard:

```text
http://localhost:3000/setup
```

The wizard reads `GET /api/setup/status` without authentication and shows database, identity, administrator, JWT, and CORS readiness. A seeded local administrator is available for development:

```text
local.user@companion-core.local / CompanionDev123!
```

For a real daily-use machine, create your own administrator account and replace development secrets before exposing the instance.

## Admin Health

Open:

```text
http://localhost:3000/admin-health
```

The page shows:

- `GET /api/system/health` for database, queue, approvals, notification, provider, connector, and failure state.
- `GET /api/system/diagnostics` for runtime details, table counts, provider status, and connector status.
- `GET /api/system/logs` for recent audit events plus failed agent and connector runs.
- `GET /api/system/smoke-test/status` for the local smoke-test command and script availability.

The API intentionally does not execute shell scripts. Run smoke tests from the repo root:

```bash
API_PORT=18081 RUN_ID=manual-$(date +%s) ./scripts/smoke-test.sh
```

## Provider And Connector Tests

The web console includes inline test buttons:

- `/ai-settings` calls `POST /api/settings/ai/{provider}/test`.
- `/connectors` calls `POST /api/connectors/{provider}/test`.

Provider test failures are returned as structured status responses so bad keys, missing local models, malformed responses, and timeouts do not break the settings UI.

Connector tests call the registered connector's `TestConnectionAsync` method and return a clear `Succeeded` or `Failed` status. Local connectors can be tested without OAuth.

## Backup And Restore

The admin health page can export and import user-owned data:

- `GET /api/system/backup/export`
- `POST /api/system/backup/import`

The backup envelope includes preferences, active memories, tasks, goals, projects, and reminders. It deliberately excludes passwords, JWTs, encrypted API keys, OAuth tokens, connector tokens, and external account secrets.

Restore imports into the authenticated user and avoids obvious duplicate memories and planning item titles. It is meant for local recovery and migration, not multi-user cloning.

## Local Network And Mobile

For a remote PC or mobile device, set reachable URLs before building the web container or starting Expo:

```bash
export NEXT_PUBLIC_API_BASE_URL=http://192.168.4.191:8080
export COMPANION_WEB_BASE_URL=http://192.168.4.191:3000
docker compose up --build
```

With Tailscale, use the Tailscale IP instead of the LAN IP. The web app and mobile app both need an API URL that is reachable from the device, not just from the server itself.

## Configuration Separation

`appsettings.json` no longer contains local database credentials or the development JWT signing key. Local defaults live in `appsettings.Development.json`, while Docker Compose supplies explicit development environment variables.

For an exposed or long-running instance:

- Copy `.env.example` to `.env`.
- Set a unique `POSTGRES_PASSWORD`.
- Set a long random `JWT_SIGNING_KEY`.
- Set `NEXT_PUBLIC_API_BASE_URL` and `COMPANION_WEB_BASE_URL` to reachable hostnames or IPs.
- Keep API keys and OAuth tokens out of checked-in config.

## Verification Checklist

Before relying on a daily-use instance:

```bash
dotnet clean Companion.Core.sln
dotnet build Companion.Core.sln
dotnet test Companion.Tests/Companion.Tests.csproj
npm --prefix Companion.Web run typecheck
npm --prefix Companion.Web run build
npm --prefix Companion.Mobile ci
npm --prefix Companion.Mobile run typecheck
docker compose up --build
./scripts/smoke-test.sh
```
