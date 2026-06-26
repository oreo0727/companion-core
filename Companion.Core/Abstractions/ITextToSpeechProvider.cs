using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface ITextToSpeechProvider
{
    string Name { get; }

    bool IsLocal { get; }

    bool StreamingReady { get; }

    Task<VoiceSpeechResult> SynthesizeAsync(
        VoiceSpeechRequest request,
        CancellationToken cancellationToken = default);
}
