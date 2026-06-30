using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/oauth")]
[Authorize]
public class OAuthController(
    IOAuthService oauthService,
    CompanionDbContext dbContext,
    ISecretStore secretStore) : ControllerBase
{
    private const string OAuthSecretScope = "OAuth";

    [HttpGet("providers")]
    [ProducesResponseType(typeof(IEnumerable<OAuthProviderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OAuthProviderResponse>>> GetProviders(CancellationToken cancellationToken)
    {
        var providers = await oauthService.GetProvidersAsync(cancellationToken);
        return Ok(providers.Select(x => x.ToResponse()));
    }

    [HttpGet("connections")]
    [ProducesResponseType(typeof(IEnumerable<OAuthConnectionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OAuthConnectionResponse>>> GetConnections(CancellationToken cancellationToken)
    {
        var connections = await oauthService.GetConnectionsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(connections.Select(x => x.ToResponse()));
    }

    [HttpGet("settings")]
    [Authorize(Roles = SystemRoles.Administrator)]
    [ProducesResponseType(typeof(IEnumerable<OAuthProviderSettingsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OAuthProviderSettingsResponse>>> GetSettings(CancellationToken cancellationToken)
    {
        var userProfileId = User.GetRequiredUserProfileId();
        var providers = await dbContext.OAuthProviderConfigurations
            .AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
        var response = new List<OAuthProviderSettingsResponse>();

        foreach (var provider in providers)
        {
            response.Add(await ToSettingsResponseAsync(provider, userProfileId, cancellationToken));
        }

        return Ok(response);
    }

    [HttpPut("settings/{provider}")]
    [Authorize(Roles = SystemRoles.Administrator)]
    [ProducesResponseType(typeof(OAuthProviderSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OAuthProviderSettingsResponse>> UpdateSettings(
        string provider,
        [FromBody] UpdateOAuthProviderSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userProfileId = User.GetRequiredUserProfileId();
        var providerConfiguration = await dbContext.OAuthProviderConfigurations
            .FirstOrDefaultAsync(x => x.Provider == provider && x.Enabled, cancellationToken);

        if (providerConfiguration is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.ClientId))
        {
            await secretStore.SaveSecretAsync(
                OAuthSecretScope,
                providerConfiguration.ClientIdSecretName,
                request.ClientId,
                userProfileId,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            await secretStore.SaveSecretAsync(
                OAuthSecretScope,
                providerConfiguration.ClientSecretSecretName,
                request.ClientSecret,
                userProfileId,
                cancellationToken);
        }

        return Ok(await ToSettingsResponseAsync(providerConfiguration, userProfileId, cancellationToken));
    }

    [HttpPost("{provider}/authorize")]
    [ProducesResponseType(typeof(OAuthAuthorizationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OAuthAuthorizationResponse>> BeginAuthorization(
        string provider,
        [FromBody] BeginOAuthAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await oauthService.BeginAuthorizationAsync(
                User.GetRequiredUserProfileId(),
                new OAuthAuthorizationCommand(
                    provider,
                    request.ConnectorProvider,
                    request.DisplayName,
                    request.RedirectUri,
                    request.Scopes),
                cancellationToken);

            return Created($"/api/oauth/authorization-requests/{result.AuthorizationRequestId}", result.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{provider}/callback")]
    [ProducesResponseType(typeof(OAuthConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OAuthConnectionResponse>> CompleteAuthorization(
        string provider,
        [FromBody] CompleteOAuthAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await oauthService.CompleteAuthorizationAsync(
                User.GetRequiredUserProfileId(),
                new OAuthCallbackCommand(
                    provider,
                    request.State,
                    request.Code,
                    request.AccessToken,
                    request.RefreshToken,
                    request.ExpiresUtc,
                    request.Subject,
                    request.DisplayName,
                    request.Scopes),
                cancellationToken);

            return Ok(result.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("connections/{connectionId:guid}")]
    [ProducesResponseType(typeof(OAuthConnectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OAuthConnectionResponse>> Disconnect(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await oauthService.DisconnectAsync(User.GetRequiredUserProfileId(), connectionId, cancellationToken);
            return Ok(result.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task<OAuthProviderSettingsResponse> ToSettingsResponseAsync(
        OAuthProviderConfiguration provider,
        Guid userProfileId,
        CancellationToken cancellationToken)
    {
        var clientId = await secretStore.GetSecretAsync(
            OAuthSecretScope,
            provider.ClientIdSecretName,
            userProfileId,
            cancellationToken);
        var clientSecret = await secretStore.GetSecretAsync(
            OAuthSecretScope,
            provider.ClientSecretSecretName,
            userProfileId,
            cancellationToken);

        return new OAuthProviderSettingsResponse(
            provider.Provider,
            provider.DisplayName,
            provider.Enabled,
            !string.IsNullOrWhiteSpace(clientId),
            !string.IsNullOrWhiteSpace(clientSecret),
            provider.ClientIdSecretName,
            provider.ClientSecretSecretName,
            provider.DefaultScopes
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList());
    }
}

public sealed class BeginOAuthAuthorizationRequest
{
    [Required]
    [MaxLength(100)]
    public string ConnectorProvider { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string RedirectUri { get; init; } = string.Empty;

    public IReadOnlyList<string> Scopes { get; init; } = [];
}

public sealed class CompleteOAuthAuthorizationRequest
{
    [Required]
    [MaxLength(200)]
    public string State { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Code { get; init; } = string.Empty;

    [MaxLength(8000)]
    public string? AccessToken { get; init; }

    [MaxLength(8000)]
    public string? RefreshToken { get; init; }

    public DateTime? ExpiresUtc { get; init; }

    [MaxLength(300)]
    public string? Subject { get; init; }

    [MaxLength(200)]
    public string? DisplayName { get; init; }

    public IReadOnlyList<string> Scopes { get; init; } = [];
}

public sealed class UpdateOAuthProviderSettingsRequest
{
    [MaxLength(1000)]
    public string? ClientId { get; init; }

    [MaxLength(4000)]
    public string? ClientSecret { get; init; }
}

public sealed record OAuthProviderSettingsResponse(
    string Provider,
    string DisplayName,
    bool Enabled,
    bool HasClientId,
    bool HasClientSecret,
    string ClientIdSecretName,
    string ClientSecretSecretName,
    IReadOnlyList<string> DefaultScopes);
