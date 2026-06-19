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

    public static readonly DateTime UserCreatedUtc = new(2026, 6, 19, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime ConversationCreatedUtc = new(2026, 6, 19, 12, 5, 0, DateTimeKind.Utc);
    public static readonly DateTime MemoryCreatedUtc = new(2026, 6, 19, 12, 10, 0, DateTimeKind.Utc);
    public static readonly DateTime TaskCreatedUtc = new(2026, 6, 19, 12, 15, 0, DateTimeKind.Utc);

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
            Description = "Create a sample approval request and verify it can be approved or rejected.",
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
            Description = "Queue a pending agent run and confirm the worker moves it to completed.",
            Status = TaskItemStatus.Todo,
            Priority = TaskItemPriority.High,
            DueDateUtc = new DateTime(2026, 6, 22, 17, 0, 0, DateTimeKind.Utc),
            CreatedUtc = TaskCreatedUtc.AddMinutes(2),
            SourceMessageId = null,
            CompletedUtc = null
        }
    ];
}
