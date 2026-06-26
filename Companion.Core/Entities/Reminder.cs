using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class Reminder
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime DueUtc { get; set; }

    public ReminderStatus Status { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string? SourceId { get; set; }

    public Guid? NotificationId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public Notification? Notification { get; set; }
}
