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

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    public DbSet<ConnectorAccount> ConnectorAccounts => Set<ConnectorAccount>();

    public DbSet<AiProviderConfiguration> AiProviderConfigurations => Set<AiProviderConfiguration>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<StoredSecret> StoredSecrets => Set<StoredSecret>();

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
            entity.HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(x => x.UserProfileId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(x => x.ConversationId)
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
