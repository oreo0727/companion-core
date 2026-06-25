using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Models;

namespace Companion.Infrastructure.Services;

public class AnthropicProvider(
    HttpClient httpClient,
    IAiProviderConfigurationService configurationService) : IAIProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ProviderName => AiProviderNames.Anthropic;

    public async Task<AiCompletionResult> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var configuration = await configurationService.GetConfigurationAsync(ProviderName, cancellationToken)
            ?? throw new InvalidOperationException("Anthropic configuration was not found.");
        var apiKey = configurationService.GetApiKey(configuration);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Anthropic API key is not configured.");
        }

        var systemPrompt = string.Join(
            "\n\n",
            request.Messages
                .Where(x => string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Content));
        var messagePayload = request.Messages
            .Where(x => !string.Equals(x.Role, "system", StringComparison.OrdinalIgnoreCase))
            .Select(x => new
            {
                role = string.Equals(x.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user",
                content = x.Content
            })
            .ToList();

        var payload = new
        {
            model = configuration.Model,
            system = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
            messages = messagePayload,
            temperature = request.Temperature ?? (double)configuration.Temperature,
            max_tokens = request.MaxTokens ?? configuration.MaxTokens
        };

        using var httpRequest = AiProviderExecution.CreateJsonRequest(
            HttpMethod.Post,
            configuration.ApiBaseUrl,
            "messages",
            payload,
            JsonOptions);
        httpRequest.Headers.Authorization = null;
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        var executionResult = await AiProviderExecution.SendAsync(
            httpClient,
            httpRequest,
            ProviderName,
            configuration.TimeoutSeconds,
            cancellationToken);

        using var document = JsonDocument.Parse(executionResult.Body);
        var root = document.RootElement;
        var contentBuilder = new StringBuilder();

        if (root.TryGetProperty("content", out var contentElement) &&
            contentElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in contentElement.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var typeElement) &&
                    string.Equals(typeElement.GetString(), "text", StringComparison.OrdinalIgnoreCase) &&
                    part.TryGetProperty("text", out var textElement))
                {
                    contentBuilder.Append(textElement.GetString());
                }
            }
        }

        var usage = root.TryGetProperty("usage", out var usageElement)
            ? new AiUsage(
                usageElement.TryGetProperty("input_tokens", out var promptTokens) ? promptTokens.GetInt32() : 0,
                usageElement.TryGetProperty("output_tokens", out var completionTokens) ? completionTokens.GetInt32() : 0)
            : new AiUsage(0, 0);

        return new AiCompletionResult(
            contentBuilder.ToString(),
            ProviderName,
            root.TryGetProperty("model", out var modelElement) ? modelElement.GetString() ?? configuration.Model : configuration.Model,
            usage,
            executionResult.LatencyMs,
            root.TryGetProperty("stop_reason", out var finishReason) ? finishReason.GetString() : null);
    }
}
