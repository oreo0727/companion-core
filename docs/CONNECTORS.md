# Connectors

Phase 8 adds a read-only connector framework for user-owned external data snapshots.

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

## Read-Only Boundary

This phase is intentionally non-destructive.

- no email sending
- no write-back to external systems
- no destructive sync behavior
- no connector deletes during import

The local calendar connector only creates or updates internal snapshots.

## Registry And Sync Flow

`ConnectorDefinition -> ConnectorConnection -> IConnectorRegistry -> IConnectorSyncService -> ConnectorSyncRun`

Every sync run records:

- start time
- completion time
- status
- items synced
- error text when applicable

## Ownership

All connector connections and calendar snapshots belong to a `UserProfile`.

- no cross-user connector listing
- no cross-user event retrieval
- no shared connection state

## Audit

The connector layer writes:

- `ConnectorConnected`
- `ConnectorSyncStarted`
- `ConnectorSyncCompleted`
- `CalendarEventsViewed`
