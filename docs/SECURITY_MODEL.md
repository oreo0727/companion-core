# Security Model

## Overview

Phase 5 introduces the trust layer for Companion Core:

- ASP.NET Core Identity-backed accounts
- JWT bearer authentication
- role-based authorization
- per-user data ownership
- encrypted secret storage
- durable audit events

The system is still intentionally narrow. It does not yet connect to Gmail, Calendar, voice, mobile, or external action connectors. The current goal is to make the existing planning and reasoning stack safe enough to attach those systems later.

## Identity Model

- `ApplicationUser` is the authentication record used by ASP.NET Core Identity.
- `UserProfile` is the companion-domain record that owns memories, conversations, tasks, goals, projects, approvals, agent runs, preferences, and audit events.
- Each `UserProfile` maps one-to-one to one `ApplicationUser` through `ApplicationUserId`.

For local development, migrations seed a default administrator account:

- email: `local.user@companion-core.local`
- password: `CompanionDev123!`

That account exists only to unblock local setup and smoke testing. It must not be reused outside local development.

## Authorization Rules

Authenticated users can access only their own:

- memories
- conversations and messages
- tasks
- goals
- projects
- approvals
- suggestions
- open loops
- agent runs
- audit events
- preferences

`Administrator` is currently reserved for platform-level operations such as `/api/settings/ai`.

Companion does not trust client-supplied profile ids for ownership. The API resolves the active profile from JWT claims and uses that server-side profile id when reading or writing user data.

## Approval Boundary

Risky actions still require approval records before any future connector execution layer can use them.

Current approval triggers remain deterministic:

- `send`
- `delete`
- `purchase`
- `schedule`

The approval record captures:

- requesting user
- source conversation and message
- action type
- reason
- payload
- risk level
- status
- timestamps

## Suggestion Safety Rule

AI output is not allowed to write directly into:

- `MemoryEntry`
- `Goal`
- `Project`
- `TaskItem`

AI-generated candidates must become pending suggestions first. Only deterministic rules or explicit approval flows can promote them into first-class user data.

## Secret Handling

`ISecretStore` stores encrypted secrets at rest in `StoredSecrets`.

- values are protected through ASP.NET Core Data Protection before persistence
- the abstraction is scoped for future API keys and connector tokens
- the API does not return raw stored secrets back to clients

This gives Companion a server-side place for sensitive credentials before external connectors are introduced.

OAuth access and refresh tokens are also encrypted with ASP.NET Core Data Protection before they are stored on connector connections. OAuth API responses expose connection status, scopes, subject, and expiry only.

## Audit Philosophy

Audit logging is intentionally scoped to events that materially affect trust:

- login
- logout
- memory created
- task created
- approval approved
- approval rejected
- settings changed
- preference changed

`AuditEvent` is append-oriented. The point is to preserve a readable trust trail, not to build a generic analytics event bus.

## Token Model

JWT tokens include:

- application user id
- user profile id
- email
- display name
- role claims
- security stamp

Logout rotates the user's security stamp. Existing bearer tokens are rejected after that point.
