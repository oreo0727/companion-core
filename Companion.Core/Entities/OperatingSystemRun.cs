using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class OperatingSystemRun
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string RoutineType { get; set; } = string.Empty;

    public OperatingSystemRunStatus Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string InsightsJson { get; set; } = "[]";

    public string ActionsJson { get; set; } = "[]";

    public string ForecastJson { get; set; } = "{}";

    public Guid? ScheduledAgentRunId { get; set; }

    public DateTime PeriodStartUtc { get; set; }

    public DateTime PeriodEndUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public AgentRun? ScheduledAgentRun { get; set; }
}
