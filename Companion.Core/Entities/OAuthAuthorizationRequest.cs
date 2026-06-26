namespace Companion.Core.Entities;

public class OAuthAuthorizationRequest
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ConnectorProvider { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string Scopes { get; set; } = string.Empty;

    public string? CodeVerifierEncrypted { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ExpiresUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
