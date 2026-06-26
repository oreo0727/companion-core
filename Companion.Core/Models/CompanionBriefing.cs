using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record CompanionBriefing(
    IReadOnlyList<TaskItem> OpenTasks,
    IReadOnlyList<ApprovalRequest> PendingApprovals,
    IReadOnlyList<MemoryEntry> RecentMemories,
    IReadOnlyList<Goal> Goals,
    IReadOnlyList<Project> Projects,
    IReadOnlyList<CalendarEventSnapshot> UpcomingCalendarEvents,
    IReadOnlyList<EmailMessageSnapshot> ImportantRecentEmails,
    IReadOnlyList<OpenLoop> OpenLoops,
    IReadOnlyList<ProjectSuggestion> ProjectSuggestions,
    IReadOnlyList<GoalSuggestion> GoalSuggestions,
    IReadOnlyList<CompanionInsight> ChiefOfStaffInsights);
