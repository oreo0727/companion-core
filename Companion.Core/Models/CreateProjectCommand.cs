using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record CreateProjectCommand(
    string Title,
    string? Description,
    PlanningPriority Priority,
    ProjectStatus Status = ProjectStatus.Active);
