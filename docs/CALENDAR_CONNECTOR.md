# Calendar Read Connector

The first real connector is `LocalCalendar`.

## Provider

- provider: `LocalCalendar`
- category: `Calendar`
- OAuth: not required
- risk: `Low`

## Import Path

`POST /api/connectors/local-calendar/import`

This endpoint accepts a JSON payload of upcoming events and materializes them into `CalendarEventSnapshot` rows under a user-owned `ConnectorConnection`.

## Event Shape

Each imported event supports:

- `externalId`
- `title`
- `description`
- `location`
- `startUtc`
- `endUtc`
- `isAllDay`

If `externalId` is omitted, the connector derives one from the title and start time.

## Retrieval

Upcoming events are available through:

- `GET /api/calendar/events`
- `GET /api/companion/briefing`
- `CalendarEvents` tool
- reasoning context

## Briefing And Insight Rules

The Chief Of Staff layer inspects upcoming events for:

- busy day load
- deadline-like event titles
- missing locations
- overlapping events

The context builder prioritizes events happening:

1. today
2. tomorrow
3. within the next 7 days
