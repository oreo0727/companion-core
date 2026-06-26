using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public class ListNotificationsTool(INotificationService notificationService) : ITool
{
    public string Name => ToolNames.ListNotifications;

    public string Description => "List in-app notifications for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var includeRead = context.Input.TryGetProperty("includeRead", out var includeReadElement) &&
                          includeReadElement.ValueKind == System.Text.Json.JsonValueKind.True;

        var notifications = await notificationService.GetNotificationsAsync(
            context.UserProfileId,
            includeRead,
            context.CancellationToken);

        var output = notifications.Select(x => new
        {
            id = x.Id,
            type = x.Type,
            title = x.Title,
            body = x.Body,
            severity = x.Severity.ToString(),
            status = x.Status.ToString(),
            createdUtc = x.CreatedUtc,
            readUtc = x.ReadUtc
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} notification(s).");
    }
}
