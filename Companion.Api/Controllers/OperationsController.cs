using System.Diagnostics;
using System.Text.Json;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api")]
public class OperationsController(
    CompanionDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAuditService auditService,
    IConfiguration configuration,
    IHostEnvironment environment,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("setup/status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SetupStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SetupStatusResponse>> GetSetupStatus(CancellationToken cancellationToken)
    {
        var userCount = await userManager.Users.CountAsync(cancellationToken);
        var hasAdministrator = await dbContext.UserRoles
            .Join(
                dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (_, role) => role.Name)
            .AnyAsync(roleName => roleName == SystemRoles.Administrator, cancellationToken);
        var canConnectToDatabase = await dbContext.Database.CanConnectAsync(cancellationToken);
        var configuredCorsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var jwtSigningKey = configuration["Jwt:SigningKey"] ?? string.Empty;
        var webBaseUrl = configuration["Companion:WebBaseUrl"] ?? configuredCorsOrigins.FirstOrDefault() ?? "http://localhost:3000";

        var checks = new List<SetupCheckResponse>
        {
            new("database", canConnectToDatabase ? "Ready" : "ActionRequired", canConnectToDatabase
                ? "PostgreSQL is reachable."
                : "PostgreSQL is not reachable from the API."),
            new("identity", userCount > 0 ? "Ready" : "ActionRequired", userCount > 0
                ? $"{userCount} user account(s) exist."
                : "Create the first administrator account."),
            new("administrator", hasAdministrator ? "Ready" : "ActionRequired", hasAdministrator
                ? "At least one administrator role assignment exists."
                : "Assign an administrator before daily use."),
            new("jwt", IsProductionLike() && IsWeakJwtKey(jwtSigningKey) ? "Warning" : "Ready", IsProductionLike() && IsWeakJwtKey(jwtSigningKey)
                ? "Replace the development JWT signing key before exposing this instance."
                : "JWT signing configuration is present."),
            new("cors", configuredCorsOrigins.Length > 0 ? "Ready" : "Warning", configuredCorsOrigins.Length > 0
                ? $"Configured web origins: {string.Join(", ", configuredCorsOrigins)}"
                : "No explicit CORS origins are configured.")
        };

        return Ok(new SetupStatusResponse(
            IsFirstRun: userCount == 0,
            UserCount: userCount,
            HasAdministrator: hasAdministrator,
            EnvironmentName: environment.EnvironmentName,
            ApiBaseUrl: $"{Request.Scheme}://{Request.Host}",
            WebBaseUrl: webBaseUrl,
            SeededLocalAdminEmail: CompanionSeedData.LocalUser.Email,
            Checks: checks));
    }

    [HttpGet("system/health")]
    [Authorize]
    [ProducesResponseType(typeof(SystemHealthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemHealthResponse>> GetHealth(CancellationToken cancellationToken)
    {
        var userProfileId = User.GetRequiredUserProfileId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var databaseOk = await dbContext.Database.CanConnectAsync(cancellationToken);
        var pendingAgentRuns = await dbContext.AgentRuns.CountAsync(x => x.UserProfileId == userProfileId && x.Status == AgentRunStatus.Pending, cancellationToken);
        var failedAgentRuns = await dbContext.AgentRuns.CountAsync(x => x.UserProfileId == userProfileId && x.Status == AgentRunStatus.Failed, cancellationToken);
        var pendingApprovals = await dbContext.ApprovalRequests.CountAsync(x => x.UserProfileId == userProfileId && x.Status == ApprovalRequestStatus.Pending, cancellationToken);
        var unreadNotifications = await dbContext.Notifications.CountAsync(x => x.UserProfileId == userProfileId && x.Status == NotificationStatus.Unread, cancellationToken);
        var enabledProviders = await dbContext.AiProviderConfigurations.CountAsync(x => x.IsEnabled, cancellationToken);
        var connectedConnectors = await dbContext.ConnectorConnections.CountAsync(x => x.UserProfileId == userProfileId && x.Status == ConnectorConnectionStatus.Connected, cancellationToken);
        var recentFailures = await BuildRecentFailuresAsync(userProfileId, cancellationToken);

        return Ok(new SystemHealthResponse(
            Status: databaseOk && failedAgentRuns == 0 ? "Healthy" : databaseOk ? "Degraded" : "Unhealthy",
            GeneratedUtc: now,
            UptimeSeconds: (long)(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds,
            EnvironmentName: environment.EnvironmentName,
            DatabaseOk: databaseOk,
            PendingAgentRuns: pendingAgentRuns,
            FailedAgentRuns: failedAgentRuns,
            PendingApprovals: pendingApprovals,
            UnreadNotifications: unreadNotifications,
            EnabledAiProviders: enabledProviders,
            ConnectedConnectors: connectedConnectors,
            RecentFailures: recentFailures));
    }

    [HttpGet("system/diagnostics")]
    [Authorize(Roles = SystemRoles.Administrator)]
    [ProducesResponseType(typeof(SystemDiagnosticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemDiagnosticsResponse>> GetDiagnostics(CancellationToken cancellationToken)
    {
        var userProfileId = User.GetRequiredUserProfileId();
        var providers = await dbContext.AiProviderConfigurations
            .AsNoTracking()
            .OrderBy(x => x.Provider)
            .Select(x => new ProviderDiagnosticResponse(
                x.Provider,
                x.Model,
                x.ApiBaseUrl,
                x.IsEnabled,
                x.TimeoutSeconds,
                x.UpdatedUtc))
            .ToListAsync(cancellationToken);
        var connectors = await dbContext.ConnectorDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .Select(x => new ConnectorDiagnosticResponse(
                x.Id,
                x.Name,
                x.Provider,
                x.Category,
                x.Enabled,
                x.SupportsOAuth,
                x.RiskLevel.ToString(),
                dbContext.ConnectorConnections.Count(connection => connection.UserProfileId == userProfileId && connection.ConnectorDefinitionId == x.Id),
                dbContext.ConnectorSyncRuns
                    .Where(run => run.UserProfileId == userProfileId && run.ConnectorConnection!.ConnectorDefinitionId == x.Id)
                    .OrderByDescending(run => run.StartedUtc)
                    .Select(run => run.Status.ToString())
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var counts = new Dictionary<string, int>
        {
            ["conversations"] = await dbContext.Conversations.CountAsync(x => x.UserProfileId == userProfileId, cancellationToken),
            ["messages"] = await dbContext.Messages.CountAsync(x => x.Conversation!.UserProfileId == userProfileId, cancellationToken),
            ["memories"] = await dbContext.MemoryEntries.CountAsync(x => x.UserProfileId == userProfileId, cancellationToken),
            ["tasks"] = await dbContext.TaskItems.CountAsync(x => x.UserProfileId == userProfileId, cancellationToken),
            ["goals"] = await dbContext.Goals.CountAsync(x => x.UserProfileId == userProfileId, cancellationToken),
            ["projects"] = await dbContext.Projects.CountAsync(x => x.UserProfileId == userProfileId, cancellationToken),
            ["approvals"] = await dbContext.ApprovalRequests.CountAsync(x => x.UserProfileId == userProfileId, cancellationToken),
            ["agentRuns"] = await dbContext.AgentRuns.CountAsync(x => x.UserProfileId == userProfileId, cancellationToken),
            ["knowledgeDocuments"] = await dbContext.KnowledgeDocuments.CountAsync(x => x.KnowledgeSource!.UserProfileId == userProfileId, cancellationToken),
            ["connectorConnections"] = await dbContext.ConnectorConnections.CountAsync(x => x.UserProfileId == userProfileId, cancellationToken)
        };

        return Ok(new SystemDiagnosticsResponse(
            EnvironmentName: environment.EnvironmentName,
            MachineName: Environment.MachineName,
            DotnetVersion: Environment.Version.ToString(),
            ProcessId: Environment.ProcessId,
            WorkingDirectory: Environment.CurrentDirectory,
            DatabaseProvider: dbContext.Database.ProviderName ?? "unknown",
            Counts: counts,
            Providers: providers,
            Connectors: connectors));
    }

    [HttpGet("system/logs")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<SystemLogEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SystemLogEntryResponse>>> GetLogs(CancellationToken cancellationToken)
    {
        var userProfileId = User.GetRequiredUserProfileId();
        var audits = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(40)
            .Select(x => new SystemLogEntryResponse(x.CreatedUtc, "Audit", x.EventType, x.Description, null))
            .ToListAsync(cancellationToken);
        var agentRuns = await dbContext.AgentRuns
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && (x.Status == AgentRunStatus.Failed || x.Error != null))
            .OrderByDescending(x => x.StartedUtc)
            .Take(20)
            .Select(x => new SystemLogEntryResponse(x.StartedUtc ?? x.CreatedUtc, "AgentRun", x.Status.ToString(), x.Input, x.Error))
            .ToListAsync(cancellationToken);
        var connectorRuns = await dbContext.ConnectorSyncRuns
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.Error != null)
            .OrderByDescending(x => x.StartedUtc)
            .Take(20)
            .Select(x => new SystemLogEntryResponse(x.StartedUtc, "ConnectorSync", x.Status.ToString(), "Connector sync failed.", x.Error))
            .ToListAsync(cancellationToken);

        return Ok(audits
            .Concat(agentRuns)
            .Concat(connectorRuns)
            .OrderByDescending(x => x.TimestampUtc)
            .Take(80)
            .ToList());
    }

    [HttpGet("system/smoke-test/status")]
    [Authorize(Roles = SystemRoles.Administrator)]
    [ProducesResponseType(typeof(SmokeTestStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<SmokeTestStatusResponse> GetSmokeTestStatus()
    {
        var scriptPath = Path.Combine(environment.ContentRootPath, "..", "scripts", "smoke-test.sh");
        var exists = System.IO.File.Exists(scriptPath);
        return Ok(new SmokeTestStatusResponse(
            ScriptFound: exists,
            ScriptPath: Path.GetFullPath(scriptPath),
            RecommendedCommand: "API_PORT=18081 RUN_ID=manual-$(date +%s) ./scripts/smoke-test.sh",
            Status: exists ? "ReadyToRun" : "MissingScript",
            Notes: exists
                ? "Run from the repository root. The API does not execute shell scripts for safety."
                : "scripts/smoke-test.sh was not found from the API content root."));
    }

    [HttpGet("system/backup/export")]
    [Authorize]
    [ProducesResponseType(typeof(CompanionBackupEnvelope), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanionBackupEnvelope>> ExportBackup(CancellationToken cancellationToken)
    {
        var userProfileId = User.GetRequiredUserProfileId();
        var profile = await dbContext.UserProfiles.AsNoTracking().FirstAsync(x => x.Id == userProfileId, cancellationToken);
        var backup = new CompanionBackupEnvelope(
            SchemaVersion: 1,
            ExportedUtc: timeProvider.GetUtcNow().UtcDateTime,
            Profile: new BackupProfile(profile.DisplayName, profile.Email),
            Preferences: await dbContext.UserPreferences.AsNoTracking()
                .Where(x => x.UserProfileId == userProfileId)
                .OrderBy(x => x.PreferenceType)
                .Select(x => new BackupPreference(x.PreferenceType, x.Value))
                .ToListAsync(cancellationToken),
            Memories: await dbContext.MemoryEntries.AsNoTracking()
                .Where(x => x.UserProfileId == userProfileId && !x.IsArchived)
                .OrderByDescending(x => x.CreatedUtc)
                .Select(x => new BackupMemory(x.Type, x.Summary, x.Content, x.Confidence, x.Importance, x.Sensitivity, x.ExpiresUtc))
                .ToListAsync(cancellationToken),
            Tasks: await dbContext.TaskItems.AsNoTracking()
                .Where(x => x.UserProfileId == userProfileId)
                .OrderByDescending(x => x.CreatedUtc)
                .Select(x => new BackupTask(x.Title, x.Description, x.Status, x.Priority, x.DueDateUtc, x.CompletedUtc))
                .ToListAsync(cancellationToken),
            Goals: await dbContext.Goals.AsNoTracking()
                .Where(x => x.UserProfileId == userProfileId)
                .OrderByDescending(x => x.CreatedUtc)
                .Select(x => new BackupGoal(x.Title, x.Description, x.Status, x.Priority, x.TargetDateUtc))
                .ToListAsync(cancellationToken),
            Projects: await dbContext.Projects.AsNoTracking()
                .Where(x => x.UserProfileId == userProfileId)
                .OrderByDescending(x => x.CreatedUtc)
                .Select(x => new BackupProject(x.Title, x.Description, x.Status, x.Priority))
                .ToListAsync(cancellationToken),
            Reminders: await dbContext.Reminders.AsNoTracking()
                .Where(x => x.UserProfileId == userProfileId)
                .OrderByDescending(x => x.CreatedUtc)
                .Select(x => new BackupReminder(x.Title, x.Description, x.DueUtc, x.Status, x.SourceType))
                .ToListAsync(cancellationToken));

        await auditService.WriteEventAsync(userProfileId, "BackupExported", "Backup", userProfileId.ToString(), "Exported a user-owned backup.", cancellationToken);
        return Ok(backup);
    }

    [HttpPost("system/backup/import")]
    [Authorize]
    [ProducesResponseType(typeof(BackupImportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BackupImportResponse>> ImportBackup(
        [FromBody] CompanionBackupEnvelope backup,
        CancellationToken cancellationToken)
    {
        if (backup.SchemaVersion != 1)
        {
            return BadRequest(new { error = $"Unsupported backup schema version {backup.SchemaVersion}." });
        }

        var userProfileId = User.GetRequiredUserProfileId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var imported = new Dictionary<string, int>();

        foreach (var preference in backup.Preferences)
        {
            var existing = await dbContext.UserPreferences
                .FirstOrDefaultAsync(x => x.UserProfileId == userProfileId && x.PreferenceType == preference.PreferenceType, cancellationToken);
            if (existing is null)
            {
                dbContext.UserPreferences.Add(new UserPreference
                {
                    Id = Guid.NewGuid(),
                    UserProfileId = userProfileId,
                    PreferenceType = TrimTo(preference.PreferenceType, 100),
                    Value = TrimTo(preference.Value, 4000),
                    CreatedUtc = now,
                    UpdatedUtc = now
                });
                Increment(imported, "preferences");
            }
            else
            {
                existing.Value = TrimTo(preference.Value, 4000);
                existing.UpdatedUtc = now;
            }
        }

        var existingMemoryKeys = await dbContext.MemoryEntries
            .Where(x => x.UserProfileId == userProfileId)
            .Select(x => x.Summary + "\n" + x.Content)
            .ToListAsync(cancellationToken);
        foreach (var memory in backup.Memories.Where(x => !existingMemoryKeys.Contains(x.Summary + "\n" + x.Content)))
        {
            dbContext.MemoryEntries.Add(new MemoryEntry
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Type = TrimTo(memory.Type, 100),
                Summary = TrimTo(memory.Summary, 500),
                Content = memory.Content,
                Confidence = Math.Clamp(memory.Confidence, 0, 1),
                Source = "BackupRestore",
                CreatedUtc = now,
                Importance = Math.Clamp(memory.Importance, 1, 5),
                Sensitivity = TrimTo(string.IsNullOrWhiteSpace(memory.Sensitivity) ? "Normal" : memory.Sensitivity, 32),
                ExpiresUtc = memory.ExpiresUtc,
                IsArchived = false
            });
            Increment(imported, "memories");
        }

        await ImportPlanningItemsAsync(userProfileId, backup, imported, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(userProfileId, "BackupImported", "Backup", userProfileId.ToString(), $"Imported backup content: {JsonSerializer.Serialize(imported)}", cancellationToken);

        return Ok(new BackupImportResponse(now, imported));
    }

    private async Task ImportPlanningItemsAsync(
        Guid userProfileId,
        CompanionBackupEnvelope backup,
        IDictionary<string, int> imported,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existingTaskTitles = await dbContext.TaskItems.Where(x => x.UserProfileId == userProfileId).Select(x => x.Title).ToListAsync(cancellationToken);
        foreach (var task in backup.Tasks.Where(x => !existingTaskTitles.Contains(x.Title)))
        {
            dbContext.TaskItems.Add(new TaskItem
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Title = TrimTo(task.Title, 200),
                Description = TrimTo(task.Description, 2000),
                Status = task.Status,
                Priority = task.Priority,
                DueDateUtc = task.DueDateUtc,
                CreatedUtc = now,
                CompletedUtc = task.CompletedUtc
            });
            Increment(imported, "tasks");
        }

        var existingGoalTitles = await dbContext.Goals.Where(x => x.UserProfileId == userProfileId).Select(x => x.Title).ToListAsync(cancellationToken);
        foreach (var goal in backup.Goals.Where(x => !existingGoalTitles.Contains(x.Title)))
        {
            dbContext.Goals.Add(new Goal
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Title = TrimTo(goal.Title, 200),
                Description = TrimTo(goal.Description, 2000),
                Status = goal.Status,
                Priority = goal.Priority,
                TargetDateUtc = goal.TargetDateUtc,
                CreatedUtc = now,
                UpdatedUtc = now
            });
            Increment(imported, "goals");
        }

        var existingProjectTitles = await dbContext.Projects.Where(x => x.UserProfileId == userProfileId).Select(x => x.Title).ToListAsync(cancellationToken);
        foreach (var project in backup.Projects.Where(x => !existingProjectTitles.Contains(x.Title)))
        {
            dbContext.Projects.Add(new Project
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Title = TrimTo(project.Title, 200),
                Description = TrimTo(project.Description, 2000),
                Status = project.Status,
                Priority = project.Priority,
                CreatedUtc = now,
                UpdatedUtc = now
            });
            Increment(imported, "projects");
        }

        var existingReminderTitles = await dbContext.Reminders.Where(x => x.UserProfileId == userProfileId).Select(x => x.Title).ToListAsync(cancellationToken);
        foreach (var reminder in backup.Reminders.Where(x => !existingReminderTitles.Contains(x.Title)))
        {
            dbContext.Reminders.Add(new Reminder
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Title = TrimTo(reminder.Title, 300),
                Description = TrimTo(reminder.Description, 2000),
                DueUtc = reminder.DueUtc,
                Status = reminder.Status,
                SourceType = TrimTo(string.IsNullOrWhiteSpace(reminder.SourceType) ? "BackupRestore" : reminder.SourceType, 100),
                CreatedUtc = now,
                UpdatedUtc = now
            });
            Increment(imported, "reminders");
        }
    }

    private async Task<IReadOnlyList<string>> BuildRecentFailuresAsync(Guid userProfileId, CancellationToken cancellationToken)
    {
        var agentFailures = await dbContext.AgentRuns
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.Error != null)
            .OrderByDescending(x => x.StartedUtc)
            .Take(3)
            .Select(x => $"AgentRun {x.Id}: {x.Error}")
            .ToListAsync(cancellationToken);
        var connectorFailures = await dbContext.ConnectorSyncRuns
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.Error != null)
            .OrderByDescending(x => x.StartedUtc)
            .Take(3)
            .Select(x => $"ConnectorSync {x.Id}: {x.Error}")
            .ToListAsync(cancellationToken);

        return agentFailures.Concat(connectorFailures).Take(5).ToList();
    }

    private bool IsProductionLike()
    {
        return environment.IsProduction() || string.Equals(environment.EnvironmentName, "Staging", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWeakJwtKey(string value)
    {
        return value.Length < 32 || value.Contains("development", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimTo(string? value, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static void Increment(IDictionary<string, int> values, string key)
    {
        values[key] = values.TryGetValue(key, out var current) ? current + 1 : 1;
    }
}

public sealed record SetupStatusResponse(
    bool IsFirstRun,
    int UserCount,
    bool HasAdministrator,
    string EnvironmentName,
    string ApiBaseUrl,
    string WebBaseUrl,
    string SeededLocalAdminEmail,
    IReadOnlyList<SetupCheckResponse> Checks);

public sealed record SetupCheckResponse(string Key, string Status, string Message);

public sealed record SystemHealthResponse(
    string Status,
    DateTime GeneratedUtc,
    long UptimeSeconds,
    string EnvironmentName,
    bool DatabaseOk,
    int PendingAgentRuns,
    int FailedAgentRuns,
    int PendingApprovals,
    int UnreadNotifications,
    int EnabledAiProviders,
    int ConnectedConnectors,
    IReadOnlyList<string> RecentFailures);

public sealed record SystemDiagnosticsResponse(
    string EnvironmentName,
    string MachineName,
    string DotnetVersion,
    int ProcessId,
    string WorkingDirectory,
    string DatabaseProvider,
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<ProviderDiagnosticResponse> Providers,
    IReadOnlyList<ConnectorDiagnosticResponse> Connectors);

public sealed record ProviderDiagnosticResponse(
    string Provider,
    string Model,
    string ApiBaseUrl,
    bool IsEnabled,
    int TimeoutSeconds,
    DateTime UpdatedUtc);

public sealed record ConnectorDiagnosticResponse(
    Guid Id,
    string Name,
    string Provider,
    string Category,
    bool Enabled,
    bool SupportsOAuth,
    string RiskLevel,
    int ConnectionCount,
    string? LastSyncStatus);

public sealed record SystemLogEntryResponse(
    DateTime TimestampUtc,
    string Source,
    string Level,
    string Message,
    string? Error);

public sealed record SmokeTestStatusResponse(
    bool ScriptFound,
    string ScriptPath,
    string RecommendedCommand,
    string Status,
    string Notes);

public sealed record BackupImportResponse(
    DateTime ImportedUtc,
    IReadOnlyDictionary<string, int> ImportedCounts);

public sealed record CompanionBackupEnvelope(
    int SchemaVersion,
    DateTime ExportedUtc,
    BackupProfile Profile,
    IReadOnlyList<BackupPreference> Preferences,
    IReadOnlyList<BackupMemory> Memories,
    IReadOnlyList<BackupTask> Tasks,
    IReadOnlyList<BackupGoal> Goals,
    IReadOnlyList<BackupProject> Projects,
    IReadOnlyList<BackupReminder> Reminders);

public sealed record BackupProfile(string DisplayName, string Email);

public sealed record BackupPreference(string PreferenceType, string Value);

public sealed record BackupMemory(
    string Type,
    string Summary,
    string Content,
    decimal Confidence,
    int Importance,
    string Sensitivity,
    DateTime? ExpiresUtc);

public sealed record BackupTask(
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskItemPriority Priority,
    DateTime? DueDateUtc,
    DateTime? CompletedUtc);

public sealed record BackupGoal(
    string Title,
    string? Description,
    GoalStatus Status,
    PlanningPriority Priority,
    DateTime? TargetDateUtc);

public sealed record BackupProject(
    string Title,
    string? Description,
    ProjectStatus Status,
    PlanningPriority Priority);

public sealed record BackupReminder(
    string Title,
    string? Description,
    DateTime DueUtc,
    ReminderStatus Status,
    string SourceType);
