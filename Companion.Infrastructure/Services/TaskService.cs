using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class TaskService(CompanionDbContext dbContext, TimeProvider timeProvider) : ITaskService
{
    public async Task<IReadOnlyList<TaskItem>> GetTasksAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TaskItems
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.DueDateUtc)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetOpenTasksAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TaskItems
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != TaskItemStatus.Completed &&
                x.Status != TaskItemStatus.Cancelled)
            .OrderBy(x => x.DueDateUtc)
            .ThenByDescending(x => x.Priority)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem> CreateTaskAsync(
        Guid userProfileId,
        CreateTaskItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var status = command.Status;

        var taskItem = new TaskItem
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = command.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim(),
            Status = status,
            Priority = command.Priority,
            DueDateUtc = command.DueDateUtc,
            SourceMessageId = command.SourceMessageId,
            CompletedUtc = status == TaskItemStatus.Completed ? now : null,
            CreatedUtc = now
        };

        dbContext.TaskItems.Add(taskItem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return taskItem;
    }

    public async Task<TaskItem?> UpdateTaskAsync(
        Guid userProfileId,
        Guid taskItemId,
        UpdateTaskItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var taskItem = await dbContext.TaskItems
            .FirstOrDefaultAsync(
                x => x.Id == taskItemId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (taskItem is null)
        {
            return null;
        }

        taskItem.Title = command.Title.Trim();
        taskItem.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        taskItem.Status = command.Status ?? taskItem.Status;
        taskItem.Priority = command.Priority ?? taskItem.Priority;
        taskItem.DueDateUtc = command.DueDateUtc;

        if (taskItem.Status == TaskItemStatus.Completed)
        {
            taskItem.CompletedUtc ??= timeProvider.GetUtcNow().UtcDateTime;
        }
        else
        {
            taskItem.CompletedUtc = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return taskItem;
    }
}
