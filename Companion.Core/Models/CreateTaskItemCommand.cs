using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record CreateTaskItemCommand(
    string Title,
    string? Description,
    TaskItemPriority Priority,
    DateTime? DueDateUtc,
    Guid? SourceMessageId,
    TaskItemStatus Status = TaskItemStatus.Todo);
