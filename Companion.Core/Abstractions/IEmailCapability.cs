using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IEmailCapability
{
    Task<IReadOnlyList<EmailMessageSnapshot>> GetImportantRecentAsync(
        Guid userProfileId,
        int daysBack = 14,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailMessageSnapshot>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<EmailMessageSnapshot?> ReadAsync(
        Guid userProfileId,
        Guid messageId,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<EmailDraftResult> CreateDraftAsync(
        Guid userProfileId,
        EmailDraftRequest request,
        CancellationToken cancellationToken = default);
}
