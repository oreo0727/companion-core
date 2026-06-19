using Companion.Core.Entities;
using Companion.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Persistence;

public class CompanionDbContext(DbContextOptions<CompanionDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<MemoryEntry> MemoryEntries => Set<MemoryEntry>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<OpenLoop> OpenLoops => Set<OpenLoop>();

    public DbSet<GoalSuggestion> GoalSuggestions => Set<GoalSuggestion>();

    public DbSet<ProjectSuggestion> ProjectSuggestions => Set<ProjectSuggestion>();

    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    public DbSet<ConnectorAccount> ConnectorAccounts => Set<ConnectorAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.CreatedUtc).IsRequired();
            entity.Property(x => x.UpdatedUtc).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
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
    }
}
