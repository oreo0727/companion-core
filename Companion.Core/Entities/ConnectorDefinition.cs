using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class ConnectorDefinition
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool SupportsOAuth { get; set; }

    public ConnectorRiskLevel RiskLevel { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedUtc { get; set; }

    public ICollection<ConnectorConnection> Connections { get; set; } = new List<ConnectorConnection>();
}
