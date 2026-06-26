using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public sealed class CompanionOperatingSystemService(
    CompanionDbContext dbContext,
    IAgentRuntime agentRuntime,
    IAdaptiveLearningService learningService,
    IAuditService auditService,
    TimeProvider timeProvider) : ICompanionOperatingSystemService
{
    public async Task<IReadOnlyList<OperatingSystemRun>> GetRunsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OperatingSystemRuns
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperatingSystemRunResult> GenerateRunAsync(
        Guid userProfileId,
        GenerateOperatingSystemRunCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var periodStart = command.PeriodStartUtc ?? DefaultPeriodStart(command.RoutineType, now);
        var periodEnd = command.PeriodEndUtc ?? DefaultPeriodEnd(command.RoutineType, now);
        var snapshot = await BuildSnapshotAsync(userProfileId, periodStart, periodEnd, cancellationToken);
        var routineType = string.IsNullOrWhiteSpace(command.RoutineType)
            ? OperatingSystemRoutineTypes.DailyBriefing
            : command.RoutineType.Trim();

        var scheduledRun = await agentRuntime.QueueRunAsync(
            new QueueAgentRunCommand(
                AgentNames.ChiefOfStaff,
                $"Run {routineType} operating-system follow-up for period {periodStart:O} to {periodEnd:O}.",
                userProfileId,
                MetadataJson: JsonSerializer.Serialize(new
                {
                    kind = "operating-system-scheduled-agent",
                    routineType,
                    periodStart,
                    periodEnd
                })),
            cancellationToken);

        var run = new OperatingSystemRun
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            RoutineType = routineType,
            Status = OperatingSystemRunStatus.Scheduled,
            Title = BuildTitle(routineType, now),
            Summary = BuildSummary(routineType, snapshot),
            InsightsJson = JsonSerializer.Serialize(BuildInsights(snapshot)),
            ActionsJson = JsonSerializer.Serialize(BuildActions(routineType, snapshot)),
            ForecastJson = JsonSerializer.Serialize(BuildForecast(snapshot)),
            ScheduledAgentRunId = scheduledRun.Id,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            CreatedUtc = now,
            CompletedUtc = now
        };

        dbContext.OperatingSystemRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.OperatingSystemRunGenerated,
            nameof(OperatingSystemRun),
            run.Id.ToString(),
            $"Generated {routineType} operating-system run and scheduled AgentRun {scheduledRun.Id}.",
            cancellationToken);

        return new OperatingSystemRunResult(run, [scheduledRun]);
    }

    public async Task<OperatingSystemRunResult> OptimizeContextAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await GenerateRunAsync(
            userProfileId,
            new GenerateOperatingSystemRunCommand(OperatingSystemRoutineTypes.ContextOptimization),
            cancellationToken);
    }

    private async Task<OperatingSnapshot> BuildSnapshotAsync(
        Guid userProfileId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var learningProfile = await learningService.GetProfileAsync(userProfileId, cancellationToken);
        var openTasks = await dbContext.TaskItems
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.Status != TaskItemStatus.Completed && x.Status != TaskItemStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var overdueTasks = openTasks.Where(x => x.DueDateUtc is not null && x.DueDateUtc < now).ToList();
        var pendingApprovals = await dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.Status == ApprovalRequestStatus.Pending)
            .CountAsync(cancellationToken);
        var activeGoals = await dbContext.Goals
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.Status != GoalStatus.Completed && x.Status != GoalStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var activeProjects = await dbContext.Projects
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.Status != ProjectStatus.Completed && x.Status != ProjectStatus.Archived)
            .ToListAsync(cancellationToken);
        var upcomingEvents = await dbContext.CalendarEventSnapshots
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.EndUtc >= now && x.StartUtc <= now.AddDays(7))
            .CountAsync(cancellationToken);
        var importantEmails = await dbContext.EmailMessageSnapshots
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.ReceivedUtc >= now.AddDays(-7) && (!x.IsRead || x.HasAttachments || !x.IsAnswered))
            .CountAsync(cancellationToken);
        var memories = await dbContext.MemoryEntries
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && !x.IsArchived)
            .ToListAsync(cancellationToken);
        var conversationsToSummarize = await dbContext.Conversations
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.UpdatedUtc >= now.AddDays(-30))
            .CountAsync(cancellationToken);

        return new OperatingSnapshot(
            openTasks.Count,
            overdueTasks.Count,
            pendingApprovals,
            activeGoals.Count,
            activeProjects.Count,
            upcomingEvents,
            importantEmails,
            memories.Count,
            memories.Count(x => x.Importance <= 2 && x.CreatedUtc < now.AddDays(-90)),
            conversationsToSummarize,
            learningProfile);
    }

    private static string BuildSummary(string routineType, OperatingSnapshot snapshot)
    {
        return $"{routineType} generated with {snapshot.OpenTasks} open task(s), {snapshot.PendingApprovals} pending approval(s), {snapshot.ActiveGoals} active goal(s), {snapshot.ActiveProjects} active project(s), and {snapshot.UpcomingCalendarEvents} upcoming calendar event(s).";
    }

    private static IReadOnlyList<object> BuildInsights(OperatingSnapshot snapshot)
    {
        var insights = new List<object>
        {
            new { category = "Tasks", message = $"{snapshot.OpenTasks} open task(s), {snapshot.OverdueTasks} overdue." },
            new { category = "Approvals", message = $"{snapshot.PendingApprovals} approval(s) need review." },
            new { category = "Learning", message = $"{snapshot.LearningProfile.AcceptedSuggestions} accepted and {snapshot.LearningProfile.RejectedSuggestions} rejected suggestion(s)." }
        };

        if (snapshot.ImportantEmails > 0)
        {
            insights.Add(new { category = "Email", message = $"{snapshot.ImportantEmails} recent email snapshot(s) look important." });
        }

        if (snapshot.MemoryPruneCandidates > 0)
        {
            insights.Add(new { category = "Memory", message = $"{snapshot.MemoryPruneCandidates} low-importance old memory candidate(s) should be reviewed before pruning." });
        }

        return insights;
    }

    private static IReadOnlyList<object> BuildActions(string routineType, OperatingSnapshot snapshot)
    {
        var actions = new List<object>
        {
            new { action = "ReviewOpenTasks", count = snapshot.OpenTasks },
            new { action = "ReviewPendingApprovals", count = snapshot.PendingApprovals },
            new { action = "RefreshContext", routineType }
        };

        if (snapshot.OverdueTasks > 0)
        {
            actions.Add(new { action = "TriageOverdueTasks", count = snapshot.OverdueTasks });
        }

        if (snapshot.MemoryPruneCandidates > 0)
        {
            actions.Add(new { action = "ReviewMemoryPruningCandidates", count = snapshot.MemoryPruneCandidates });
        }

        return actions;
    }

    private static object BuildForecast(OperatingSnapshot snapshot)
    {
        return new
        {
            goalsAtRisk = Math.Max(0, snapshot.ActiveGoals - snapshot.OpenTasks / 4),
            projectsAtRisk = Math.Max(0, snapshot.ActiveProjects - snapshot.UpcomingCalendarEvents / 3),
            contextPressure = snapshot.MemoryCount + snapshot.ConversationsToSummarize + snapshot.ImportantEmails,
            suggestedReviewCadence = snapshot.PendingApprovals > 0 || snapshot.OverdueTasks > 0 ? "Daily" : "Weekly"
        };
    }

    private static string BuildTitle(string routineType, DateTime now)
    {
        return routineType switch
        {
            OperatingSystemRoutineTypes.MorningStartup => $"Morning Startup {now:yyyy-MM-dd}",
            OperatingSystemRoutineTypes.EveningRecap => $"Evening Recap {now:yyyy-MM-dd}",
            OperatingSystemRoutineTypes.WeeklyReview => $"Weekly Review {now:yyyy-MM-dd}",
            OperatingSystemRoutineTypes.MonthlyReview => $"Monthly Review {now:yyyy-MM}",
            _ => $"{routineType} {now:yyyy-MM-dd}"
        };
    }

    private static DateTime DefaultPeriodStart(string routineType, DateTime now)
    {
        return routineType switch
        {
            OperatingSystemRoutineTypes.WeeklyReview => now.Date.AddDays(-7),
            OperatingSystemRoutineTypes.MonthlyReview => now.Date.AddMonths(-1),
            OperatingSystemRoutineTypes.LongTermPlanning => now.Date.AddMonths(-3),
            _ => now.Date
        };
    }

    private static DateTime DefaultPeriodEnd(string routineType, DateTime now)
    {
        return routineType switch
        {
            OperatingSystemRoutineTypes.GoalForecast => now.Date.AddMonths(3),
            OperatingSystemRoutineTypes.ProjectForecast => now.Date.AddMonths(1),
            OperatingSystemRoutineTypes.LongTermPlanning => now.Date.AddMonths(12),
            _ => now.Date.AddDays(1)
        };
    }

    private sealed record OperatingSnapshot(
        int OpenTasks,
        int OverdueTasks,
        int PendingApprovals,
        int ActiveGoals,
        int ActiveProjects,
        int UpcomingCalendarEvents,
        int ImportantEmails,
        int MemoryCount,
        int MemoryPruneCandidates,
        int ConversationsToSummarize,
        LearningProfile LearningProfile);
}
