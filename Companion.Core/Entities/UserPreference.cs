namespace Companion.Core.Entities;

public class UserPreference
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string PreferenceType { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
