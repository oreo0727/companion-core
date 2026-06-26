# Connectors

Phase 8 adds a read-only connector framework for user-owned external data snapshots.
Phase 9 extends that framework with read-only email snapshots.

## Connector Model

The connector layer uses four persisted records:

- `ConnectorDefinition`
  Describes an available connector provider and its risk posture.
- `ConnectorConnection`
  Represents a user's configured account or feed binding.
- `ConnectorSyncRun`
  Tracks each sync attempt, outcome, and synced item count.
- `CalendarEventSnapshot`
  Stores read-only calendar data materialized from a connection.
- `EmailMessageSnapshot`
  Stores read-only email data materialized from a connection.

## Read-Only Boundary

This phase is intentionally non-destructive.

- no email sending
- no email deleting
- no email archiving
- no write-back to external systems
- no destructive sync behavior
- no connector deletes during import

The local calendar and local email connectors only create or update internal snapshots.

## Registry And Sync Flow

`ConnectorDefinition -> ConnectorConnection -> IConnectorRegistry -> IConnectorSyncService -> ConnectorSyncRun`

Every sync run records:

- start time
- completion time
- status
- items synced
- error text when applicable

## Ownership

All connector connections, calendar snapshots, and email snapshots belong to a `UserProfile`.

- no cross-user connector listing
- no cross-user event retrieval
- no cross-user email retrieval
- no shared connection state

## Audit

The connector layer writes:

- `ConnectorConnected`
- `ConnectorSyncStarted`
- `ConnectorSyncCompleted`
- `CalendarEventsViewed`
- `EmailMessagesViewed`
- `EmailSearchPerformed`
