namespace Companion.Core.Entities;

public class Conversation
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public DateTime LastMessageUtc { get; set; }

    public string? ActiveTopic { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
