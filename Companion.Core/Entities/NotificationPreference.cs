namespace Companion.Core.Entities;

public class NotificationPreference
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string PreferenceType { get; set; } = string.Empty;

    public bool InAppEnabled { get; set; }

    public int LeadTimeMinutes { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
