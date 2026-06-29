# Google Capability

The Google capability exposes Calendar, Gmail, Drive, and People through provider-independent capability interfaces.

## Surfaces

- Google Calendar syncs into `CalendarEventSnapshot`.
- Gmail syncs into `EmailMessageSnapshot`.
- Google Drive syncs into `FileDocumentSnapshot`.
- Google People syncs into `ContactSnapshot`.

The AI reasoning engine does not call Google APIs. It receives bounded context from capabilities:

- upcoming meetings
- important recent email
- recently opened documents
- relevant contacts

## Tools

- `GetCalendarEvents`
- `FindFreeTime`
- `SearchEmail`
- `ReadEmail`
- `CreateDraftEmail`
- `SearchDrive`
- `ReadDocument`
- `FindContact`

Legacy tools `CalendarEvents` and `EmailSearch` remain available and now use the same capability interfaces.

## Write Safety

`CreateDraftEmail` is medium risk and requires approval before execution. Email sending is not implemented.

## Web UI

- `/google-account` shows Google connection status, granted scopes, last sync, sync now, and disconnect.
- `/calendar` shows calendar snapshots.
- `/email` shows email snapshots.
- `/drive` shows file snapshots.
- `/contacts` shows contact snapshots.
