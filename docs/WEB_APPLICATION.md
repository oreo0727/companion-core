# Web Application

Phase 11 adds `Companion.Web`, a production-oriented Next.js administrative console for Companion Core.

## Stack

- React
- TypeScript
- Next.js App Router
- Tailwind CSS
- TanStack Query
- JWT bearer authentication
- Markdown rendering in chat

The UI includes a lightweight `SignalRReadyClient` abstraction so realtime updates can be introduced without reshaping page code.

## Local Development

Start the API first:

```bash
dotnet run --project Companion.Api
```

Then start the web app:

```bash
cd Companion.Web
npm install
npm run dev
```

Open `http://localhost:3000` and log in with the development account:

```text
local.user@companion-core.local
CompanionDev123!
```

Set `NEXT_PUBLIC_API_BASE_URL` when the API is not on `http://localhost:8080`.

## Docker

```bash
docker compose up --build
```

The web console is available at `http://localhost:3000`, and it calls the API at `http://localhost:8080`.

## Pages

- Dashboard
- Chat
- Memories
- Knowledge
- Tasks
- Goals
- Projects
- Open Loops
- Calendar
- Email
- Notifications
- Approvals
- Tool Executions
- Audit
- Settings
- Connectors
- AI Settings

## Security

The browser stores the JWT in local storage for the local admin console. API authorization remains server-enforced; the web app only hides unauthenticated routes and attaches the bearer token.

## Verification

```bash
cd Companion.Web
npm ci
npm run typecheck
npm run build
```

The repository smoke script runs those checks before exercising the API and worker.
