using Companion.Core.Entities;

namespace Companion.Core.Abstractions;

public interface IUserPreferenceService
{
    Task<IReadOnlyList<UserPreference>> GetPreferencesAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<UserPreference> SetPreferenceAsync(
        Guid userProfileId,
        string preferenceType,
        string value,
        CancellationToken cancellationToken = default);
}
