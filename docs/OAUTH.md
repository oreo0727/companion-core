# OAuth Foundation

Phase 12 adds OAuth infrastructure for read-only connector authorization.

## Providers

Seeded OAuth providers:

- Google
- Microsoft

Provider configuration stores authorization, token, and optional revocation endpoints. Client IDs and client secrets are referenced by secret-store names so runtime secrets can be stored encrypted outside source code.

## Connector Support

OAuth-capable connector definitions are seeded for:

- Google Calendar
- Google Drive
- Gmail
- Microsoft Calendar
- OneDrive
- Outlook Mail

Phase 12 only manages authorization and consent lifecycle. Phase 13 implements production read sync for these connectors.

## Lifecycle

1. `GET /api/oauth/providers` returns configured OAuth providers.
2. `POST /api/oauth/{provider}/authorize` creates a short-lived authorization request with state and PKCE challenge.
3. Browser/user consent occurs at the returned authorization URL.
4. `POST /api/oauth/{provider}/callback` completes the connection and stores access/refresh tokens encrypted at rest.
5. `GET /api/oauth/connections` lists user-owned OAuth grants without exposing tokens.
6. `DELETE /api/oauth/connections/{connectionId}` disconnects the connector, clears encrypted token fields, and marks the consent grant revoked.

## Security Rules

- OAuth state is unique and expires after 10 minutes.
- Access and refresh tokens are encrypted with ASP.NET Core Data Protection.
- Tokens are never returned by API responses.
- Consent grants are scoped to `UserProfileId`.
- No user can see or revoke another user's OAuth connection.
- All authorization, grant, and revoke events are audited.

## Audit Events

- `OAuthAuthorizationStarted`
- `OAuthConsentGranted`
- `OAuthConsentRevoked`
- `ConnectorDisconnected`
