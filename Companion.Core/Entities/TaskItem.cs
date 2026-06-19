using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class TaskItem
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; }

    public TaskItemPriority Priority { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public Guid? SourceMessageId { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
