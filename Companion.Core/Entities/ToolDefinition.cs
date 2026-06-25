using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class ToolDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public ToolRiskLevel RiskLevel { get; set; }

    public bool RequiresApproval { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedUtc { get; set; }
}
