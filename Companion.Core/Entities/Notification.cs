using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class Notification
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public NotificationSeverity Severity { get; set; }

    public NotificationStatus Status { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ReadUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
