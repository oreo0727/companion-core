using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;

namespace Companion.Infrastructure.Persistence;

public static class CompanionSeedData
{
    public static readonly Guid ConversationId = Guid.Parse("7d5359f4-09b2-4351-9c8d-c4c34b19d74f");
    public static readonly Guid MemoryPreferenceId = Guid.Parse("4f761be7-f7c7-4bd3-bff5-6f3aa08e09aa");
    public static readonly Guid MemoryProjectId = Guid.Parse("e550806f-384c-4104-84c9-f5cff8297900");
    public static readonly Guid MemoryToneId = Guid.Parse("de687616-b0c7-4715-9a31-c1e1fc792532");
    public static readonly Guid TaskReviewApiId = Guid.Parse("b7fd0e94-87ba-42ed-a0f7-f8ce8b2d4cc4");
    public static readonly Guid TaskApprovalFlowId = Guid.Parse("9995cd58-1841-4732-8c87-34a6334f6652");
    public static readonly Guid TaskAgentRuntimeId = Guid.Parse("0ec7d330-08fb-44fe-9b34-e0075ef75f8a");
    public static readonly Guid GoalPlanningLayerId = Guid.Parse("fa3f78ea-b16f-4b83-8d55-3371aa2c0d7e");
    public static readonly Guid ProjectChurchAppId = Guid.Parse("7efc59c5-27ec-48b8-9c67-f08a8da71d99");
    public static readonly Guid OpenLoopArchitectureId = Guid.Parse("5cddf563-fb88-4ce0-b64d-558867bd8b44");
    public static readonly Guid OpenAiProviderConfigurationId = Guid.Parse("3b678d7f-7d22-4ef2-a653-8a45b0b88011");
    public static readonly Guid AnthropicProviderConfigurationId = Guid.Parse("2d9e33d7-4386-4d20-8d2d-68ccdb554a7d");
    public static readonly Guid OllamaProviderConfigurationId = Guid.Parse("a65cdf3d-b2ee-44d8-9c81-729f60a7a31c");

