using Companion.Core.Abstractions;
using Companion.Core.Models;

namespace Companion.Infrastructure.Services;

public class VoiceProviderRegistry(
    IEnumerable<ISpeechToTextProvider> speechToTextProviders,
    IEnumerable<ITextToSpeechProvider> textToSpeechProviders) : IVoiceProviderRegistry
{
    private readonly IReadOnlyList<ISpeechToTextProvider> sttProviders = speechToTextProviders.ToList();
    private readonly IReadOnlyList<ITextToSpeechProvider> ttsProviders = textToSpeechProviders.ToList();

    public IReadOnlyList<VoiceProviderSummary> GetProviders()
    {
        return sttProviders
            .Select(x => new VoiceProviderSummary(x.Name, "SpeechToText", x.IsLocal, x.StreamingReady))
            .Concat(ttsProviders.Select(x => new VoiceProviderSummary(x.Name, "TextToSpeech", x.IsLocal, x.StreamingReady)))
            .OrderBy(x => x.ProviderType)
            .ThenBy(x => x.Name)
            .ToList();
    }

    public ISpeechToTextProvider GetSpeechToTextProvider(string? name)
    {
        return FindProvider(sttProviders, name) ?? sttProviders.First();
    }

    public ITextToSpeechProvider GetTextToSpeechProvider(string? name)
    {
        return FindProvider(ttsProviders, name) ?? ttsProviders.First();
    }

    private static TProvider? FindProvider<TProvider>(IReadOnlyList<TProvider> providers, string? name)
        where TProvider : class
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return providers.FirstOrDefault(provider =>
            string.Equals(
                provider switch
                {
                    ISpeechToTextProvider stt => stt.Name,
                    ITextToSpeechProvider tts => tts.Name,
                    _ => string.Empty
                },
                name.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }
}
