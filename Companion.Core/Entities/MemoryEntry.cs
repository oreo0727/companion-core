namespace Companion.Core.Entities;

public class MemoryEntry
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public decimal Confidence { get; set; }

    public string Source { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime? LastReferencedUtc { get; set; }

    public int Importance { get; set; }

    public string Sensitivity { get; set; } = string.Empty;

    public DateTime? ExpiresUtc { get; set; }

    public bool IsArchived { get; set; }

    public UserProfile? UserProfile { get; set; }
}
