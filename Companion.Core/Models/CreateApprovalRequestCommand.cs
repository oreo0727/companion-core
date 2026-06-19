namespace Companion.Core.Models;

public sealed record CreateApprovalRequestCommand(
    Guid? UserProfileId,
    Guid? ConversationId,
    Guid? SourceMessageId,
    string Type,
    string Reason,
    string Payload,
    string RiskLevel);
