using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IOAuthService
{
    Task<IReadOnlyList<OAuthProviderSummary>> GetProvidersAsync(CancellationToken cancellationToken = default);

    Task<OAuthAuthorizationResult> BeginAuthorizationAsync(
        Guid userProfileId,
        OAuthAuthorizationCommand command,
        CancellationToken cancellationToken = default);

    Task<OAuthConnectionSummary> CompleteAuthorizationAsync(
        Guid userProfileId,
        OAuthCallbackCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OAuthConnectionSummary>> GetConnectionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<OAuthConnectionSummary> DisconnectAsync(
        Guid userProfileId,
        Guid connectorConnectionId,
        CancellationToken cancellationToken = default);
}
