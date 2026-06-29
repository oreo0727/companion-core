using Companion.Core.Entities;

namespace Companion.Core.Abstractions;

public interface IFileCapability
{
    Task<IReadOnlyList<FileDocumentSnapshot>> GetRecentAsync(
        Guid userProfileId,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileDocumentSnapshot>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<FileDocumentSnapshot?> ReadMetadataAsync(
        Guid userProfileId,
        Guid documentId,
        bool audit = true,
        CancellationToken cancellationToken = default);
}
