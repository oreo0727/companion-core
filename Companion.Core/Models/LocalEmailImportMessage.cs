namespace Companion.Core.Models;

public sealed record LocalEmailImportMessage(
    string? ExternalId,
    string Subject,
    string? FromName,
    string FromAddress,
    IReadOnlyList<string> ToAddresses,
    string? Preview,
    string? Body,
    DateTime ReceivedUtc,
    bool IsRead,
    bool HasAttachments,
    bool IsAnswered);
