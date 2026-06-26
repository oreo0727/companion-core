using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class VoiceSession
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConversationId { get; set; }

    public VoiceSessionStatus Status { get; set; }

    public string SpeechToTextProvider { get; set; } = string.Empty;

    public string TextToSpeechProvider { get; set; } = string.Empty;

    public bool IsWakeSession { get; set; }

    public string? WakePhrase { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime LastActivityUtc { get; set; }

    public DateTime? InterruptedUtc { get; set; }

    public DateTime? EndedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public Conversation? Conversation { get; set; }

    public ICollection<VoiceInteraction> Interactions { get; set; } = new List<VoiceInteraction>();
}
