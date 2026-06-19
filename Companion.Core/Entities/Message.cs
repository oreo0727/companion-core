using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class Message
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public MessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public string? MetadataJson { get; set; }

    public int? TokensEstimate { get; set; }

    public Conversation? Conversation { get; set; }
}