    public static readonly DateTime UserCreatedUtc = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime ConversationCreatedUtc = new(2026, 6, 19, 12, 5, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime MemoryCreatedUtc = new(2026, 6, 19, 12, 10, 0, DateTimeKind.Utc);
    public static readonly DateTime TaskCreatedUtc = new(2026, 6, 19, 12, 15, 0, DateTimeKind.Utc);
    public static readonly DateTime GoalCreatedUtc = new(2026, 6, 19, 12, 20, 0, DateTimeKind.Utc);
    public static readonly DateTime ProjectCreatedUtc = new(2026, 5, 1, 14, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime OpenLoopCreatedUtc = new(2026, 6, 19, 12, 25, 0, DateTimeKind.Utc);
    public static readonly DateTime AiProviderCreatedUtc = new(2026, 6, 19, 12, 30, 0, DateTimeKind.Utc);

    public static readonly UserProfile LocalUser = new()
    {
        Id = CompanionDefaults.LocalUserProfileId,
        DisplayName = "Local User",
        Email = "local.user@companion-core.local",
        CreatedUtc = UserCreatedUtc,
        UpdatedUtc = UserCreatedUtc
    };

    public static readonly Conversation InitialConversation = new()
    {
        Id = ConversationId,
        UserProfileId = CompanionDefaults.LocalUserProfileId,
        Title = "Companion Core Onboarding",
        CreatedUtc = ConversationCreatedUtc,
        UpdatedUtc = ConversationCreatedUtc,
        LastMessageUtc = ConversationCreatedUtc,
        ActiveTopic = "Companion Core Onboarding"
    };

    public static readonly MemoryEntry[] MemoryEntries =
    [
        new MemoryEntry
        {
            Id = MemoryPreferenceId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Type = "Preference",
            Summary = "Prefers concise status updates",
            Content = "The local user prefers direct, high-signal summaries during development work.",
            Confidence = 0.93m,
            Source = "Seed",
            CreatedUtc = MemoryCreatedUtc,
            LastReferencedUtc = MemoryCreatedUtc,
            Importance = 4,
            Sensitivity = "Normal",
            ExpiresUtc = null,
            IsArchived = false
        },
        new MemoryEntry
        {
            Id = MemoryProjectId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Type = "Project",
            Summary = "Building Companion Core",
            Content = "Companion Core is the backend foundation for a private AI companion platform.",
            Confidence = 0.98m,
            Source = "Seed",
            CreatedUtc = MemoryCreatedUtc.AddMinutes(1),
            LastReferencedUtc = MemoryCreatedUtc.AddMinutes(1),
            Importance = 5,
            Sensitivity = "Normal",
            ExpiresUtc = null,
            IsArchived = false
        },
        new MemoryEntry
        {
            Id = MemoryToneId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Type = "Style",
            Summary = "Likes collaborative momentum",
            Content = "The local user responds well to supportive, practical progress updates while building software.",
            Confidence = 0.88m,
            Source = "Seed",
            CreatedUtc = MemoryCreatedUtc.AddMinutes(2),
            LastReferencedUtc = null,
            Importance = 3,
            Sensitivity = "Normal",
            ExpiresUtc = null,
            IsArchived = false
        }
    ];

    public static readonly TaskItem[] TaskItems =
    [
        new TaskItem
        {
            Id = TaskReviewApiId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Title = "Review the initial API surface",
            Description = "Confirm the core endpoints match the first Companion Core backend milestone.",
            Status = TaskItemStatus.Todo,
            Priority = TaskItemPriority.High,
            DueDateUtc = new DateTime(2026, 6, 20, 17, 0, 0, DateTimeKind.Utc),
            CreatedUtc = TaskCreatedUtc,
            SourceMessageId = null,
            CompletedUtc = null
        },
        new TaskItem
        {
            Id = TaskApprovalFlowId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Title = "Exercise the approval workflow",
            Description = "Create a sample Companion Core approval request and verify it can be approved or rejected.",
            Status = TaskItemStatus.InProgress,
            Priority = TaskItemPriority.Normal,
            DueDateUtc = new DateTime(2026, 6, 21, 17, 0, 0, DateTimeKind.Utc),
            CreatedUtc = TaskCreatedUtc.AddMinutes(1),
            SourceMessageId = null,
            CompletedUtc = null
        },
        new TaskItem
        {
            Id = TaskAgentRuntimeId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Title = "Observe worker-driven agent runs",
            Description = "Queue a pending Companion Core agent run and confirm the worker moves it to completed.",
            Status = TaskItemStatus.Todo,
            Priority = TaskItemPriority.High,
            DueDateUtc = new DateTime(2026, 6, 22, 17, 0, 0, DateTimeKind.Utc),
            CreatedUtc = TaskCreatedUtc.AddMinutes(2),
            SourceMessageId = null,
            CompletedUtc = null
        }
    ];

    public static readonly Goal[] Goals =
    [
        new Goal
        {
            Id = GoalPlanningLayerId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Title = "Ship the Chief Of Staff planning layer",
            Description = "Expand Companion Core from memory and task tracking into deterministic planning support.",
            Status = GoalStatus.Active,
            Priority = PlanningPriority.High,
            TargetDateUtc = new DateTime(2026, 6, 27, 17, 0, 0, DateTimeKind.Utc),
            CreatedUtc = GoalCreatedUtc,
            UpdatedUtc = GoalCreatedUtc
        }
    ];

    public static readonly Project[] Projects =
    [
        new Project
        {
            Id = ProjectChurchAppId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Title = "Church App",
            Description = "Resume progress on the church app after the earlier planning pass stalled.",
            Status = ProjectStatus.Active,
            Priority = PlanningPriority.Normal,
            CreatedUtc = ProjectCreatedUtc,
            UpdatedUtc = ProjectCreatedUtc
        }
    ];

    public static readonly OpenLoop[] OpenLoops =
    [
        new OpenLoop
        {
            Id = OpenLoopArchitectureId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            Title = "Architecture sign-off for outbound approvals",
            Description = "Waiting on architecture sign-off before enabling broader action execution flows.",
            Status = OpenLoopStatus.Waiting,
            CreatedUtc = OpenLoopCreatedUtc,
            ClosedUtc = null
        }
    ];

    public static readonly AiProviderConfiguration[] AiProviderConfigurations =
    [
        new AiProviderConfiguration
        {
            Id = OpenAiProviderConfigurationId,
            Provider = AiProviderNames.OpenAI,
            Model = "gpt-4.1-mini",
            ApiBaseUrl = "https://api.openai.com/v1",
            ApiKeyEncrypted = "",
            IsEnabled = false,
            Temperature = 0.4m,
            MaxTokens = 600,
            TimeoutSeconds = 30,
            CreatedUtc = AiProviderCreatedUtc,
            UpdatedUtc = AiProviderCreatedUtc
        },
        new AiProviderConfiguration
        {
            Id = AnthropicProviderConfigurationId,
            Provider = AiProviderNames.Anthropic,
            Model = "claude-3-5-sonnet-latest",
            ApiBaseUrl = "https://api.anthropic.com/v1",
            ApiKeyEncrypted = "",
            IsEnabled = false,
            Temperature = 0.4m,
            MaxTokens = 600,
            TimeoutSeconds = 30,
            CreatedUtc = AiProviderCreatedUtc,
            UpdatedUtc = AiProviderCreatedUtc
        },
        new AiProviderConfiguration
        {
            Id = OllamaProviderConfigurationId,
            Provider = AiProviderNames.Ollama,
            Model = "llama3",
            ApiBaseUrl = "http://ollama:11434",
            ApiKeyEncrypted = "",
            IsEnabled = true,
            Temperature = 0.3m,
            MaxTokens = 600,
            TimeoutSeconds = 30,
            CreatedUtc = AiProviderCreatedUtc,
            UpdatedUtc = AiProviderCreatedUtc
        }
    ];
}
