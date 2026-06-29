using Companion.Core.Entities;
using Companion.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Persistence;

public class CompanionDbContext(DbContextOptions<CompanionDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MemoryEntry> MemoryEntries => Set<MemoryEntry>();

    public DbSet<MemorySuggestion> MemorySuggestions => Set<MemorySuggestion>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<TaskSuggestion> TaskSuggestions => Set<TaskSuggestion>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<OpenLoop> OpenLoops => Set<OpenLoop>();

    public DbSet<GoalSuggestion> GoalSuggestions => Set<GoalSuggestion>();

    public DbSet<ProjectSuggestion> ProjectSuggestions => Set<ProjectSuggestion>();

    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();

    public DbSet<AgentDefinition> AgentDefinitions => Set<AgentDefinition>();

    public DbSet<LearningEvent> LearningEvents => Set<LearningEvent>();

    public DbSet<ConversationRating> ConversationRatings => Set<ConversationRating>();

    public DbSet<OperatingSystemRun> OperatingSystemRuns => Set<OperatingSystemRun>();

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    public DbSet<ConnectorAccount> ConnectorAccounts => Set<ConnectorAccount>();

    public DbSet<AiProviderConfiguration> AiProviderConfigurations => Set<AiProviderConfiguration>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Reminder> Reminders => Set<Reminder>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<StoredSecret> StoredSecrets => Set<StoredSecret>();

    public DbSet<ConnectorDefinition> ConnectorDefinitions => Set<ConnectorDefinition>();

    public DbSet<ConnectorConnection> ConnectorConnections => Set<ConnectorConnection>();

    public DbSet<ConnectorSyncRun> ConnectorSyncRuns => Set<ConnectorSyncRun>();

    public DbSet<OAuthProviderConfiguration> OAuthProviderConfigurations => Set<OAuthProviderConfiguration>();

    public DbSet<OAuthAuthorizationRequest> OAuthAuthorizationRequests => Set<OAuthAuthorizationRequest>();

    public DbSet<OAuthConsentGrant> OAuthConsentGrants => Set<OAuthConsentGrant>();

    public DbSet<CalendarEventSnapshot> CalendarEventSnapshots => Set<CalendarEventSnapshot>();

    public DbSet<EmailMessageSnapshot> EmailMessageSnapshots => Set<EmailMessageSnapshot>();

    public DbSet<FileDocumentSnapshot> FileDocumentSnapshots => Set<FileDocumentSnapshot>();

    public DbSet<ContactSnapshot> ContactSnapshots => Set<ContactSnapshot>();

    public DbSet<HomeDeviceSnapshot> HomeDeviceSnapshots => Set<HomeDeviceSnapshot>();

    public DbSet<HomeSensorSnapshot> HomeSensorSnapshots => Set<HomeSensorSnapshot>();

    public DbSet<VoiceSession> VoiceSessions => Set<VoiceSession>();

    public DbSet<VoiceInteraction> VoiceInteractions => Set<VoiceInteraction>();

    public DbSet<KnowledgeSource> KnowledgeSources => Set<KnowledgeSource>();

    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();

    public DbSet<KnowledgeCollection> KnowledgeCollections => Set<KnowledgeCollection>();

    public DbSet<ToolDefinition> ToolDefinitions => Set<ToolDefinition>();

    public DbSet<ToolExecution> ToolExecutions => Set<ToolExecution>();

    public DbSet<ToolPermission> ToolPermissions => Set<ToolPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("ApplicationUsers");
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.LastLoginUtc);
            entity.HasOne(x => x.UserProfile)
                .WithOne(x => x.ApplicationUser)
                .HasForeignKey<UserProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.LocalApplicationUser);
        });

        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles").HasData(CompanionSeedData.Roles);
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles").HasData(CompanionSeedData.LocalUserRoles);
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ApplicationUserId).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.ApplicationUserId).IsUnique();
            entity.HasData(CompanionSeedData.LocalUser);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.Property(x => x.LastMessageUtc).IsRequired();
            entity.Property(x => x.ActiveTopic).HasMaxLength(200);
            entity.HasIndex(x => new { x.UserProfileId, x.LastMessageUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.Conversations)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.InitialConversation);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.TokensEstimate);
            entity.HasIndex(x => new { x.ConversationId, x.CreatedUtc });
            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MemoryEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.Confidence).HasPrecision(5, 4).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.Importance).IsRequired();
            entity.Property(x => x.Sensitivity).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ExpiresUtc);
            entity.Property(x => x.IsArchived).HasDefaultValue(false).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.IsArchived, x.Type });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.MemoryEntries)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.MemoryEntries);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Priority)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.CompletedUtc);
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.DueDateUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Message>()
                .WithMany()
                .HasForeignKey(x => x.SourceMessageId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasData(CompanionSeedData.TaskItems);
        });

        modelBuilder.Entity<MemorySuggestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.Confidence).HasPrecision(5, 4).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Importance).IsRequired();
            entity.Property(x => x.Sensitivity).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.MemorySuggestions)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskSuggestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Priority)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.TaskSuggestions)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Priority)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.TargetDateUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.Goals)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.Goals);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Priority)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.UpdatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.Projects)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.Projects);
        });

        modelBuilder.Entity<OpenLoop>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.OpenLoops)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.OpenLoops);
        });

        modelBuilder.Entity<GoalSuggestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.GoalSuggestions)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectSuggestion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.MentionCount).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.ProjectSuggestions)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DelegationReason).HasMaxLength(1000);
            entity.Property(x => x.AgentName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Input).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.Error).HasMaxLength(4000);
            entity.Property(x => x.MetadataJson);
            entity.Property(x => x.Provider).HasMaxLength(50);
            entity.Property(x => x.Model).HasMaxLength(200);
            entity.Property(x => x.TotalTokens);
            entity.Property(x => x.FallbackUsed).HasDefaultValue(false).IsRequired();
            entity.HasIndex(x => new { x.Status, x.CreatedUtc });
            entity.HasIndex(x => new { x.UserProfileId, x.AgentName, x.CreatedUtc });
            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.AgentDefinition)
                .WithMany(x => x.AgentRuns)
                .HasForeignKey(x => x.AgentDefinitionId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ParentAgentRun)
                .WithMany(x => x.ChildAgentRuns)
                .HasForeignKey(x => x.ParentAgentRunId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Prompt).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ToolNamesJson).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ContextPolicyJson).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.MemoryWeight).HasPrecision(5, 2).IsRequired();
            entity.Property(x => x.Enabled).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(CompanionSeedData.AgentDefinitions);
        });

        modelBuilder.Entity<LearningEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SourceId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Signal).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Weight).HasPrecision(6, 2).IsRequired();
            entity.Property(x => x.MetadataJson).HasMaxLength(4000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.EventType, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.LearningEvents)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConversationRating>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Rating).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(1000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.ConversationId, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.ConversationRatings)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Conversation)
                .WithMany()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OperatingSystemRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RoutineType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.InsightsJson).IsRequired();
            entity.Property(x => x.ActionsJson).IsRequired();
            entity.Property(x => x.ForecastJson).IsRequired();
            entity.Property(x => x.PeriodStartUtc).IsRequired();
            entity.Property(x => x.PeriodEndUtc).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.RoutineType, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.OperatingSystemRuns)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ScheduledAgentRun)
                .WithMany()
                .HasForeignKey(x => x.ScheduledAgentRunId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.RiskLevel).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.CreatedUtc });
            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Message>()
                .WithMany()
                .HasForeignKey(x => x.SourceMessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ConnectorAccount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.Provider, x.Status });
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PreferenceType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.PreferenceType }).IsUnique();
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.Preferences)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.UserPreferences);
        });

        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PreferenceType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.InAppEnabled).IsRequired();
            entity.Property(x => x.LeadTimeMinutes).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.PreferenceType }).IsUnique();
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.NotificationPreferences)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.NotificationPreferences);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Body).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Severity)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100);
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.MetadataJson).HasMaxLength(4000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.CreatedUtc });
            entity.HasIndex(x => new { x.UserProfileId, x.Type, x.EntityType, x.EntityId });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.DueUtc).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SourceId).HasMaxLength(100);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.DueUtc });
            entity.HasIndex(x => new { x.UserProfileId, x.SourceType, x.SourceId });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.Reminders)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Notification)
                .WithMany()
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.AuditEvents)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StoredSecret>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Scope).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EncryptedValue).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Scope, x.Name }).IsUnique();
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.StoredSecrets)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConnectorDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RiskLevel)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => x.Provider).IsUnique();
            entity.HasData(CompanionSeedData.ConnectorDefinitions);
        });

        modelBuilder.Entity<ConnectorConnection>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.AccessTokenEncrypted).HasMaxLength(8000);
            entity.Property(x => x.RefreshTokenEncrypted).HasMaxLength(8000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.ConnectorDefinitionId, x.DisplayName }).IsUnique();
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.ConnectorConnections)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorDefinition)
                .WithMany(x => x.Connections)
                .HasForeignKey(x => x.ConnectorDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConnectorSyncRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.StartedUtc).IsRequired();
            entity.Property(x => x.Error).HasMaxLength(4000);
            entity.HasIndex(x => new { x.UserProfileId, x.StartedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.ConnectorSyncRuns)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorConnection)
                .WithMany(x => x.SyncRuns)
                .HasForeignKey(x => x.ConnectorConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OAuthProviderConfiguration>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AuthorizationEndpoint).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.TokenEndpoint).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.RevocationEndpoint).HasMaxLength(1000);
            entity.Property(x => x.DefaultScopes).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ClientIdSecretName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ClientSecretSecretName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Enabled).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => x.Provider).IsUnique();
            entity.HasData(CompanionSeedData.OAuthProviderConfigurations);
        });

        modelBuilder.Entity<OAuthAuthorizationRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ConnectorProvider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.State).HasMaxLength(200).IsRequired();
            entity.Property(x => x.RedirectUri).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Scopes).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CodeVerifierEncrypted).HasMaxLength(8000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.ExpiresUtc).IsRequired();
            entity.HasIndex(x => x.State).IsUnique();
            entity.HasIndex(x => new { x.UserProfileId, x.Provider, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.OAuthAuthorizationRequests)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OAuthConsentGrant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Scopes).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ConsentUtc).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Provider, x.Subject });
            entity.HasIndex(x => new { x.UserProfileId, x.ConnectorConnectionId });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.OAuthConsentGrants)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorDefinition)
                .WithMany(x => x.OAuthConsentGrants)
                .HasForeignKey(x => x.ConnectorDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorConnection)
                .WithMany(x => x.OAuthConsentGrants)
                .HasForeignKey(x => x.ConnectorConnectionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CalendarEventSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.Location).HasMaxLength(500);
            entity.Property(x => x.StartUtc).IsRequired();
            entity.Property(x => x.EndUtc).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.ConnectorConnectionId, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.UserProfileId, x.StartUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.CalendarEventSnapshots)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorConnection)
                .WithMany(x => x.CalendarEvents)
                .HasForeignKey(x => x.ConnectorConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailMessageSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FromName).HasMaxLength(300);
            entity.Property(x => x.FromAddress).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ToAddresses).HasMaxLength(2000);
            entity.Property(x => x.Preview).HasMaxLength(1000);
            entity.Property(x => x.Body).HasMaxLength(12000);
            entity.Property(x => x.ReceivedUtc).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.ConnectorConnectionId, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.UserProfileId, x.ReceivedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.EmailMessageSnapshots)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorConnection)
                .WithMany(x => x.EmailMessages)
                .HasForeignKey(x => x.ConnectorConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FileDocumentSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(500).IsRequired();
            entity.Property(x => x.MimeType).HasMaxLength(300);
            entity.Property(x => x.WebUrl).HasMaxLength(1000);
            entity.Property(x => x.PreviewText).HasMaxLength(4000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.ConnectorConnectionId, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.UserProfileId, x.ModifiedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.FileDocumentSnapshots)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorConnection)
                .WithMany(x => x.FileDocuments)
                .HasForeignKey(x => x.ConnectorConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(300).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(500);
            entity.Property(x => x.Phone).HasMaxLength(100);
            entity.Property(x => x.Organization).HasMaxLength(300);
            entity.Property(x => x.PhotoUrl).HasMaxLength(1000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.ConnectorConnectionId, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.UserProfileId, x.DisplayName });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.ContactSnapshots)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorConnection)
                .WithMany(x => x.Contacts)
                .HasForeignKey(x => x.ConnectorConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HomeDeviceSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.Property(x => x.DeviceType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.State).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Room).HasMaxLength(200);
            entity.Property(x => x.CapabilitiesJson).HasMaxLength(4000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.ConnectorConnectionId, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.UserProfileId, x.DeviceType, x.State });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.HomeDeviceSnapshots)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorConnection)
                .WithMany(x => x.HomeDevices)
                .HasForeignKey(x => x.ConnectorConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HomeSensorSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalId).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.Property(x => x.SensorType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Unit).HasMaxLength(50);
            entity.Property(x => x.Room).HasMaxLength(200);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => new { x.ConnectorConnectionId, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.UserProfileId, x.SensorType });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.HomeSensorSnapshots)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ConnectorConnection)
                .WithMany(x => x.HomeSensors)
                .HasForeignKey(x => x.ConnectorConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VoiceSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.SpeechToTextProvider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TextToSpeechProvider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.WakePhrase).HasMaxLength(200);
            entity.Property(x => x.MetadataJson).HasMaxLength(4000);
            entity.Property(x => x.StartedUtc).IsRequired();
            entity.Property(x => x.LastActivityUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Status, x.LastActivityUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.VoiceSessions)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.VoiceSessions)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VoiceInteraction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Text).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.AudioReference).HasMaxLength(1000);
            entity.Property(x => x.MetadataJson).HasMaxLength(4000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.CreatedUtc });
            entity.HasIndex(x => new { x.VoiceSessionId, x.CreatedUtc });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.VoiceInteractions)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.VoiceSession)
                .WithMany(x => x.Interactions)
                .HasForeignKey(x => x.VoiceSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeSource>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Name });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.KnowledgeSources)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.MimeType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.KnowledgeSourceId, x.CreatedUtc });
            entity.HasOne(x => x.KnowledgeSource)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.KnowledgeSourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeChunk>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.ChunkIndex).IsRequired();
            entity.Property(x => x.MetadataJson).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.KnowledgeDocumentId, x.ChunkIndex }).IsUnique();
            entity.HasOne(x => x.KnowledgeDocument)
                .WithMany(x => x.Chunks)
                .HasForeignKey(x => x.KnowledgeDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeCollection>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.Name });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.KnowledgeCollections)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ToolDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RiskLevel)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.RequiresApproval).IsRequired();
            entity.Property(x => x.Enabled).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(CompanionSeedData.ToolDefinitions);
        });

        modelBuilder.Entity<ToolExecution>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.InputJson).IsRequired();
            entity.Property(x => x.OutputJson);
            entity.Property(x => x.Error).HasMaxLength(4000);
            entity.Property(x => x.StartedUtc).IsRequired();
            entity.Property(x => x.CompletedUtc);
            entity.HasIndex(x => new { x.UserProfileId, x.StartedUtc });
            entity.HasIndex(x => new { x.ToolDefinitionId, x.Status });
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.ToolExecutions)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ToolDefinition)
                .WithMany()
                .HasForeignKey(x => x.ToolDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AgentRun)
                .WithMany(x => x.ToolExecutions)
                .HasForeignKey(x => x.AgentRunId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ToolPermission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Allowed).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.HasIndex(x => new { x.UserProfileId, x.ToolDefinitionId }).IsUnique();
            entity.HasOne(x => x.UserProfile)
                .WithMany(x => x.ToolPermissions)
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ToolDefinition)
                .WithMany()
                .HasForeignKey(x => x.ToolDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasData(CompanionSeedData.LocalUserToolPermissions);
        });

        modelBuilder.Entity<AiProviderConfiguration>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ApiBaseUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ApiKeyEncrypted).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.IsEnabled).IsRequired();
            entity.Property(x => x.Temperature).HasPrecision(4, 3).IsRequired();
            entity.Property(x => x.MaxTokens).IsRequired();
            entity.Property(x => x.TimeoutSeconds).HasDefaultValue(30).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => x.Provider).IsUnique();
            entity.HasData(CompanionSeedData.AiProviderConfigurations);
        });
    }
}
