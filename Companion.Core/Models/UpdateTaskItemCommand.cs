using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record UpdateTaskItemCommand(
    string Title,
    string? Description,
    TaskItemStatus? Status,
    TaskItemPriority? Priority,
    DateTime? DueDateUtc);
