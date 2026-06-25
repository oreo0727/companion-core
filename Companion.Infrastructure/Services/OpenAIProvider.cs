using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Models;

namespace Companion.Infrastructure.Services;

public class OpenAIProvider(
    HttpClient httpClient,
    IAiProviderConfigurationService configurationService) : IAIProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ProviderName => AiProviderNames.OpenAI;

    public async Task<AiCompletionResult> CompleteAsync(
        AiCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var configuration = await configurationService.GetConfigurationAsync(ProviderName, cancellationToken)
            ?? throw new InvalidOperationException("OpenAI configuration was not found.");
        var apiKey = configurationService.GetApiKey(configuration);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        var payload = new
        {
            model = configuration.Model,
            messages = request.Messages.Select(x => new { role = x.Role, content = x.Content }),
            temperature = request.Temperature ?? (double)configuration.Temperature,
            max_tokens = request.MaxTokens ?? configuration.MaxTokens,
            response_format = request.ExpectJson ? new { type = "json_object" } : null
        };

        using var httpRequest = AiProviderExecution.CreateJsonRequest(
            HttpMethod.Post,
            configuration.ApiBaseUrl,
            "chat/completions",
            payload,
            JsonOptions);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var executionResult = await AiProviderExecution.SendAsync(
            httpClient,
            httpRequest,
            ProviderName,
            configuration.TimeoutSeconds,
            cancellationToken);

        using var document = JsonDocument.Parse(executionResult.Body);
        var root = document.RootElement;
        var choice = root.GetProperty("choices")[0];
        var content = choice.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        var usage = root.TryGetProperty("usage", out var usageElement)
            ? new AiUsage(
                usageElement.TryGetProperty("prompt_tokens", out var promptTokens) ? promptTokens.GetInt32() : 0,
                usageElement.TryGetProperty("completion_tokens", out var completionTokens) ? completionTokens.GetInt32() : 0)
            : new AiUsage(0, 0);

        return new AiCompletionResult(
            content,
            ProviderName,
            root.TryGetProperty("model", out var modelElement) ? modelElement.GetString() ?? configuration.Model : configuration.Model,
            usage,
            executionResult.LatencyMs,
            choice.TryGetProperty("finish_reason", out var finishReason) ? finishReason.GetString() : null);
    }
}
