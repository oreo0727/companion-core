namespace Companion.Core.Abstractions;

public interface ISecretStore
{
    Task SaveSecretAsync(
        string scope,
        string name,
        string secret,
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default);

    Task<string?> GetSecretAsync(
        string scope,
        string name,
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default);

    Task DeleteSecretAsync(
        string scope,
        string name,
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default);
}
