using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface ISpeechToTextProvider
{
    string Name { get; }

    bool IsLocal { get; }

    bool StreamingReady { get; }

    Task<VoiceTranscriptionResult> TranscribeAsync(
        VoiceTranscriptionRequest request,
        CancellationToken cancellationToken = default);
}
