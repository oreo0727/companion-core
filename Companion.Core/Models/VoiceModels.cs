using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record VoiceProviderSummary(
    string Name,
    string ProviderType,
    bool Local,
    bool StreamingReady);

public sealed record StartVoiceSessionCommand(
    Guid UserProfileId,
    Guid? ConversationId,
    string? SpeechToTextProvider,
    string? TextToSpeechProvider,
    bool IsWakeSession,
    string? WakePhrase);

public sealed record VoiceTranscriptionRequest(
    string? AudioContentBase64,
    string? SimulatedTranscript,
    string Language);

public sealed record VoiceTranscriptionResult(
    string Provider,
    string Transcript,
    long LatencyMs);

public sealed record VoiceSpeechRequest(
    string Text,
    string Voice,
    string Format);

public sealed record VoiceSpeechResult(
    string Provider,
    string AudioContentBase64,
    string Format,
    long LatencyMs);

public sealed record VoiceConversationResult(
    VoiceSession Session,
    string Transcript,
    string Reply,
    IReadOnlyList<string> StreamChunks,
    VoiceSpeechResult Speech);
