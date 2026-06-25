namespace Companion.Core.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }

    public Guid? UserProfileId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
