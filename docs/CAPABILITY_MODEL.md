# Capability Model

Capabilities are the provider-independent boundary between Companion reasoning/tools and external systems.

The reasoning engine and tools depend on:

- `ICalendarCapability`
- `IEmailCapability`
- `IFileCapability`
- `IPeopleCapability`

They must not call Google, Microsoft, Apple, IMAP, Dropbox, or local connector implementations directly.

Provider connectors sync external data into user-owned snapshots. Capabilities then read bounded, audited slices of those snapshots for context, tools, briefing, and dashboard surfaces.

## Safety Rules

- Read operations return user-owned snapshots only.
- Write operations must be approval-gated.
- Email draft creation is modeled as approval-required and does not send email.
- Context builders include selected relevant data, never entire inboxes, drives, or contact books.
- Connector sync and capability access are audit logged.

## Current Capability Providers

- Calendar: local calendar, Google Calendar, Microsoft Calendar.
- Email: local email, Gmail, Outlook Mail.
- Files: Google Drive, OneDrive.
- People: Google People.

Future providers should implement connectors that populate the same snapshot model and expose data through the existing capability interfaces.
