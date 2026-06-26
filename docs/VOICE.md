# Voice Platform

Phase 14 adds internal voice architecture without building phone apps.

## Capabilities

- speech-to-text abstraction
- text-to-speech abstraction
- voice sessions
- wake sessions
- conversation history
- interruption tracking
- streaming-ready conversation responses

## Providers

Speech-to-text providers:

- OpenAI
- Azure
- LocalWhisper

Text-to-speech providers:

- OpenAI
- Azure
- LocalPiper

The current implementations are deterministic architecture adapters. They provide the provider boundary, session flow, audit events, and testability needed before real audio provider SDKs are wired in.

## API

- `GET /api/voice/providers`
- `POST /api/voice/wake`
- `POST /api/voice/sessions`
- `POST /api/voice/sessions/{id}/transcribe`
- `POST /api/voice/sessions/{id}/conversation`
- `POST /api/voice/sessions/{id}/speak`
- `POST /api/voice/sessions/{id}/interrupt`
- `POST /api/voice/sessions/{id}/end`
- `GET /api/voice/sessions/{id}/history`

## Persistence

- `VoiceSession` records provider choices, conversation linkage, wake state, status, interruption, and session timestamps.
- `VoiceInteraction` records transcription, user utterance, assistant speech, wake, and interruption events.

## Audit

- `VoiceSessionStarted`
- `VoiceTranscribed`
- `VoiceSpoken`
- `VoiceInterrupted`

## Boundaries

- No mobile app in this phase.
- No phone calling.
- No external microphone capture in the backend.
- Dangerous actions still route through the normal approval and tool runtime boundaries.
