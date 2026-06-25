namespace Companion.Core.Entities;

public class AiProviderConfiguration
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string ApiBaseUrl { get; set; } = string.Empty;

    public string ApiKeyEncrypted { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public decimal Temperature { get; set; }

    public int MaxTokens { get; set; }

    public int TimeoutSeconds { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
