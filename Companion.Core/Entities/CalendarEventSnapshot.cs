namespace Companion.Core.Entities;

public class CalendarEventSnapshot
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConnectorConnectionId { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public bool IsAllDay { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ConnectorConnection? ConnectorConnection { get; set; }
}
