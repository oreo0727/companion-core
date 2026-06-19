using Companion.Core.Enums;

namespace Companion.Core.Entities;

public class ApprovalRequest
{
    public Guid Id { get; set; }

    public Guid? UserProfileId { get; set; }

    public Guid? ConversationId { get; set; }

    public Guid? SourceMessageId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string RiskLevel { get; set; } = string.Empty;

    public ApprovalRequestStatus Status { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ReviewedUtc { get; set; }
}
