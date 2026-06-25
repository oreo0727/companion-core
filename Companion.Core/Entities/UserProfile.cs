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

    public ICollection<MemorySuggestion> MemorySuggestions { get; set; } = new List<MemorySuggestion>();

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public ICollection<TaskSuggestion> TaskSuggestions { get; set; } = new List<TaskSuggestion>();

    public ICollection<Goal> Goals { get; set; } = new List<Goal>();

    public ICollection<Project> Projects { get; set; } = new List<Project>();

    public ICollection<OpenLoop> OpenLoops { get; set; } = new List<OpenLoop>();

    public ICollection<GoalSuggestion> GoalSuggestions { get; set; } = new List<GoalSuggestion>();

    public ICollection<ProjectSuggestion> ProjectSuggestions { get; set; } = new List<ProjectSuggestion>();
}
