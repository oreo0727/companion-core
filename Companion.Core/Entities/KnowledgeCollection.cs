namespace Companion.Core.Entities;

public class KnowledgeCollection
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
