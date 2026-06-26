namespace Companion.Core.Entities;

public class ConversationRating
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConversationId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public Conversation? Conversation { get; set; }
}
