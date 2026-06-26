using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public class GetBriefingTool(IChiefOfStaffService chiefOfStaffService) : ITool
{
    public string Name => ToolNames.GetBriefing;

    public string Description => "Fetch the current companion briefing for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var briefing = await chiefOfStaffService.GetBriefingAsync(context.UserProfileId, context.CancellationToken);

        var output = new
        {
            openTasks = briefing.OpenTasks.Select(task => new { task.Id, task.Title, Status = task.Status.ToString() }).ToList(),
            pendingApprovals = briefing.PendingApprovals.Select(approval => new { approval.Id, approval.Type, approval.RiskLevel }).ToList(),
            goals = briefing.Goals.Select(goal => new { goal.Id, goal.Title, Status = goal.Status.ToString() }).ToList(),
            projects = briefing.Projects.Select(project => new { project.Id, project.Title, Status = project.Status.ToString() }).ToList(),
            upcomingCalendarEvents = briefing.UpcomingCalendarEvents.Select(calendarEvent => new { calendarEvent.Id, calendarEvent.Title, calendarEvent.StartUtc, calendarEvent.EndUtc, calendarEvent.Location }).ToList(),
            openLoops = briefing.OpenLoops.Select(loop => new { loop.Id, loop.Title, Status = loop.Status.ToString() }).ToList(),
            insights = briefing.ChiefOfStaffInsights.Select(insight => new { insight.Category, insight.Message, insight.Priority }).ToList()
        };

        return new ToolExecutionResult(output, "Fetched the current companion briefing.");
    }
}
