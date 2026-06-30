using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class OAuthService(
    CompanionDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider,
    IOAuthTokenProtector tokenProtector,
    IAuditService auditService,
    ISecretStore secretStore,
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider) : IOAuthService
{
    private const string OAuthSecretScope = "OAuth";
    private readonly IDataProtector verifierProtector = dataProtectionProvider.CreateProtector("companion.oauth-verifiers.v1");

    public async Task<IReadOnlyList<OAuthProviderSummary>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providers = await dbContext.OAuthProviderConfigurations
            .AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return providers
            .Select(x => new OAuthProviderSummary(
                x.Id,
                x.Provider,
                x.DisplayName,
                x.AuthorizationEndpoint,
                SplitScopes(x.DefaultScopes),
                x.Enabled,
                x.CreatedUtc,
                x.UpdatedUtc))
            .ToList();
    }

    public async Task<OAuthAuthorizationResult> BeginAuthorizationAsync(
        Guid userProfileId,
        OAuthAuthorizationCommand command,
        CancellationToken cancellationToken = default)
    {
        var provider = await GetEnabledProviderAsync(command.Provider, cancellationToken);
        var connectorDefinition = await GetOAuthConnectorDefinitionAsync(command.ConnectorProvider, provider.Provider, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var scopes = NormalizeScopes(command.Scopes.Count == 0 ? SplitScopes(provider.DefaultScopes) : command.Scopes);
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        var codeVerifier = Base64Url(RandomNumberGenerator.GetBytes(48));

        var request = new OAuthAuthorizationRequest
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Provider = provider.Provider,
            ConnectorProvider = connectorDefinition.Provider,
            DisplayName = string.IsNullOrWhiteSpace(command.DisplayName)
                ? connectorDefinition.Name
                : command.DisplayName.Trim(),
            State = state,
            RedirectUri = command.RedirectUri.Trim(),
            Scopes = string.Join(' ', scopes),
            CodeVerifierEncrypted = verifierProtector.Protect(codeVerifier),
            CreatedUtc = now,
            ExpiresUtc = now.AddMinutes(10)
        };

        dbContext.OAuthAuthorizationRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        var clientId = await GetRequiredOAuthSecretAsync(
            userProfileId,
            provider.ClientIdSecretName,
            $"{provider.DisplayName} client ID",
            cancellationToken);
        var authorizationUrl = BuildAuthorizationUrl(provider.AuthorizationEndpoint, clientId, request, scopes, codeVerifier);

        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.OAuthAuthorizationStarted,
            nameof(OAuthAuthorizationRequest),
            request.Id.ToString(),
            $"Started OAuth authorization for {connectorDefinition.Name} through {provider.DisplayName}.",
            cancellationToken);

        return new OAuthAuthorizationResult(
            request.Id,
            provider.Provider,
            connectorDefinition.Provider,
            authorizationUrl,
            state,
            scopes,
            request.ExpiresUtc);
    }

    public async Task<OAuthConnectionSummary> CompleteAuthorizationAsync(
        Guid userProfileId,
        OAuthCallbackCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var provider = await GetEnabledProviderAsync(command.Provider, cancellationToken);
        var request = await dbContext.OAuthAuthorizationRequests
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId &&
                     x.Provider == provider.Provider &&
                     x.State == command.State &&
                     x.CompletedUtc == null,
                cancellationToken)
            ?? throw new KeyNotFoundException("OAuth authorization request was not found.");

        if (request.ExpiresUtc < now)
        {
            throw new InvalidOperationException("OAuth authorization request has expired.");
        }

        var connectorDefinition = await GetOAuthConnectorDefinitionAsync(request.ConnectorProvider, provider.Provider, cancellationToken);
        var tokenResponse = string.IsNullOrWhiteSpace(command.AccessToken)
            ? await ExchangeAuthorizationCodeAsync(provider, request, command.Code, cancellationToken)
            : new OAuthTokenExchangeResult(
                command.AccessToken.Trim(),
                command.RefreshToken?.Trim(),
                command.ExpiresUtc,
                NormalizeScopes(command.Scopes.Count == 0 ? SplitScopes(request.Scopes) : command.Scopes));
        var scopes = NormalizeScopes(command.Scopes.Count == 0
            ? tokenResponse.Scopes.Count == 0 ? SplitScopes(request.Scopes) : tokenResponse.Scopes
            : command.Scopes);
        var subject = string.IsNullOrWhiteSpace(command.Subject) ? $"{provider.Provider}:{userProfileId}" : command.Subject.Trim();
        var displayName = string.IsNullOrWhiteSpace(command.DisplayName) ? request.DisplayName : command.DisplayName.Trim();

        var connection = await dbContext.ConnectorConnections
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId &&
                     x.ConnectorDefinitionId == connectorDefinition.Id &&
                     x.DisplayName == displayName,
                cancellationToken);

        if (connection is null)
        {
            connection = new ConnectorConnection
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ConnectorDefinitionId = connectorDefinition.Id,
                DisplayName = displayName,
                CreatedUtc = now
            };
            dbContext.ConnectorConnections.Add(connection);
        }

        connection.Status = ConnectorConnectionStatus.Connected;
        connection.AccessTokenEncrypted = tokenProtector.Protect(tokenResponse.AccessToken);
        connection.RefreshTokenEncrypted = string.IsNullOrWhiteSpace(tokenResponse.RefreshToken)
            ? connection.RefreshTokenEncrypted
            : tokenProtector.Protect(tokenResponse.RefreshToken);
        connection.ExpiresUtc = tokenResponse.ExpiresUtc;
        connection.UpdatedUtc = now;

        var grant = await dbContext.OAuthConsentGrants
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId &&
                     x.ConnectorDefinitionId == connectorDefinition.Id &&
                     x.Subject == subject,
                cancellationToken);

        if (grant is null)
        {
            grant = new OAuthConsentGrant
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                ConnectorDefinitionId = connectorDefinition.Id,
                Provider = provider.Provider,
                Subject = subject,
                CreatedUtc = now
            };
            dbContext.OAuthConsentGrants.Add(grant);
        }

        grant.ConnectorConnection = connection;
        grant.Scopes = string.Join(' ', scopes);
        grant.ConsentUtc = now;
        grant.RevokedUtc = null;
        grant.UpdatedUtc = now;
        request.CompletedUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.OAuthConsentGranted,
            nameof(ConnectorConnection),
            connection.Id.ToString(),
            $"Granted {provider.DisplayName} OAuth consent for {connectorDefinition.Name} with {scopes.Count} scope(s).",
            cancellationToken);

        connection.ConnectorDefinition = connectorDefinition;
        return ToSummary(connection, grant, connectorDefinition);
    }

    public async Task<IReadOnlyList<OAuthConnectionSummary>> GetConnectionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        var grants = await dbContext.OAuthConsentGrants
            .AsNoTracking()
            .Include(x => x.ConnectorConnection)
                .ThenInclude(x => x!.ConnectorDefinition)
            .Include(x => x.ConnectorDefinition)
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.UpdatedUtc)
            .ToListAsync(cancellationToken);

        return grants
            .Where(x => x.ConnectorConnection is not null)
            .Select(x => ToSummary(x.ConnectorConnection!, x, x.ConnectorDefinition!))
            .ToList();
    }

    public async Task<OAuthConnectionSummary> DisconnectAsync(
        Guid userProfileId,
        Guid connectorConnectionId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var connection = await dbContext.ConnectorConnections
            .Include(x => x.ConnectorDefinition)
            .FirstOrDefaultAsync(x => x.Id == connectorConnectionId && x.UserProfileId == userProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Connector connection was not found.");

        var grant = await dbContext.OAuthConsentGrants
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId && x.ConnectorConnectionId == connectorConnectionId,
                cancellationToken)
            ?? throw new KeyNotFoundException("OAuth consent grant was not found.");

        connection.Status = ConnectorConnectionStatus.Disconnected;
        connection.AccessTokenEncrypted = null;
        connection.RefreshTokenEncrypted = null;
        connection.UpdatedUtc = now;
        grant.RevokedUtc = now;
        grant.UpdatedUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.OAuthConsentRevoked,
            nameof(ConnectorConnection),
            connection.Id.ToString(),
            $"Revoked OAuth consent for {connection.ConnectorDefinition?.Name ?? connection.DisplayName}.",
            cancellationToken);
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.ConnectorDisconnected,
            nameof(ConnectorConnection),
            connection.Id.ToString(),
            $"Disconnected OAuth connector '{connection.DisplayName}'.",
            cancellationToken);

        return ToSummary(connection, grant, connection.ConnectorDefinition!);
    }

    private async Task<OAuthProviderConfiguration> GetEnabledProviderAsync(
        string provider,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim();
        return await dbContext.OAuthProviderConfigurations
            .FirstOrDefaultAsync(x => x.Provider == normalizedProvider && x.Enabled, cancellationToken)
            ?? throw new KeyNotFoundException($"OAuth provider '{normalizedProvider}' was not found.");
    }

    private async Task<ConnectorDefinition> GetOAuthConnectorDefinitionAsync(
        string connectorProvider,
        string oauthProvider,
        CancellationToken cancellationToken)
    {
        var normalizedConnectorProvider = connectorProvider.Trim();
        var definition = await dbContext.ConnectorDefinitions
            .FirstOrDefaultAsync(
                x => x.Provider == normalizedConnectorProvider &&
                     x.SupportsOAuth &&
                     x.Enabled,
                cancellationToken)
            ?? throw new KeyNotFoundException($"OAuth connector '{normalizedConnectorProvider}' was not found.");

        if (!ConnectorMatchesProvider(definition.Provider, oauthProvider))
        {
            throw new InvalidOperationException($"{definition.Provider} does not use the {oauthProvider} OAuth provider.");
        }

        return definition;
    }

    private static OAuthConnectionSummary ToSummary(
        ConnectorConnection connection,
        OAuthConsentGrant grant,
        ConnectorDefinition definition)
    {
        return new OAuthConnectionSummary(
            connection.Id,
            definition.Id,
            grant.Provider,
            definition.Provider,
            connection.DisplayName,
            connection.Status.ToString(),
            SplitScopes(grant.Scopes),
            grant.Subject,
            connection.ExpiresUtc,
            grant.ConsentUtc,
            grant.RevokedUtc);
    }

    private static bool ConnectorMatchesProvider(string connectorProvider, string oauthProvider)
    {
        return oauthProvider switch
        {
            OAuthProviders.Google => connectorProvider is
                ConnectorProviders.GoogleCalendar or
                ConnectorProviders.GoogleDrive or
                ConnectorProviders.Gmail or
                ConnectorProviders.GooglePeople,
            OAuthProviders.Microsoft => connectorProvider is
                ConnectorProviders.MicrosoftCalendar or
                ConnectorProviders.OneDrive or
                ConnectorProviders.OutlookMail,
            _ => false
        };
    }

    private static string BuildAuthorizationUrl(
        string authorizationEndpoint,
        string clientId,
        OAuthAuthorizationRequest request,
        IReadOnlyList<string> scopes,
        string codeVerifier)
    {
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = Base64Url(challengeBytes);
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = request.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(' ', scopes),
            ["state"] = request.State,
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        return $"{authorizationEndpoint}?{string.Join('&', query.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value ?? string.Empty)}"))}";
    }

    private async Task<OAuthTokenExchangeResult> ExchangeAuthorizationCodeAsync(
        OAuthProviderConfiguration provider,
        OAuthAuthorizationRequest request,
        string code,
        CancellationToken cancellationToken)
    {
        var clientId = await GetRequiredOAuthSecretAsync(
            request.UserProfileId,
            provider.ClientIdSecretName,
            $"{provider.DisplayName} client ID",
            cancellationToken);
        var clientSecret = await GetRequiredOAuthSecretAsync(
            request.UserProfileId,
            provider.ClientSecretSecretName,
            $"{provider.DisplayName} client secret",
            cancellationToken);
        if (string.IsNullOrWhiteSpace(request.CodeVerifierEncrypted))
        {
            throw new InvalidOperationException("OAuth authorization request is missing its PKCE verifier.");
        }

        var codeVerifier = verifierProtector.Unprotect(request.CodeVerifierEncrypted);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code.Trim(),
            ["code_verifier"] = codeVerifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = request.RedirectUri
        });

        var httpClient = httpClientFactory.CreateClient(nameof(OAuthService));
        using var response = await httpClient.PostAsync(provider.TokenEndpoint, content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OAuth token exchange failed with HTTP {(int)response.StatusCode}: {TrimForMessage(responseText)}");
        }

        try
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;
            var accessToken = root.TryGetProperty("access_token", out var accessTokenElement)
                ? accessTokenElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException("OAuth token exchange did not return an access token.");
            }

            var refreshToken = root.TryGetProperty("refresh_token", out var refreshTokenElement)
                ? refreshTokenElement.GetString()
                : null;
            DateTime? expiresUtc = null;
            if (root.TryGetProperty("expires_in", out var expiresInElement) &&
                expiresInElement.ValueKind == JsonValueKind.Number &&
                expiresInElement.TryGetInt32(out var expiresInSeconds))
            {
                expiresUtc = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(expiresInSeconds);
            }

            var scopes = root.TryGetProperty("scope", out var scopeElement) && scopeElement.ValueKind == JsonValueKind.String
                ? SplitScopes(scopeElement.GetString() ?? string.Empty)
                : SplitScopes(request.Scopes);

            return new OAuthTokenExchangeResult(accessToken.Trim(), refreshToken?.Trim(), expiresUtc, scopes);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("OAuth token exchange returned malformed JSON.", ex);
        }
    }

    private async Task<string> GetRequiredOAuthSecretAsync(
        Guid userProfileId,
        string secretName,
        string description,
        CancellationToken cancellationToken)
    {
        var secret = await secretStore.GetSecretAsync(OAuthSecretScope, secretName, userProfileId, cancellationToken);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException($"{description} is not configured.");
        }

        return secret.Trim();
    }

    private static string TrimForMessage(string value)
    {
        var compact = value.ReplaceLineEndings(" ").Trim();
        return compact.Length <= 500 ? compact : string.Concat(compact.AsSpan(0, 500), "...");
    }

    private static IReadOnlyList<string> NormalizeScopes(IEnumerable<string> scopes)
    {
        return scopes
            .SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> SplitScopes(string scopes)
    {
        return NormalizeScopes([scopes]);
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed record OAuthTokenExchangeResult(
        string AccessToken,
        string? RefreshToken,
        DateTime? ExpiresUtc,
        IReadOnlyList<string> Scopes);
}
