namespace Companion.Core.Entities;

public class HomeSensorSnapshot
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public Guid ConnectorConnectionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SensorType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? Room { get; set; }
    public DateTime? ObservedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
    public ConnectorConnection? ConnectorConnection { get; set; }
}
