using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IMemoryService
{
    Task<IReadOnlyList<MemoryEntry>> GetMemoriesAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemoryEntry>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<MemoryEntry> CreateMemoryAsync(Guid userProfileId, CreateMemoryCommand command, CancellationToken cancellationToken = default);

    Task<MemoryEntry?> ArchiveMemoryAsync(
        Guid userProfileId,
        Guid memoryEntryId,
        CancellationToken cancellationToken = default);
}
