using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class AgentRun
{
    public Guid Id { get; set; }

    public Guid? UserProfileId { get; set; }

    public Guid? ConversationId { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public AgentRunStatus Status { get; set; }

    public string Input { get; set; } = string.Empty;

    public string? Output { get; set; }

    public string? Error { get; set; }

    public string? MetadataJson { get; set; }

    public string? Provider { get; set; }

    public string? Model { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? TotalTokens { get; set; }

    public long? LatencyMs { get; set; }

    public bool FallbackUsed { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? StartedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public ICollection<ToolExecution> ToolExecutions { get; set; } = new List<ToolExecution>();
}
