using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class ConnectorConnection
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ConnectorDefinitionId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public ConnectorConnectionStatus Status { get; set; }

    public string? AccessTokenEncrypted { get; set; }

    public string? RefreshTokenEncrypted { get; set; }

    public DateTime? ExpiresUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ConnectorDefinition? ConnectorDefinition { get; set; }

    public ICollection<ConnectorSyncRun> SyncRuns { get; set; } = new List<ConnectorSyncRun>();

    public ICollection<CalendarEventSnapshot> CalendarEvents { get; set; } = new List<CalendarEventSnapshot>();

    public ICollection<EmailMessageSnapshot> EmailMessages { get; set; } = new List<EmailMessageSnapshot>();

    public ICollection<FileDocumentSnapshot> FileDocuments { get; set; } = new List<FileDocumentSnapshot>();

    public ICollection<HomeDeviceSnapshot> HomeDevices { get; set; } = new List<HomeDeviceSnapshot>();

    public ICollection<HomeSensorSnapshot> HomeSensors { get; set; } = new List<HomeSensorSnapshot>();

    public ICollection<OAuthConsentGrant> OAuthConsentGrants { get; set; } = new List<OAuthConsentGrant>();
}
