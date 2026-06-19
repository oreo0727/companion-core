using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface ITaskService
{
    Task<IReadOnlyList<TaskItem>> GetTasksAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskItem>> GetOpenTasksAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<TaskItem> CreateTaskAsync(
        Guid userProfileId,
        CreateTaskItemCommand command,
        CancellationToken cancellationToken = default);

    Task<TaskItem?> UpdateTaskAsync(
        Guid userProfileId,
        Guid taskItemId,
        UpdateTaskItemCommand command,
        CancellationToken cancellationToken = default);
}
