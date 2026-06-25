namespace Companion.Core.Models;

public sealed record AiProviderConfigurationSummary(
    Guid Id,
    string Provider,
    string Model,
    string ApiBaseUrl,
    bool IsEnabled,
    decimal Temperature,
    int MaxTokens,
    int TimeoutSeconds,
    bool HasApiKey,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
