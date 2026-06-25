using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class UserPreferenceService(
    CompanionDbContext dbContext,
    IAuditService auditService,
    TimeProvider timeProvider) : IUserPreferenceService
{
    public async Task<IReadOnlyList<UserPreference>> GetPreferencesAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserPreferences
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.PreferenceType)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserPreference> SetPreferenceAsync(
        Guid userProfileId,
        string preferenceType,
        string value,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = preferenceType.Trim();
        var normalizedValue = value.Trim();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var preference = await dbContext.UserPreferences
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId && x.PreferenceType == normalizedType,
                cancellationToken);

        if (preference is null)
        {
            preference = new UserPreference
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                PreferenceType = normalizedType,
                Value = normalizedValue,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            dbContext.UserPreferences.Add(preference);
        }
        else
        {
            preference.Value = normalizedValue;
            preference.UpdatedUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.PreferenceChanged,
            nameof(UserPreference),
            preference.Id.ToString(),
            $"Updated preference '{normalizedType}'.",
            cancellationToken);

        return preference;
    }
}
