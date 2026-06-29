# Google OAuth

Google OAuth is handled through the existing OAuth framework.

Provider:

```text
Google
```

Connector providers:

```text
GoogleCalendar
Gmail
GoogleDrive
GooglePeople
```

Default scopes:

```text
openid
email
profile
https://www.googleapis.com/auth/calendar.readonly
https://www.googleapis.com/auth/gmail.readonly
https://www.googleapis.com/auth/gmail.compose
https://www.googleapis.com/auth/drive.readonly
https://www.googleapis.com/auth/contacts.readonly
```

Tokens are stored on `ConnectorConnection` using encrypted access and refresh token fields. OAuth grants are recorded in `OAuthConsentGrant`.

## Flow

1. `POST /api/oauth/Google/authorize`
2. User grants consent at Google.
3. `POST /api/oauth/Google/callback`
4. Connector sync uses the encrypted token.

## Disconnect

Use:

```text
DELETE /api/oauth/connections/{connectionId}
```

Disconnect clears encrypted tokens, marks the connection disconnected, and audit logs the revocation.
