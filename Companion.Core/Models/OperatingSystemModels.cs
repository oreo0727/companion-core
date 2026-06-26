using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record GenerateOperatingSystemRunCommand(
    string RoutineType,
    DateTime? PeriodStartUtc = null,
    DateTime? PeriodEndUtc = null);

public sealed record OperatingSystemRunResult(
    OperatingSystemRun Run,
    IReadOnlyList<AgentRun> ScheduledAgentRuns);
