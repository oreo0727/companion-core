using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IVoiceSessionService
{
    Task<IReadOnlyList<VoiceProviderSummary>> GetProvidersAsync(CancellationToken cancellationToken = default);

    Task<VoiceSession> StartSessionAsync(StartVoiceSessionCommand command, CancellationToken cancellationToken = default);

    Task<VoiceTranscriptionResult> TranscribeAsync(Guid userProfileId, Guid sessionId, VoiceTranscriptionRequest request, CancellationToken cancellationToken = default);

    Task<VoiceSpeechResult> SpeakAsync(Guid userProfileId, Guid sessionId, VoiceSpeechRequest request, CancellationToken cancellationToken = default);

    Task<VoiceConversationResult> ConverseAsync(Guid userProfileId, Guid sessionId, VoiceTranscriptionRequest request, CancellationToken cancellationToken = default);

    Task<VoiceSession> InterruptAsync(Guid userProfileId, Guid sessionId, string reason, CancellationToken cancellationToken = default);

    Task<VoiceSession> EndSessionAsync(Guid userProfileId, Guid sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VoiceInteraction>> GetHistoryAsync(Guid userProfileId, Guid sessionId, CancellationToken cancellationToken = default);
}
