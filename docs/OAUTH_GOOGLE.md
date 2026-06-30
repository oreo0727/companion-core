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

## Google Cloud setup

Create a Google Cloud OAuth 2.0 **Web application** client and add this authorized redirect URI for local development:

```text
http://localhost:3000/oauth/google/callback
```

If you open the web app from another machine, add that host too, for example:

```text
http://192.168.4.191:3000/oauth/google/callback
http://100.71.8.121:3000/oauth/google/callback
```

The redirect URI must exactly match the browser address you use for the web app.

## UI setup

1. Sign in as an administrator.
2. Open `/google-account`.
3. Paste the Google OAuth client ID and client secret into **OAuth credentials** and save.
4. Click **Connect** on Google Calendar, Gmail, Drive, or People.
5. Approve the read-only scopes at Google.
6. Google returns to `/oauth/google/callback`, where Companion exchanges the code for tokens and stores them encrypted.
7. Return to `/google-account` and use **Sync now** to import snapshots.

## Flow

1. `POST /api/oauth/Google/authorize`
2. Companion creates a PKCE state request and returns the Google authorization URL.
3. User grants consent at Google.
4. Google redirects to `/oauth/google/callback`.
5. `POST /api/oauth/Google/callback` exchanges the authorization code for access and refresh tokens.
6. Connector sync uses the encrypted token.

The API also supports deterministic test callbacks by passing `accessToken` and `refreshToken` directly to `POST /api/oauth/Google/callback`; live browser OAuth does not use that shortcut.

## Disconnect

Use:

```text
DELETE /api/oauth/connections/{connectionId}
```

Disconnect clears encrypted tokens, marks the connection disconnected, and audit logs the revocation.
