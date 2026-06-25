using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class MemorySuggestion
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public decimal Confidence { get; set; }

    public string Source { get; set; } = string.Empty;

    public int Importance { get; set; }

    public string Sensitivity { get; set; } = string.Empty;

    public SuggestionStatus Status { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ReviewedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
