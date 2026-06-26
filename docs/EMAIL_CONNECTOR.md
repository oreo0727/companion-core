# Email Read Connector

Phase 9 adds `LocalEmail`, a safe read-only email connector.

## Boundaries

The email connector does not send, delete, archive, label, or mutate external email. It only imports JSON payloads into user-owned `EmailMessageSnapshot` rows.

## Provider

- provider: `LocalEmail`
- category: `Email`
- OAuth: not required
- risk: `Low`

## Import Path

`POST /api/connectors/local-email/import`

This endpoint accepts a JSON payload of message snapshots and materializes them under a user-owned `ConnectorConnection`.

## Message Shape

Each imported message supports:

- `externalId`
- `subject`
- `fromName`
- `fromAddress`
- `toAddresses`
- `preview`
- `body`
- `receivedUtc`
- `isRead`
- `hasAttachments`
- `isAnswered`

If `externalId` is omitted, the connector derives one from sender, subject, and received time.

## Retrieval

Email snapshots are available through:

- `GET /api/email/messages`
- `GET /api/email/search`
- `GET /api/companion/briefing`
- `EmailSearch` tool
- reasoning context

## Briefing And Insight Rules

The Chief Of Staff layer inspects important recent email for:

- unread-looking items
- urgent keywords
- bill, payment, invoice, due, deadline, and overdue language
- attachments
- unanswered messages

`isAnswered` is an imported snapshot field. The local connector does not infer full thread state.
