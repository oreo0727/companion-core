# Connectors

Phase 8 adds a read-only connector framework for user-owned external data snapshots.
Phase 9 extends that framework with read-only email snapshots.
Phase 12 adds OAuth-capable Google and Microsoft connector definitions.

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
- `FileDocumentSnapshot`
  Stores read-only file/document metadata materialized from Drive and OneDrive connections.

## Read-Only Boundary

This phase is intentionally non-destructive.

- no email sending
- no email deleting
- no email archiving
- no write-back to external systems
- no destructive sync behavior
- no connector deletes during import

The local calendar and local email connectors only create or update internal snapshots.
Google and Microsoft connector definitions are present for consent and token lifecycle; production read sync arrives in Phase 13.
Phase 13 implements read sync for Google Calendar, Google Drive, Gmail, Microsoft Calendar, OneDrive, and Outlook Mail.

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
- no cross-user file document retrieval

## Audit

The connector layer writes:

- `ConnectorConnected`
- `ConnectorSyncStarted`
- `ConnectorSyncCompleted`
- `CalendarEventsViewed`
- `EmailMessagesViewed`
- `EmailSearchPerformed`
- `OAuthAuthorizationStarted`
- `OAuthConsentGranted`
- `OAuthConsentRevoked`
- `ConnectorDisconnected`
