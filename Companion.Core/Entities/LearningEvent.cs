namespace Companion.Core.Entities;

public class LearningEvent
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public string SourceId { get; set; } = string.Empty;

    public string Signal { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
