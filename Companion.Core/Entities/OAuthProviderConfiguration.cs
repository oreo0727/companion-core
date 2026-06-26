namespace Companion.Core.Entities;

public class OAuthProviderConfiguration
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string AuthorizationEndpoint { get; set; } = string.Empty;

    public string TokenEndpoint { get; set; } = string.Empty;

    public string? RevocationEndpoint { get; set; }

    public string DefaultScopes { get; set; } = string.Empty;

    public string ClientIdSecretName { get; set; } = string.Empty;

    public string ClientSecretSecretName { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }
}
