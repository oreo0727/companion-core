# Mobile Application

Phase 15 adds a React Native app built with Expo.

## Scope

The mobile app is a first-party client for the existing Companion Core API. It does not add new backend product capabilities, external connectors, destructive actions, SMS, or push delivery.

## Features

- login with the existing JWT authentication API
- configurable API base URL for LAN or Tailscale testing
- biometric local session lock when the device supports enrolled biometrics
- briefing view
- dashboard view
- chat through `POST /api/chat`
- voice session flow through `POST /api/voice/sessions` and `POST /api/voice/sessions/{id}/conversation`
- task list
- approval list
- notification list
- offline cache for read-heavy mobile views

## Running

Install dependencies:

```bash
npm --prefix Companion.Mobile ci
```

Start Expo:

```bash
npm --prefix Companion.Mobile run start
```

For a physical phone, set the API URL in the login screen to a reachable host, such as a LAN or Tailscale address:

```text
http://192.168.4.191:8080
```

or:

```text
http://100.71.8.121:8080
```

The API must allow the origin used by the client. Native mobile requests do not use the browser CORS model, but Expo web previews do.

## Verification

Run:

```bash
npm --prefix Companion.Mobile run typecheck
```

The smoke script also installs and typechecks the mobile app.

## Boundaries

- Voice uses simulated transcript text input in this phase.
- No phone-call workflow is included.
- No push notification provider is included.
- No mobile-only permission model is added; all authorization remains enforced by the API.
