# Sync Engine

Connector sync runs through `IConnectorSyncService`.

Every sync creates a `ConnectorSyncRun` and audit events:

- `ConnectorSyncStarted`
- `ConnectorSyncCompleted`

Failures are captured on the sync run and move the connection to `NeedsAttention`.

## Worker Schedule

`ConnectorSyncWorker` processes connected OAuth connectors on a conservative schedule:

- Calendar: every 15 minutes
- Email: every 10 minutes
- Drive/files: every hour
- People/contacts: daily

The worker calls the existing sync service, so token handling, snapshot writes, status changes, and audit logging remain centralized.

## Mocked Sync

For tests and local smoke flows, `POST /api/connectors/{id}/sync` accepts provider-shaped JSON payloads. When a payload is supplied, no external provider call is made.
