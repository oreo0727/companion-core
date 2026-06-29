using Companion.Core.Entities;

namespace Companion.Core.Abstractions;

public interface IPeopleCapability
{
    Task<IReadOnlyList<ContactSnapshot>> SearchAsync(
        Guid userProfileId,
        string query,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContactSnapshot>> GetRelevantContactsAsync(
        Guid userProfileId,
        int limit = 25,
        bool audit = true,
        CancellationToken cancellationToken = default);
}
