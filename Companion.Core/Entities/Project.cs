using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class Project
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; }

    public PlanningPriority Priority { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
