using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Companion.Infrastructure.Services;

public class AiProviderConfigurationService(
    CompanionDbContext dbContext,
    IConfiguration appConfiguration,
    IDataProtectionProvider dataProtectionProvider,
    IAuditService auditService,
    TimeProvider timeProvider) : IAiProviderConfigurationService
{
    private readonly IDataProtector apiKeyProtector = dataProtectionProvider.CreateProtector("companion.ai-provider-api-keys.v1");

    public async Task<IReadOnlyList<AiProviderConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AiProviderConfigurations
            .AsNoTracking()
            .OrderBy(x => x.Provider)
            .ToListAsync(cancellationToken);
    }

    public async Task<AiProviderConfiguration?> GetConfigurationAsync(
        string provider,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = NormalizeProvider(provider);

        return await dbContext.AiProviderConfigurations
            .FirstOrDefaultAsync(x => x.Provider == normalizedProvider, cancellationToken);
    }

    public async Task<AiProviderConfiguration?> GetEnabledConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AiProviderConfigurations
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Provider)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AiProviderConfiguration> UpdateConfigurationAsync(
        UpdateAiProviderConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedProvider = NormalizeProvider(command.Provider);
        var providerConfiguration = await dbContext.AiProviderConfigurations
            .FirstOrDefaultAsync(x => x.Provider == normalizedProvider, cancellationToken)
            ?? throw new KeyNotFoundException($"AI provider '{normalizedProvider}' was not found.");

        providerConfiguration.Model = command.Model.Trim();
        providerConfiguration.ApiBaseUrl = command.ApiBaseUrl.Trim().TrimEnd('/');
        providerConfiguration.IsEnabled = command.IsEnabled;
        providerConfiguration.Temperature = Math.Clamp(command.Temperature, 0m, 2m);
        providerConfiguration.MaxTokens = Math.Max(command.MaxTokens, 128);
        providerConfiguration.TimeoutSeconds = Math.Clamp(command.TimeoutSeconds, 1, 300);
        providerConfiguration.UpdatedUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (command.ApiKey is not null)
        {
            providerConfiguration.ApiKeyEncrypted = string.IsNullOrWhiteSpace(command.ApiKey)
                ? string.Empty
                : apiKeyProtector.Protect(command.ApiKey.Trim());
        }

        if (providerConfiguration.IsEnabled)
        {
            var otherConfigurations = await dbContext.AiProviderConfigurations
                .Where(x => x.Provider != normalizedProvider && x.IsEnabled)
                .ToListAsync(cancellationToken);

            foreach (var otherConfiguration in otherConfigurations)
            {
                otherConfiguration.IsEnabled = false;
                otherConfiguration.UpdatedUtc = providerConfiguration.UpdatedUtc;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(
            null,
            AuditEventTypes.SettingsChanged,
            nameof(AiProviderConfiguration),
            providerConfiguration.Id.ToString(),
            $"Updated AI provider settings for '{providerConfiguration.Provider}'.",
            cancellationToken);
        return providerConfiguration;
    }

    public string? GetApiKey(AiProviderConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.ApiKeyEncrypted))
        {
            try
            {
                return apiKeyProtector.Unprotect(configuration.ApiKeyEncrypted);
            }
            catch
            {
                return configuration.ApiKeyEncrypted;
            }
        }

        return configuration.Provider switch
        {
            AiProviderNames.OpenAI => configurationSection("OPENAI_API_KEY", $"AiProviders:{AiProviderNames.OpenAI}:ApiKey"),
            AiProviderNames.Anthropic => configurationSection("ANTHROPIC_API_KEY", $"AiProviders:{AiProviderNames.Anthropic}:ApiKey"),
            AiProviderNames.Ollama => configurationSection("OLLAMA_API_KEY", $"AiProviders:{AiProviderNames.Ollama}:ApiKey"),
            _ => null
        };

        string? configurationSection(params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = appConfiguration[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }
    }

    private static string NormalizeProvider(string provider)
    {
        var normalized = provider.Trim();

        return normalized.ToLowerInvariant() switch
        {
            "openai" => AiProviderNames.OpenAI,
            "anthropic" => AiProviderNames.Anthropic,
            "ollama" => AiProviderNames.Ollama,
            _ => throw new ArgumentException($"Unsupported AI provider '{provider}'.", nameof(provider))
        };
    }
}
