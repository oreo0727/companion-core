namespace Companion.Core.Entities;

public class UserProfile
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public ICollection<MemoryEntry> MemoryEntries { get; set; } = new List<MemoryEntry>();

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
