# Authentication

## Stack

Companion Core uses:

- ASP.NET Core Identity for account management
- JWT bearer tokens for API authentication
- role claims for authorization
- `ApplicationUser` plus `UserProfile` one-to-one linkage

## Endpoints

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`

`register` and `login` return:

- `accessToken`
- `expiresUtc`
- current profile snapshot
- current preferences
- capabilities

## Local Development Account

Migrations seed one development-only administrator:

- email: `local.user@companion-core.local`
- password: `CompanionDev123!`

This exists for smoke testing and local bootstrapping only.

## JWT Claims

Tokens carry:

- application user id
- user profile id
- email
- display name
- roles
- security stamp

The API validates the signing key, issuer, audience, expiration, and current security stamp on every authenticated request.

## Logout Behavior

Logout is server-enforced by rotating the Identity security stamp. That invalidates previously issued JWTs without introducing a separate token blacklist table.
