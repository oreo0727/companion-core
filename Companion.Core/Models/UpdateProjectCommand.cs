using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record UpdateProjectCommand(
    string Title,
    string? Description,
    ProjectStatus? Status,
    PlanningPriority? Priority);
