using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record CreateGoalCommand(
    string Title,
    string? Description,
    PlanningPriority Priority,
    DateTime? TargetDateUtc,
    GoalStatus Status = GoalStatus.Active);
