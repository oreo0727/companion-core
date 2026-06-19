using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class ProjectSuggestion
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int MentionCount { get; set; }

    public SuggestionStatus Status { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ReviewedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
