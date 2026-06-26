using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class VoiceInteraction
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid VoiceSessionId { get; set; }

    public VoiceInteractionType Type { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string? AudioReference { get; set; }

    public long? LatencyMs { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public VoiceSession? VoiceSession { get; set; }
}
