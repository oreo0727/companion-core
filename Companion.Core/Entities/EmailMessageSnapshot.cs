namespace Companion.Core.Entities;

public class EmailMessageSnapshot
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConnectorConnectionId { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string? FromName { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string? ToAddresses { get; set; }

    public string? Preview { get; set; }

    public string? Body { get; set; }

    public DateTime ReceivedUtc { get; set; }

    public bool IsRead { get; set; }

    public bool HasAttachments { get; set; }

    public bool IsAnswered { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ConnectorConnection? ConnectorConnection { get; set; }
}
