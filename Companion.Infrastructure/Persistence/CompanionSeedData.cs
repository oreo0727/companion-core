using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Microsoft.AspNetCore.Identity;

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
    public static readonly Guid ResponseStylePreferenceId = Guid.Parse("34f29331-b2bb-4bb3-a1f7-82d73af1ecc8");
    public static readonly Guid NotificationPreferenceId = Guid.Parse("535730f9-d610-4060-9282-f8af4de2c220");
    public static readonly Guid PersonalityPreferenceId = Guid.Parse("de65c017-8a0d-427e-a8b6-9fa383d63e1f");
    public static readonly Guid AdministratorRoleId = Guid.Parse("1fc75f52-55c1-4222-aeeb-77291e0c6c80");
    public static readonly Guid UserRoleId = Guid.Parse("0a56a52f-cf4b-4a39-9432-1aa4c03be59f");
    public static readonly Guid MemorySearchToolDefinitionId = Guid.Parse("ba1ae420-3338-40cb-b7be-b7a08b95fe7b");
    public static readonly Guid CreateTaskToolDefinitionId = Guid.Parse("d39d98cc-c066-44b5-bc05-6dc81c7dbf6c");
    public static readonly Guid GetBriefingToolDefinitionId = Guid.Parse("56ec1a59-1115-4da9-9292-c8a2609fe632");
    public static readonly Guid KnowledgeSearchToolDefinitionId = Guid.Parse("fb6c3e11-6546-4df0-bbf5-f974e0d307ec");
    public static readonly Guid CalendarEventsToolDefinitionId = Guid.Parse("0ddf4583-81b6-4e2d-a3d6-738066b13d8c");
    public static readonly Guid MemorySearchToolPermissionId = Guid.Parse("1a9f7783-8d03-4769-ab39-f9b8dc7bd3b4");
    public static readonly Guid CreateTaskToolPermissionId = Guid.Parse("b4608125-c91c-4a2a-ae17-68a4b0f4f6df");
    public static readonly Guid GetBriefingToolPermissionId = Guid.Parse("f2a6cdb9-212d-4f0f-92a1-0e2db84cf90f");
    public static readonly Guid KnowledgeSearchToolPermissionId = Guid.Parse("ab0c3178-4f4c-4634-b25d-fcdafb4fbb6c");
    public static readonly Guid CalendarEventsToolPermissionId = Guid.Parse("e1fc039a-1ca6-426b-9a9a-29873fe46f76");
    public static readonly Guid LocalCalendarConnectorDefinitionId = Guid.Parse("fb132d85-476e-48d2-81cb-4e6a1bf09cf5");

    public static readonly DateTime UserCreatedUtc = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime ConversationCreatedUtc = new(2026, 6, 19, 12, 5, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime MemoryCreatedUtc = new(2026, 6, 19, 12, 10, 0, DateTimeKind.Utc);
    public static readonly DateTime TaskCreatedUtc = new(2026, 6, 19, 12, 15, 0, DateTimeKind.Utc);
    public static readonly DateTime GoalCreatedUtc = new(2026, 6, 19, 12, 20, 0, DateTimeKind.Utc);
    public static readonly DateTime ProjectCreatedUtc = new(2026, 5, 1, 14, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime OpenLoopCreatedUtc = new(2026, 6, 19, 12, 25, 0, DateTimeKind.Utc);
    public static readonly DateTime AiProviderCreatedUtc = new(2026, 6, 19, 12, 30, 0, DateTimeKind.Utc);
    public static readonly DateTime ToolCreatedUtc = new(2026, 6, 25, 20, 0, 0, DateTimeKind.Utc);

    public static readonly UserProfile LocalUser = new()
    {
        Id = CompanionDefaults.LocalUserProfileId,
        ApplicationUserId = CompanionDefaults.LocalUserProfileId,
        DisplayName = "Local User",
        Email = "local.user@companion-core.local",
        CreatedUtc = UserCreatedUtc,
        UpdatedUtc = UserCreatedUtc
    };

    public static readonly ApplicationUser LocalApplicationUser = new()
    {
        Id = CompanionDefaults.LocalUserProfileId,
        UserName = "local.user@companion-core.local",
        NormalizedUserName = "LOCAL.USER@COMPANION-CORE.LOCAL",
        Email = "local.user@companion-core.local",
        NormalizedEmail = "LOCAL.USER@COMPANION-CORE.LOCAL",
        EmailConfirmed = true,
        DisplayName = "Local User",
        CreatedUtc = UserCreatedUtc,
        LastLoginUtc = null,
        SecurityStamp = "4e20587c-39c3-4e5f-8333-ec0d79678d0c",
        ConcurrencyStamp = "9a4dd6cc-5338-43fa-84de-366623f67835",
        PasswordHash = "AQAAAAIAAYagAAAAECezyM026CWB+FQT/V8PIpB583Fm8u9AKMVl9aYw2Mx9qhpUUS9xa9+0xRS1aveITA=="
    };

    public static readonly IdentityRole<Guid>[] Roles =
    [
        new IdentityRole<Guid>
        {
            Id = AdministratorRoleId,
            Name = SystemRoles.Administrator,
            NormalizedName = SystemRoles.Administrator.ToUpperInvariant(),
            ConcurrencyStamp = "b8ec6f5a-8694-4763-a342-e1eb31b8b5cb"
        },
        new IdentityRole<Guid>
        {
            Id = UserRoleId,
            Name = SystemRoles.User,
            NormalizedName = SystemRoles.User.ToUpperInvariant(),
            ConcurrencyStamp = "bfd21d45-ec66-47eb-9603-f6459c4d6348"
        }
    ];

    public static readonly IdentityUserRole<Guid>[] LocalUserRoles =
    [
        new()
        {
            UserId = CompanionDefaults.LocalUserProfileId,
            RoleId = AdministratorRoleId
        },
        new()
        {
            UserId = CompanionDefaults.LocalUserProfileId,
            RoleId = UserRoleId
        }
    ];

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

    public static readonly UserPreference[] UserPreferences =
    [
        new UserPreference
        {
            Id = ResponseStylePreferenceId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            PreferenceType = "ResponseStyle",
            Value = "Concise",
            CreatedUtc = UserCreatedUtc,
            UpdatedUtc = UserCreatedUtc
        },
        new UserPreference
        {
            Id = NotificationPreferenceId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            PreferenceType = "Notifications",
            Value = "ImportantOnly",
            CreatedUtc = UserCreatedUtc,
            UpdatedUtc = UserCreatedUtc
        },
        new UserPreference
        {
            Id = PersonalityPreferenceId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            PreferenceType = "AiPersonality",
            Value = "SupportivePragmatic",
            CreatedUtc = UserCreatedUtc,
            UpdatedUtc = UserCreatedUtc
        }
    ];

    public static readonly ToolDefinition[] ToolDefinitions =
    [
        new ToolDefinition
        {
            Id = MemorySearchToolDefinitionId,
            Name = ToolNames.MemorySearch,
            Description = "Search the authenticated user's saved memories.",
            Category = ToolCategories.Memory,
            RiskLevel = ToolRiskLevel.Low,
            RequiresApproval = false,
            Enabled = true,
            CreatedUtc = ToolCreatedUtc
        },
        new ToolDefinition
        {
            Id = CreateTaskToolDefinitionId,
            Name = ToolNames.CreateTask,
            Description = "Create a task for the authenticated user after approval.",
            Category = ToolCategories.Planning,
            RiskLevel = ToolRiskLevel.Medium,
            RequiresApproval = true,
            Enabled = true,
            CreatedUtc = ToolCreatedUtc
        },
        new ToolDefinition
        {
            Id = GetBriefingToolDefinitionId,
            Name = ToolNames.GetBriefing,
            Description = "Retrieve the authenticated user's current companion briefing.",
            Category = ToolCategories.Companion,
            RiskLevel = ToolRiskLevel.Low,
            RequiresApproval = false,
            Enabled = true,
            CreatedUtc = ToolCreatedUtc
        },
        new ToolDefinition
        {
            Id = KnowledgeSearchToolDefinitionId,
            Name = ToolNames.KnowledgeSearch,
            Description = "Search the authenticated user's imported knowledge documents.",
            Category = ToolCategories.Knowledge,
            RiskLevel = ToolRiskLevel.Low,
            RequiresApproval = false,
            Enabled = true,
            CreatedUtc = ToolCreatedUtc
        },
        new ToolDefinition
        {
            Id = CalendarEventsToolDefinitionId,
            Name = ToolNames.CalendarEvents,
            Description = "Retrieve upcoming calendar events for the authenticated user.",
            Category = ToolCategories.Calendar,
            RiskLevel = ToolRiskLevel.Low,
            RequiresApproval = false,
            Enabled = true,
            CreatedUtc = ToolCreatedUtc
        }
    ];

    public static readonly ToolPermission[] LocalUserToolPermissions =
    [
        new ToolPermission
        {
            Id = MemorySearchToolPermissionId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            ToolDefinitionId = MemorySearchToolDefinitionId,
            Allowed = true,
            CreatedUtc = ToolCreatedUtc
        },
        new ToolPermission
        {
            Id = CreateTaskToolPermissionId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            ToolDefinitionId = CreateTaskToolDefinitionId,
            Allowed = true,
            CreatedUtc = ToolCreatedUtc
        },
        new ToolPermission
        {
            Id = GetBriefingToolPermissionId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            ToolDefinitionId = GetBriefingToolDefinitionId,
            Allowed = true,
            CreatedUtc = ToolCreatedUtc
        },
        new ToolPermission
        {
            Id = KnowledgeSearchToolPermissionId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            ToolDefinitionId = KnowledgeSearchToolDefinitionId,
            Allowed = true,
            CreatedUtc = ToolCreatedUtc
        },
        new ToolPermission
        {
            Id = CalendarEventsToolPermissionId,
            UserProfileId = CompanionDefaults.LocalUserProfileId,
            ToolDefinitionId = CalendarEventsToolDefinitionId,
            Allowed = true,
            CreatedUtc = ToolCreatedUtc
        }
    ];

    public static readonly ConnectorDefinition[] ConnectorDefinitions =
    [
        new ConnectorDefinition
        {
            Id = LocalCalendarConnectorDefinitionId,
            Name = "Local Calendar",
            Provider = ConnectorProviders.LocalCalendar,
            Description = "Read-only local calendar connector that imports upcoming events from a JSON payload.",
            Category = ConnectorCategories.Calendar,
            SupportsOAuth = false,
            RiskLevel = ConnectorRiskLevel.Low,
            Enabled = true,
            CreatedUtc = ToolCreatedUtc
        }
    ];
}
