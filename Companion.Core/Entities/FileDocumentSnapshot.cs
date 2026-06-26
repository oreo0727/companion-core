namespace Companion.Core.Entities;

public class FileDocumentSnapshot
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConnectorConnectionId { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public string? WebUrl { get; set; }

    public string? PreviewText { get; set; }

    public DateTime? ModifiedUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ConnectorConnection? ConnectorConnection { get; set; }
}
