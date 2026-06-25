# AI Architecture

Phase 4 adds a provider-independent reasoning layer to Companion Core.

## Flow

`POST /api/chat` now follows this sequence:

1. Persist the user message into the conversation.
2. Build a bounded context window with recent messages, relevant memories, active work, open loops, pending approvals, and Chief Of Staff insights.
3. Run the `ChiefOfStaffReasoningEngine`.
4. Send the prompt through the enabled `IAIProvider`.
5. Extract memory, goal, project, and task candidates from the exchange.
6. Store candidates as suggestions only.
7. Persist the assistant reply and AgentRun telemetry.

## Main Components

- `IAIProvider`
  Implemented by `OpenAIProvider`, `AnthropicProvider`, and `OllamaProvider`.
- `IAiProviderConfigurationService`
  Loads and updates the provider settings stored in `AiProviderConfiguration`.
- `IContextBuilder`
  Creates the structured, limited context window for the current conversation.
- `IReasoningEngine`
  Produces the assistant reply, insights, and recommendations.
- `IMemoryExtractionService`
  Pulls candidate memories, goals, projects, and tasks from the exchange.
- `ISuggestionService`
  Stores suggestion records and materializes approved items into first-class entities.

## Settings And Approval Endpoints

- `GET /api/settings/ai`
- `PUT /api/settings/ai`
- `GET /api/suggestions`
- `POST /api/suggestions/{id}/approve`
- `POST /api/suggestions/{id}/reject`

## Safety Model

- Only one provider configuration can be enabled at a time.
- Suggestions are never auto-approved.
- AI-generated memories, goals, projects, and tasks must remain in pending suggestion state until explicitly approved.
- High-risk action language still routes through `ApprovalRequest`.
- Provider failures fall back to a deterministic reply so chat does not hard-fail.
- Provider/model choice and token/latency telemetry are captured on `AgentRun`.

## Observability

Chat V2 writes an `AgentRun` record with:

- provider
- model
- prompt tokens
- completion tokens
- latency
- fallback/error metadata
