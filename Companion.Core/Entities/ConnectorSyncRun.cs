using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class ConnectorSyncRun
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConnectorConnectionId { get; set; }

    public ConnectorSyncRunStatus Status { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public int ItemsSynced { get; set; }

    public string? Error { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ConnectorConnection? ConnectorConnection { get; set; }
}
