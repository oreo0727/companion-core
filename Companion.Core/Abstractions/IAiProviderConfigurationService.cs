using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IAiProviderConfigurationService
{
    Task<IReadOnlyList<AiProviderConfiguration>> GetConfigurationsAsync(CancellationToken cancellationToken = default);

    Task<AiProviderConfiguration?> GetConfigurationAsync(string provider, CancellationToken cancellationToken = default);

    Task<AiProviderConfiguration?> GetEnabledConfigurationAsync(CancellationToken cancellationToken = default);

    Task<AiProviderConfiguration> UpdateConfigurationAsync(
        UpdateAiProviderConfigurationCommand command,
        CancellationToken cancellationToken = default);

    string? GetApiKey(AiProviderConfiguration configuration);
}
