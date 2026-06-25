using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public class CreateTaskTool(ITaskService taskService) : ITool
{
    public string Name => ToolNames.CreateTask;

    public string Description => "Create a task for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Medium;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var title = context.Input.TryGetProperty("title", out var titleElement)
            ? titleElement.GetString()?.Trim()
            : null;
        var description = context.Input.TryGetProperty("description", out var descriptionElement)
            ? descriptionElement.GetString()?.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("CreateTask requires a non-empty 'title' value.");
        }

        var task = await taskService.CreateTaskAsync(
            context.UserProfileId,
            new CreateTaskItemCommand(
                title,
                description,
                TaskItemPriority.Normal,
                null,
                null,
                TaskItemStatus.Todo),
            context.CancellationToken);

        return new ToolExecutionResult(
            new
            {
                task.Id,
                task.Title,
                task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                task.CreatedUtc
            },
            $"Created task '{task.Title}'.");
    }
}
