using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record UpdateGoalCommand(
    string Title,
    string? Description,
    GoalStatus? Status,
    PlanningPriority? Priority,
    DateTime? TargetDateUtc);
