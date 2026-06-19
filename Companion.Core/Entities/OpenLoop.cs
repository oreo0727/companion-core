using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class OpenLoop
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public OpenLoopStatus Status { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ClosedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
