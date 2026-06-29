using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Companion.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/settings/ai")]
[Authorize(Roles = SystemRoles.Administrator)]
public class AiSettingsController(
    IAiProviderConfigurationService configurationService,
    IEnumerable<IAIProvider> aiProviders) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AiProviderConfigurationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AiProviderConfigurationResponse>>> GetConfigurations(
        CancellationToken cancellationToken)
    {
        var configurations = await configurationService.GetConfigurationsAsync(cancellationToken);
        var response = configurations
            .Select(x => new AiProviderConfigurationSummary(
                x.Id,
                x.Provider,
                x.Model,
                x.ApiBaseUrl,
                x.IsEnabled,
                x.Temperature,
                x.MaxTokens,
                x.TimeoutSeconds,
                !string.IsNullOrWhiteSpace(configurationService.GetApiKey(x)),
                x.CreatedUtc,
                x.UpdatedUtc))
            .Select(x => x.ToResponse());

        return Ok(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(AiProviderConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiProviderConfigurationResponse>> UpdateConfiguration(
        [FromBody] UpdateAiProviderConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await configurationService.UpdateConfigurationAsync(
                new UpdateAiProviderConfigurationCommand(
                    request.Provider,
                    request.Model,
                    request.ApiBaseUrl,
                    request.ApiKey,
                    request.IsEnabled,
                    request.Temperature,
                    request.MaxTokens,
                    request.TimeoutSeconds),
                cancellationToken);

            return Ok(new AiProviderConfigurationSummary(
                configuration.Id,
                configuration.Provider,
                configuration.Model,
                configuration.ApiBaseUrl,
                configuration.IsEnabled,
                configuration.Temperature,
                configuration.MaxTokens,
                configuration.TimeoutSeconds,
                !string.IsNullOrWhiteSpace(configurationService.GetApiKey(configuration)),
                configuration.CreatedUtc,
                configuration.UpdatedUtc).ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{provider}/test")]
    [ProducesResponseType(typeof(AiProviderTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiProviderTestResponse>> TestProvider(
        string provider,
        CancellationToken cancellationToken)
    {
        var aiProvider = aiProviders.FirstOrDefault(x => string.Equals(x.ProviderName, provider, StringComparison.OrdinalIgnoreCase));
        if (aiProvider is null)
        {
            return NotFound();
        }

        var startedUtc = DateTime.UtcNow;
        try
        {
            var result = await aiProvider.CompleteAsync(
                new AiCompletionRequest(
                    [
                        new AiMessage("system", "You are a health-check endpoint. Reply with a short plain-text status."),
                        new AiMessage("user", "Return a five word or shorter Companion provider health response.")
                    ],
                    Temperature: 0,
                    MaxTokens: 32),
                cancellationToken);

            return Ok(new AiProviderTestResponse(
                provider,
                result.Model,
                "Succeeded",
                result.LatencyMs,
                result.Content,
                null,
                startedUtc,
                DateTime.UtcNow));
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TimeoutException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return Ok(new AiProviderTestResponse(
                provider,
                null,
                "Failed",
                null,
                null,
                ex.Message,
                startedUtc,
                DateTime.UtcNow));
        }
    }
}

public sealed class UpdateAiProviderConfigurationRequest
{
    [Required]
    [MaxLength(50)]
    public string Provider { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Model { get; init; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ApiBaseUrl { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? ApiKey { get; init; }

    public bool IsEnabled { get; init; }

    [Range(typeof(decimal), "0", "2")]
    public decimal Temperature { get; init; } = 0.4m;

    [Range(1, 32000)]
    public int MaxTokens { get; init; } = 600;

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;
}

public sealed record AiProviderTestResponse(
    string Provider,
    string? Model,
    string Status,
    long? LatencyMs,
    string? Reply,
    string? Error,
    DateTime StartedUtc,
    DateTime CompletedUtc);
