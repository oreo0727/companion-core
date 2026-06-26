# Production Read Connectors

Phase 13 adds OAuth-backed read connector implementations for Google and Microsoft.

## Connectors

- Google Calendar -> `CalendarEventSnapshot`
- Google Drive -> `FileDocumentSnapshot`
- Gmail -> `EmailMessageSnapshot`
- Microsoft Calendar -> `CalendarEventSnapshot`
- OneDrive -> `FileDocumentSnapshot`
- Outlook Mail -> `EmailMessageSnapshot`

All connectors are read-only. They do not send email, edit calendar events, modify files, delete records, or write back to external providers.

## Sync

Production connectors run through the same lifecycle as local connectors:

`ConnectorConnection -> IConnectorRegistry -> IConnectorSyncService -> ConnectorSyncRun -> Snapshot`

Sync requires an OAuth connector connection with encrypted access token data. The connector decrypts the token server-side, sends a bearer-token read request, and maps the provider response into internal snapshots.

For deterministic smoke and integration testing, `POST /api/connectors/{id}/sync` also accepts a provider-shaped JSON payload. When a payload is supplied, no external HTTP request is made.

## Snapshot APIs

- `GET /api/calendar/events`
- `GET /api/email/messages`
- `GET /api/email/search`
- `GET /api/files/documents`

## Security

- Tokens remain encrypted at rest.
- Tokens are never returned by API responses.
- Snapshots are owned by `UserProfileId`.
- Cross-user access is blocked by controller and service ownership checks.
- Every sync writes `ConnectorSyncStarted` and `ConnectorSyncCompleted` audit records.
