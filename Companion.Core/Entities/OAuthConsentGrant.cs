namespace Companion.Core.Entities;

public class OAuthConsentGrant
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConnectorDefinitionId { get; set; }

    public Guid? ConnectorConnectionId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Scopes { get; set; } = string.Empty;

    public DateTime ConsentUtc { get; set; }

    public DateTime? RevokedUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ConnectorDefinition? ConnectorDefinition { get; set; }

    public ConnectorConnection? ConnectorConnection { get; set; }
}
