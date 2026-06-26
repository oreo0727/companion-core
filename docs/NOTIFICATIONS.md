# Notifications And Reminders

Phase 10 adds internal notifications and reminders.

## Boundaries

This phase is in-app only.

- no SMS
- no mobile push
- no email sending
- no destructive external action

## Model

- `Notification`
  User-owned in-app item with type, title, body, severity, read state, source entity, and timestamps.
- `Reminder`
  User-owned scheduled item. The worker turns due reminders into in-app notifications.
- `NotificationPreference`
  User-owned preference row for reminder type, in-app enablement, and lead time.

## Reminder Sources

The reminder worker processes:

- manually scheduled reminders
- task due reminders
- pending approval reminders
- upcoming calendar event reminders

Automatic source reminders are deduplicated by user, source type, and source id.

## API

- `GET /api/notifications`
- `POST /api/notifications/{id}/read`
- `POST /api/reminders`
- `GET /api/reminders`

## Tools

- `CreateReminder`
- `ListNotifications`

Both tools are low risk because they only operate on internal in-app state.

## Audit

The system writes audit events for:

- reminder creation
- notification reads
