# AI Architecture

Phase 6 keeps the provider-independent reasoning layer and extends it with internal tool requests.

## Flow

`POST /api/chat` now follows this sequence:

1. Persist the user message into the conversation.
2. Build a bounded context window with recent messages, relevant memories, active work, open loops, pending approvals, and Chief Of Staff insights.
3. Run the `ChiefOfStaffReasoningEngine`.
4. Send the prompt through the enabled `IAIProvider`.
5. Accept optional `toolRequests` from the reasoning payload.
6. Route tool requests through permission and approval checks before execution.
7. Extract memory, goal, project, and task candidates from the exchange.
8. Store candidates as suggestions only.
9. Persist the assistant reply, tool execution state, and `AgentRun` telemetry.

## Main Components

- `IAIProvider`
  Implemented by `OpenAIProvider`, `AnthropicProvider`, and `OllamaProvider`.
- `IAiProviderConfigurationService`
  Loads and updates the provider settings stored in `AiProviderConfiguration`.
- `IContextBuilder`
  Creates the structured, limited context window for the current conversation.
- `IReasoningEngine`
  Produces the assistant reply, insights, recommendations, and optional tool requests.
- `IMemoryExtractionService`
  Pulls candidate memories, goals, projects, and tasks from the exchange.
- `ISuggestionService`
  Stores suggestion records and materializes approved items into first-class entities.
- `IToolExecutor`
  Executes approved internal tools and records status, output, and audit state.

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
- AI-generated tool requests never bypass permission or approval checks.
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
