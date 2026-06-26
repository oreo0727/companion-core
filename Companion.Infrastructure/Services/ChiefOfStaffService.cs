using System.Text.RegularExpressions;
using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public partial class ChiefOfStaffService(
    CompanionDbContext dbContext,
    IGoalService goalService,
    IProjectService projectService,
    IOpenLoopService openLoopService,
    IConnectorSyncService connectorSyncService,
    INotificationService notificationService,
    TimeProvider timeProvider) : IChiefOfStaffService
{
    private static readonly string[] GoalMarkers =
    [
        "i want to",
        "my goal is",
        "i am trying to"
    ];

    private static readonly string[] OpenLoopMarkers =
    [
        "need to",
        "still haven't",
        "waiting on"
    ];

    private static readonly HashSet<string> ProjectStopWords =
    [
        "about",
        "after",
        "before",
        "build",
        "building",
        "can",
        "create",
        "exercise",
        "follow",
        "from",
        "have",
        "launch",
        "need",
        "observe",
        "please",
        "queue",
        "review",
        "send",
        "ship",
        "still",
        "that",
        "the",
        "this",
        "today",
        "tomorrow",
        "waiting",
        "want",
        "week",
        "with"
    ];

    public async Task<ChiefOfStaffAnalysisResult> AnalyzeMessageAsync(
        Guid userProfileId,
        Message message,
        CancellationToken cancellationToken = default)
    {
        var createdOpenLoops = new List<OpenLoop>();
        var goalSuggestions = new List<GoalSuggestion>();
        var projectSuggestions = new List<ProjectSuggestion>();

        if (TryBuildGoalSuggestion(message.Content, out var goalSuggestionCommand))
        {
            var suggestion = await goalService.CaptureGoalSuggestionAsync(
                userProfileId,
                goalSuggestionCommand,
                cancellationToken);

            if (suggestion is not null)
            {
                goalSuggestions.Add(suggestion);
            }
        }

        if (TryBuildOpenLoop(message.Content, out var openLoopCommand))
        {
            var openLoop = await openLoopService.CaptureOpenLoopAsync(
                userProfileId,
                openLoopCommand,
                cancellationToken);

            if (openLoop is not null)
            {
                createdOpenLoops.Add(openLoop);
            }
        }

        var recentUserMessages = await GetRecentUserMessagesAsync(userProfileId, 24, cancellationToken);
        foreach (var projectSuggestionCommand in BuildProjectSuggestionCommands(recentUserMessages))
        {
            var suggestion = await projectService.CaptureProjectSuggestionAsync(
                userProfileId,
                projectSuggestionCommand,
                cancellationToken);

            if (suggestion is not null &&
                projectSuggestions.All(x => x.Id != suggestion.Id))
            {
                projectSuggestions.Add(suggestion);
            }
        }

        var context = await LoadContextAsync(userProfileId, cancellationToken);
        var insights = BuildInsights(context, timeProvider.GetUtcNow().UtcDateTime)
            .Take(4)
            .ToList();

        return new ChiefOfStaffAnalysisResult(
            createdOpenLoops,
            goalSuggestions,
            projectSuggestions,
            insights);
    }

    public async Task<CompanionBriefing> GetBriefingAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(userProfileId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new CompanionBriefing(
            context.OpenTasks,
            context.PendingApprovals,
            context.OpenTasks
                .Where(x => x.DueDateUtc is not null && x.DueDateUtc < now)
                .OrderBy(x => x.DueDateUtc)
                .ToList(),
            context.UpcomingReminders,
            context.RecentMemories,
            context.Goals,
            context.Projects,
            context.UpcomingCalendarEvents,
            context.ImportantRecentEmails,
            context.OpenLoops,
            context.ProjectSuggestions,
            context.GoalSuggestions,
            BuildInsights(context, now).Take(6).ToList());
    }

    public async Task<CompanionDashboard> GetDashboardAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(userProfileId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new CompanionDashboard(
            context.Projects.Count,
            context.Goals.Count,
            context.OpenLoops.Count,
            context.PendingApprovals.Count,
            context.UnreadNotifications.Count,
            context.UpcomingReminders.Count,
            BuildInsights(context, now).Take(5).ToList());
    }

    private async Task<IReadOnlyList<Message>> GetRecentUserMessagesAsync(
        Guid userProfileId,
        int count,
        CancellationToken cancellationToken)
    {
        var userMessages = await (
            from message in dbContext.Messages.AsNoTracking()
            join conversation in dbContext.Conversations.AsNoTracking() on message.ConversationId equals conversation.Id
            where conversation.UserProfileId == userProfileId && message.Role == MessageRole.User
            orderby message.CreatedUtc descending
            select message)
            .Take(Math.Max(count, 1))
            .ToListAsync(cancellationToken);

        userMessages.Reverse();
        return userMessages;
    }

    private async Task<PlanningContext> LoadContextAsync(
        Guid userProfileId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var memoryWindow = await dbContext.MemoryEntries
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                !x.IsArchived &&
                (x.ExpiresUtc == null || x.ExpiresUtc > now))
            .OrderByDescending(x => x.LastReferencedUtc ?? x.CreatedUtc)
            .ToListAsync(cancellationToken);

        var recentMessages = await (
            from message in dbContext.Messages.AsNoTracking()
            join conversation in dbContext.Conversations.AsNoTracking() on message.ConversationId equals conversation.Id
            where conversation.UserProfileId == userProfileId
            orderby message.CreatedUtc descending
            select message)
            .Take(60)
            .ToListAsync(cancellationToken);

        recentMessages.Reverse();

        return new PlanningContext(
            await dbContext.TaskItems
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId == userProfileId &&
                    x.Status != TaskItemStatus.Completed &&
                    x.Status != TaskItemStatus.Cancelled)
                .OrderBy(x => x.DueDateUtc)
                .ThenByDescending(x => x.Priority)
                .ThenByDescending(x => x.CreatedUtc)
                .ToListAsync(cancellationToken),
            await dbContext.ApprovalRequests
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId == userProfileId &&
                    x.Status == ApprovalRequestStatus.Pending)
                .OrderByDescending(x => x.CreatedUtc)
                .ToListAsync(cancellationToken),
            memoryWindow.Take(5).ToList(),
            await dbContext.Goals
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId == userProfileId &&
                    x.Status != GoalStatus.Completed &&
                    x.Status != GoalStatus.Cancelled)
                .OrderBy(x => x.TargetDateUtc)
                .ThenByDescending(x => x.Priority)
                .ThenByDescending(x => x.UpdatedUtc)
                .ToListAsync(cancellationToken),
            await dbContext.Projects
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId == userProfileId &&
                    x.Status != ProjectStatus.Completed &&
                    x.Status != ProjectStatus.Archived)
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.UpdatedUtc)
                .ToListAsync(cancellationToken),
            await connectorSyncService.GetUpcomingCalendarEventsAsync(
                userProfileId,
                daysAhead: 7,
                audit: false,
                cancellationToken: cancellationToken),
            await connectorSyncService.GetRecentEmailMessagesAsync(
                userProfileId,
                daysBack: 14,
                limit: 12,
                audit: false,
                cancellationToken: cancellationToken),
            await notificationService.GetUpcomingRemindersAsync(
                userProfileId,
                daysAhead: 7,
                cancellationToken: cancellationToken),
            await notificationService.GetNotificationsAsync(
                userProfileId,
                includeRead: false,
                cancellationToken: cancellationToken),
            await dbContext.OpenLoops
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId == userProfileId &&
                    x.Status != OpenLoopStatus.Closed)
                .OrderByDescending(x => x.Status == OpenLoopStatus.Waiting)
                .ThenByDescending(x => x.CreatedUtc)
                .ToListAsync(cancellationToken),
            await dbContext.ProjectSuggestions
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId == userProfileId &&
                    x.Status == SuggestionStatus.Pending)
                .OrderByDescending(x => x.MentionCount)
                .ThenByDescending(x => x.CreatedUtc)
                .ToListAsync(cancellationToken),
            await dbContext.GoalSuggestions
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId == userProfileId &&
                    x.Status == SuggestionStatus.Pending)
                .OrderByDescending(x => x.CreatedUtc)
                .ToListAsync(cancellationToken),
            memoryWindow,
            recentMessages);
    }

    private static IReadOnlyList<CompanionInsight> BuildInsights(PlanningContext context, DateTime now)
    {
        var insights = new List<CompanionInsight>();
        var topics = BuildPlanningTopics(context);

        foreach (var topic in topics)
        {
            var relatedTaskCount = context.OpenTasks
                .Count(x => TopicMatches($"{x.Title} {x.Description}", topic));

            if (relatedTaskCount >= 2)
            {
                insights.Add(new CompanionInsight(
                    "Focus",
                    $"You have {relatedTaskCount} open tasks related to {topic}.",
                    88));
            }

            var weeklyMentions = context.RecentMessages
                .Count(x =>
                    x.Role == MessageRole.User &&
                    x.CreatedUtc >= now.AddDays(-7) &&
                    TopicMatches(x.Content, topic));

            if (weeklyMentions >= 3)
            {
                insights.Add(new CompanionInsight(
                    "Momentum",
                    $"You mentioned {topic} {weeklyMentions} times this week.",
                    82));
            }
        }

        if (context.PendingApprovals.Count == 1)
        {
            insights.Add(new CompanionInsight(
                "Blocked",
                "There is an approval request blocking progress.",
                95));
        }
        else if (context.PendingApprovals.Count > 1)
        {
            insights.Add(new CompanionInsight(
                "Blocked",
                $"There are {context.PendingApprovals.Count} approval requests that may be blocking progress.",
                97));
        }

        foreach (var project in context.Projects)
        {
            var lastInteractionUtc = ResolveLastInteraction(project, context);
            var inactiveDays = (now - lastInteractionUtc).TotalDays;

            if (inactiveDays >= 30)
            {
                insights.Add(new CompanionInsight(
                    "Forgotten",
                    $"You have not interacted with {project.Title} in {(int)inactiveDays} days.",
                    85));
            }
        }

        foreach (var goal in context.Goals.Where(x => x.TargetDateUtc is not null))
        {
            var targetDateUtc = goal.TargetDateUtc!.Value;
            if (targetDateUtc < now)
            {
                insights.Add(new CompanionInsight(
                    "Deadline",
                    $"Goal '{goal.Title}' is past its target date.",
                    93));
            }
            else if (targetDateUtc <= now.AddDays(7))
            {
                insights.Add(new CompanionInsight(
                    "Deadline",
                    $"Goal '{goal.Title}' is due within the next 7 days.",
                    76));
            }
        }

        if (context.OpenLoops.Count >= 2)
        {
            insights.Add(new CompanionInsight(
                "Load",
                $"You are carrying {context.OpenLoops.Count} open loops right now.",
                72));
        }

        var overdueTasks = context.OpenTasks
            .Where(x => x.DueDateUtc is not null && x.DueDateUtc < now)
            .ToList();
        if (overdueTasks.Count > 0)
        {
            insights.Add(new CompanionInsight(
                "Deadline",
                $"You have {overdueTasks.Count} overdue task(s).",
                90));
        }

        if (context.UpcomingReminders.Count > 0)
        {
            insights.Add(new CompanionInsight(
                "Reminder",
                $"You have {context.UpcomingReminders.Count} upcoming reminder(s).",
                68));
        }

        if (context.UnreadNotifications.Count > 0)
        {
            insights.Add(new CompanionInsight(
                "Notification",
                $"You have {context.UnreadNotifications.Count} unread notification(s).",
                66));
        }

        var todaysEvents = context.UpcomingCalendarEvents
            .Where(x => x.StartUtc.Date == now.Date)
            .OrderBy(x => x.StartUtc)
            .ToList();

        if (todaysEvents.Count >= 4)
        {
            insights.Add(new CompanionInsight(
                "Calendar",
                $"Today is busy with {todaysEvents.Count} calendar events.",
                81));
        }

        var deadlineKeywords = new[] { "deadline", "due", "submit", "review", "launch" };
        foreach (var calendarEvent in context.UpcomingCalendarEvents
                     .Where(x => x.StartUtc <= now.AddDays(7))
                     .Where(x => deadlineKeywords.Any(keyword =>
                         x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
        {
            insights.Add(new CompanionInsight(
                "Deadline",
                $"Calendar event '{calendarEvent.Title}' is coming up by {calendarEvent.StartUtc:u}.",
                78));
        }

        foreach (var calendarEvent in context.UpcomingCalendarEvents
                     .Where(x => !x.IsAllDay && string.IsNullOrWhiteSpace(x.Location)))
        {
            insights.Add(new CompanionInsight(
                "Calendar",
                $"Calendar event '{calendarEvent.Title}' does not have a location set.",
                64));
        }

        var overlappingEvents = context.UpcomingCalendarEvents
            .OrderBy(x => x.StartUtc)
            .Zip(context.UpcomingCalendarEvents.OrderBy(x => x.StartUtc).Skip(1))
            .Where(pair => pair.First.EndUtc > pair.Second.StartUtc)
            .Take(2)
            .ToList();

        foreach (var overlap in overlappingEvents)
        {
            insights.Add(new CompanionInsight(
                "Conflict",
                $"Calendar events '{overlap.First.Title}' and '{overlap.Second.Title}' overlap.",
                86));
        }

        var unreadMessages = context.ImportantRecentEmails.Where(x => !x.IsRead).ToList();
        if (unreadMessages.Count > 0)
        {
            insights.Add(new CompanionInsight(
                "Email",
                $"There are {unreadMessages.Count} unread-looking recent email message(s).",
                74));
        }

        foreach (var message in context.ImportantRecentEmails.Where(IsUrgentEmail).Take(3))
        {
            insights.Add(new CompanionInsight(
                "Email",
                $"Email '{message.Subject}' looks urgent.",
                83));
        }

        foreach (var message in context.ImportantRecentEmails.Where(HasBillOrDeadlineLanguage).Take(3))
        {
            insights.Add(new CompanionInsight(
                "Deadline",
                $"Email '{message.Subject}' may involve a bill, payment, or deadline.",
                82));
        }

        foreach (var message in context.ImportantRecentEmails.Where(x => x.HasAttachments).Take(2))
        {
            insights.Add(new CompanionInsight(
                "Email",
                $"Email '{message.Subject}' has an attachment.",
                62));
        }

        foreach (var message in context.ImportantRecentEmails.Where(x => !x.IsAnswered).Take(3))
        {
            insights.Add(new CompanionInsight(
                "Email",
                $"Email '{message.Subject}' appears unanswered.",
                70));
        }

        return insights
            .DistinctBy(x => x.Message)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Message)
            .ToList();
    }

    private static DateTime ResolveLastInteraction(Project project, PlanningContext context)
    {
        var topic = project.Title;
        var candidates = new List<DateTime>
        {
            project.UpdatedUtc
        };

        candidates.AddRange(context.RecentMessages
            .Where(x => TopicMatches(x.Content, topic))
            .Select(x => x.CreatedUtc));

        candidates.AddRange(context.OpenTasks
            .Where(x => TopicMatches($"{x.Title} {x.Description}", topic))
            .Select(x => x.CompletedUtc ?? x.CreatedUtc));

        candidates.AddRange(context.AllMemories
            .Where(x => TopicMatches($"{x.Summary} {x.Content}", topic))
            .Select(x => x.LastReferencedUtc ?? x.CreatedUtc));

        candidates.AddRange(context.Goals
            .Where(x => TopicMatches($"{x.Title} {x.Description}", topic))
            .Select(x => x.UpdatedUtc));

        candidates.AddRange(context.OpenLoops
            .Where(x => TopicMatches($"{x.Title} {x.Description}", topic))
            .Select(x => x.ClosedUtc ?? x.CreatedUtc));

        return candidates.Max();
    }

    private static IReadOnlyList<string> BuildPlanningTopics(PlanningContext context)
    {
        return context.Projects
            .Select(x => x.Title)
            .Concat(context.ProjectSuggestions.Select(x => x.Title))
            .Select(title => PlanningText.NormalizeTitle(title))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<CreateProjectSuggestionCommand> BuildProjectSuggestionCommands(
        IReadOnlyList<Message> userMessages)
    {
        var mentionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var message in userMessages)
        {
            var topicsInMessage = TitleCasePhraseRegex()
                .Matches(message.Content)
                .Cast<Match>()
                .Select(match => PlanningText.NormalizeTitle(match.Value))
                .Where(IsValidProjectCandidate)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var topic in topicsInMessage)
            {
                mentionCounts[topic] = mentionCounts.GetValueOrDefault(topic) + 1;
            }
        }

        return mentionCounts
            .Where(x => x.Value >= (x.Key.Contains(' ') ? 2 : 3))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Key)
            .Take(3)
            .Select(x => new CreateProjectSuggestionCommand(
                x.Key,
                $"Repeated references across {x.Value} recent user messages.",
                x.Value))
            .ToList();
    }

    private static bool IsValidProjectCandidate(string topic)
    {
        var normalized = PlanningText.NormalizeKey(topic);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || words.Length > 3)
        {
            return false;
        }

        if (words.All(ProjectStopWords.Contains))
        {
            return false;
        }

        return words.Any(word => word.Length >= 4 && !ProjectStopWords.Contains(word));
    }

    private static bool TryBuildGoalSuggestion(
        string content,
        out CreateGoalSuggestionCommand command)
    {
        if (!TryExtractAfterMarker(content, GoalMarkers, out var fragment))
        {
            command = default!;
            return false;
        }

        command = new CreateGoalSuggestionCommand(
            PlanningText.NormalizeTitle(fragment),
            PlanningText.NormalizeDescription(content));

        return true;
    }

    private static bool TryBuildOpenLoop(
        string content,
        out CreateOpenLoopCommand command)
    {
        if (!TryExtractAfterMarker(content, OpenLoopMarkers, out var fragment, out var marker))
        {
            command = default!;
            return false;
        }

        var status = marker == "waiting on"
            ? OpenLoopStatus.Waiting
            : OpenLoopStatus.Open;

        command = new CreateOpenLoopCommand(
            PlanningText.NormalizeTitle(fragment),
            PlanningText.NormalizeDescription(content),
            status);

        return true;
    }

    private static bool TryExtractAfterMarker(
        string content,
        IReadOnlyList<string> markers,
        out string fragment)
    {
        return TryExtractAfterMarker(content, markers, out fragment, out _);
    }

    private static bool TryExtractAfterMarker(
        string content,
        IReadOnlyList<string> markers,
        out string fragment,
        out string matchedMarker)
    {
        foreach (var marker in markers.OrderByDescending(x => x.Length))
        {
            var index = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            fragment = content[(index + marker.Length)..]
                .Trim()
                .Trim(':', '-', '.', '!', '?');

            fragment = fragment.StartsWith("to ", StringComparison.OrdinalIgnoreCase)
                ? fragment[3..].Trim()
                : fragment;

            matchedMarker = marker;
            return !string.IsNullOrWhiteSpace(fragment);
        }

        fragment = string.Empty;
        matchedMarker = string.Empty;
        return false;
    }

    private static bool TopicMatches(string? text, string topic)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalizedText = PlanningText.NormalizeKey(text);
        var normalizedTopic = PlanningText.NormalizeKey(topic);

        if (string.IsNullOrWhiteSpace(normalizedTopic))
        {
            return false;
        }

        if (normalizedText.Contains(normalizedTopic, StringComparison.Ordinal))
        {
            return true;
        }

        var topicTerms = normalizedTopic.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return topicTerms.Length > 0 &&
            topicTerms.All(term => normalizedText.Contains(term, StringComparison.Ordinal));
    }

    private static bool IsUrgentEmail(EmailMessageSnapshot message)
    {
        return ContainsAnyEmailText(message, ["urgent", "asap", "important", "action required", "immediately"]);
    }

    private static bool HasBillOrDeadlineLanguage(EmailMessageSnapshot message)
    {
        return ContainsAnyEmailText(message, ["bill", "payment", "invoice", "due", "deadline", "overdue"]);
    }

    private static bool ContainsAnyEmailText(EmailMessageSnapshot message, IReadOnlyList<string> terms)
    {
        var text = $"{message.Subject} {message.Preview} {message.Body}";
        return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"\b(?:[A-Z][A-Za-z0-9]+(?:\s+[A-Z][A-Za-z0-9]+){0,2})\b", RegexOptions.Compiled)]
    private static partial Regex TitleCasePhraseRegex();

    private sealed record PlanningContext(
        IReadOnlyList<TaskItem> OpenTasks,
        IReadOnlyList<ApprovalRequest> PendingApprovals,
        IReadOnlyList<MemoryEntry> RecentMemories,
        IReadOnlyList<Goal> Goals,
        IReadOnlyList<Project> Projects,
        IReadOnlyList<CalendarEventSnapshot> UpcomingCalendarEvents,
        IReadOnlyList<EmailMessageSnapshot> ImportantRecentEmails,
        IReadOnlyList<Reminder> UpcomingReminders,
        IReadOnlyList<Notification> UnreadNotifications,
        IReadOnlyList<OpenLoop> OpenLoops,
        IReadOnlyList<ProjectSuggestion> ProjectSuggestions,
        IReadOnlyList<GoalSuggestion> GoalSuggestions,
        IReadOnlyList<MemoryEntry> AllMemories,
        IReadOnlyList<Message> RecentMessages);
}
