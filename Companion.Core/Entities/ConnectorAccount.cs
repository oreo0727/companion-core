using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class ConnectorAccount
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ConnectorAccountStatus Status { get; set; }

    public DateTime CreatedUtc { get; set; }
}
