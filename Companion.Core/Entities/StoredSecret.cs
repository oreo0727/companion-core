namespace Companion.Core.Entities;

public class StoredSecret
{
    public Guid Id { get; set; }

    public Guid? UserProfileId { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string EncryptedValue { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
