using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IVoiceProviderRegistry
{
    IReadOnlyList<VoiceProviderSummary> GetProviders();

    ISpeechToTextProvider GetSpeechToTextProvider(string? name);

    ITextToSpeechProvider GetTextToSpeechProvider(string? name);
}
