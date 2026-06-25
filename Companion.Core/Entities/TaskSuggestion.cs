using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class TaskSuggestion
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemPriority Priority { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public SuggestionStatus Status { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ReviewedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
