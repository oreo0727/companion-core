# Adaptive Learning

Phase 19 adds behavioral learning without model fine-tuning.

## What Is Tracked

Companion records `LearningEvent` rows for:

- accepted suggestions
- rejected suggestions
- ignored suggestions
- tool usage
- failed tool usage
- conversation ratings
- goal completion
- project completion
- preference evolution

`ConversationRating` stores explicit 1-5 ratings and optional comments for a conversation.

## Learning Profile

`GET /api/learning/profile` returns an aggregated profile for the authenticated user:

- suggestion acceptance/rejection/ignore counts
- tool success/failure counts
- conversation rating count and average
- completed goal/project counts
- preference evolution count
- strongest weighted signal groups

This profile is computed from durable events. It is not hidden model state.

## Safety

Learning events do not directly create memories, goals, projects, or tasks. Suggestions still require the pending suggestion workflow, and tools still go through permission and approval checks.

## API

- `GET /api/learning/profile`
- `GET /api/learning/events`
- `POST /api/learning/events`
- `POST /api/learning/ratings`
- `POST /api/suggestions/{id}/ignore`

Manual event recording is intended for internal workflows, tests, and future UI controls. It stores bounded metadata and writes an audit event.
