namespace Companion.Core.Models;

public sealed record OAuthProviderSummary(
    Guid Id,
    string Provider,
    string DisplayName,
    string AuthorizationEndpoint,
    IReadOnlyList<string> DefaultScopes,
    bool Enabled,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record OAuthAuthorizationCommand(
    string Provider,
    string ConnectorProvider,
    string DisplayName,
    string RedirectUri,
    IReadOnlyList<string> Scopes);

public sealed record OAuthAuthorizationResult(
    Guid AuthorizationRequestId,
    string Provider,
    string ConnectorProvider,
    string AuthorizationUrl,
    string State,
    IReadOnlyList<string> Scopes,
    DateTime ExpiresUtc);

public sealed record OAuthCallbackCommand(
    string Provider,
    string State,
    string Code,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresUtc,
    string? Subject,
    string? DisplayName,
    IReadOnlyList<string> Scopes);

public sealed record OAuthConnectionSummary(
    Guid ConnectionId,
    Guid ConnectorDefinitionId,
    string Provider,
    string ConnectorProvider,
    string DisplayName,
    string Status,
    IReadOnlyList<string> Scopes,
    string Subject,
    DateTime? ExpiresUtc,
    DateTime ConsentUtc,
    DateTime? RevokedUtc);
