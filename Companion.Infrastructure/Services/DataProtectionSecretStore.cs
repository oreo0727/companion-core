using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class DataProtectionSecretStore(
    CompanionDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider) : ISecretStore
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("companion.secret-store.v1");

    public async Task SaveSecretAsync(
        string scope,
        string name,
        string secret,
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedScope = scope.Trim();
        var normalizedName = name.Trim();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var storedSecret = await dbContext.StoredSecrets
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId && x.Scope == normalizedScope && x.Name == normalizedName,
                cancellationToken);

        if (storedSecret is null)
        {
            storedSecret = new StoredSecret
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Scope = normalizedScope,
                Name = normalizedName,
                CreatedUtc = now
            };

            dbContext.StoredSecrets.Add(storedSecret);
        }

        storedSecret.EncryptedValue = protector.Protect(secret.Trim());
        storedSecret.UpdatedUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetSecretAsync(
        string scope,
        string name,
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedScope = scope.Trim();
        var normalizedName = name.Trim();

        var storedSecret = await dbContext.StoredSecrets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId && x.Scope == normalizedScope && x.Name == normalizedName,
                cancellationToken);

        if (storedSecret is null)
        {
            return null;
        }

        try
        {
            return protector.Unprotect(storedSecret.EncryptedValue);
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteSecretAsync(
        string scope,
        string name,
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedScope = scope.Trim();
        var normalizedName = name.Trim();

        var storedSecret = await dbContext.StoredSecrets
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId && x.Scope == normalizedScope && x.Name == normalizedName,
                cancellationToken);

        if (storedSecret is null)
        {
            return;
        }

        dbContext.StoredSecrets.Remove(storedSecret);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
