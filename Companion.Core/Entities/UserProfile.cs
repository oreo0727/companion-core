namespace Companion.Core.Entities;

public class UserProfile
{
    public Guid Id { get; set; }

    public Guid ApplicationUserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

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

    public ICollection<UserPreference> Preferences { get; set; } = new List<UserPreference>();

    public ICollection<NotificationPreference> NotificationPreferences { get; set; } = new List<NotificationPreference>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    public ICollection<StoredSecret> StoredSecrets { get; set; } = new List<StoredSecret>();

    public ICollection<KnowledgeSource> KnowledgeSources { get; set; } = new List<KnowledgeSource>();

    public ICollection<KnowledgeCollection> KnowledgeCollections { get; set; } = new List<KnowledgeCollection>();

    public ICollection<ConnectorConnection> ConnectorConnections { get; set; } = new List<ConnectorConnection>();

    public ICollection<ConnectorSyncRun> ConnectorSyncRuns { get; set; } = new List<ConnectorSyncRun>();

    public ICollection<OAuthAuthorizationRequest> OAuthAuthorizationRequests { get; set; } = new List<OAuthAuthorizationRequest>();

    public ICollection<OAuthConsentGrant> OAuthConsentGrants { get; set; } = new List<OAuthConsentGrant>();

    public ICollection<CalendarEventSnapshot> CalendarEventSnapshots { get; set; } = new List<CalendarEventSnapshot>();

    public ICollection<EmailMessageSnapshot> EmailMessageSnapshots { get; set; } = new List<EmailMessageSnapshot>();

    public ICollection<ToolExecution> ToolExecutions { get; set; } = new List<ToolExecution>();

    public ICollection<ToolPermission> ToolPermissions { get; set; } = new List<ToolPermission>();
}
