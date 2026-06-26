namespace Companion.Core.Entities;

public class HomeDeviceSnapshot
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public Guid ConnectorConnectionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Room { get; set; }
    public string? CapabilitiesJson { get; set; }
    public DateTime? LastSeenUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
    public ConnectorConnection? ConnectorConnection { get; set; }
}
