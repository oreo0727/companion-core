using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class ToolExecution
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ToolDefinitionId { get; set; }

    public Guid? AgentRunId { get; set; }

    public ToolExecutionStatus Status { get; set; }

    public string InputJson { get; set; } = string.Empty;

    public string? OutputJson { get; set; }

    public string? Error { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ToolDefinition? ToolDefinition { get; set; }

    public AgentRun? AgentRun { get; set; }
}
