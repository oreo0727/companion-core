namespace Companion.Core.Entities;

public class AgentDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public string ToolNamesJson { get; set; } = "[]";

    public string ContextPolicyJson { get; set; } = "{}";

    public decimal MemoryWeight { get; set; }

    public bool Enabled { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public ICollection<AgentRun> AgentRuns { get; set; } = new List<AgentRun>();
}
