using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public class CalendarEventsTool(IConnectorSyncService connectorSyncService) : ITool
{
    public string Name => ToolNames.CalendarEvents;

    public string Description => "Retrieve upcoming calendar events for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var daysAhead = context.Input.TryGetProperty("daysAhead", out var daysAheadElement) && daysAheadElement.TryGetInt32(out var parsedDaysAhead)
            ? Math.Clamp(parsedDaysAhead, 1, 30)
            : 7;

        var events = await connectorSyncService.GetUpcomingCalendarEventsAsync(
            context.UserProfileId,
            daysAhead,
            audit: true,
            cancellationToken: context.CancellationToken);

        var output = events.Select(x => new
        {
            id = x.Id,
            title = x.Title,
            description = x.Description,
            location = x.Location,
            startUtc = x.StartUtc,
            endUtc = x.EndUtc,
            isAllDay = x.IsAllDay,
            connector = x.ConnectorConnection?.DisplayName
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} upcoming calendar event(s).");
    }
}
