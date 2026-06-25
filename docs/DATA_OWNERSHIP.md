# Data Ownership

Every user-owned record in Companion Core belongs to a `UserProfile`.

That includes:

- conversations
- messages through conversation ownership
- memories
- memory suggestions
- task items
- task suggestions
- goals
- goal suggestions
- projects
- project suggestions
- open loops
- approvals
- agent runs
- preferences
- audit events
- stored secrets

## Enforcement

The API never trusts a client-supplied user profile id for owned resources.

Instead it:

1. authenticates the bearer token
2. resolves the authenticated `UserProfileId` from claims
3. applies that profile id to service calls and queries

Cross-user reads and writes should therefore fail closed:

- list endpoints return only the caller's data
- point lookups outside the caller's ownership return `404`
- write operations stamp the caller's profile id server-side

## Administrative Scope

`Administrator` does not currently bypass ownership for companion-domain data. The role is reserved for platform configuration, especially AI provider settings.
