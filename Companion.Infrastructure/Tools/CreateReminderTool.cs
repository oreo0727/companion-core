using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public class CreateReminderTool(INotificationService notificationService) : ITool
{
    public string Name => ToolNames.CreateReminder;

    public string Description => "Create an in-app reminder for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var title = context.Input.TryGetProperty("title", out var titleElement)
            ? titleElement.GetString()?.Trim()
            : null;
        var dueUtc = context.Input.TryGetProperty("dueUtc", out var dueElement) && dueElement.TryGetDateTime(out var parsedDueUtc)
            ? parsedDueUtc.ToUniversalTime()
            : (DateTime?)null;

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("CreateReminder requires a title.");
        }

        if (dueUtc is null)
        {
            throw new InvalidOperationException("CreateReminder requires a dueUtc value.");
        }

        var reminder = await notificationService.CreateReminderAsync(
            context.UserProfileId,
            new CreateReminderCommand(
                title,
                context.Input.TryGetProperty("description", out var descriptionElement)
                    ? descriptionElement.GetString()
                    : null,
                dueUtc.Value,
                "ManualReminder",
                null),
            context.CancellationToken);

        return new ToolExecutionResult(
            new
            {
                reminder.Id,
                reminder.Title,
                reminder.Description,
                reminder.DueUtc,
                Status = reminder.Status.ToString()
            },
            $"Created reminder '{reminder.Title}'.");
    }
}
