# Provider Model

Companion Core stores provider settings in the `AiProviderConfiguration` table.

## Stored Fields

- `Provider`
- `Model`
- `ApiBaseUrl`
- `ApiKeyEncrypted`
- `IsEnabled`
- `Temperature`
- `MaxTokens`
- `TimeoutSeconds`
- `CreatedUtc`
- `UpdatedUtc`

## Seeded Providers

- `OpenAI`
  Disabled by default, seeded with `gpt-4.1-mini`
- `Anthropic`
  Disabled by default, seeded with `claude-3-5-sonnet-latest`
- `Ollama`
  Enabled by default, seeded with `llama3`

## Runtime Selection

- The enabled row decides which provider implementation is used.
- Switching providers is a data change through `PUT /api/settings/ai`.
- Provider failures still leave a usable chat response because the reasoning engine falls back instead of crashing the request.
- API keys can be stored on the row or supplied through configuration:
  - `AiProviders:OpenAI:ApiKey`
  - `AiProviders:Anthropic:ApiKey`
  - `AiProviders:Ollama:ApiKey`
  - `OPENAI_API_KEY`
  - `ANTHROPIC_API_KEY`
  - `OLLAMA_API_KEY`

## Docker / Ollama

`docker-compose.yml` includes an optional `ollama` profile. Start it with:

```bash
docker compose --profile ollama up -d ollama
```

The default seeded base URL is `http://ollama:11434`. If the profile is not running, Companion falls back instead of crashing the chat pipeline.
