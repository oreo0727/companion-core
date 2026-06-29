namespace Companion.Core.Entities;

public class ContactSnapshot
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConnectorConnectionId { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Organization { get; set; }

    public DateTime? BirthdayUtc { get; set; }

    public string? PhotoUrl { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ConnectorConnection? ConnectorConnection { get; set; }
}
