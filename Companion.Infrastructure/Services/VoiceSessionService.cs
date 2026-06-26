using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class VoiceSessionService(
    CompanionDbContext dbContext,
    IVoiceProviderRegistry voiceProviderRegistry,
    IConversationService conversationService,
    IAgentRuntime agentRuntime,
    IAuditService auditService,
    TimeProvider timeProvider) : IVoiceSessionService
{
    public Task<IReadOnlyList<VoiceProviderSummary>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(voiceProviderRegistry.GetProviders());
    }

    public async Task<VoiceSession> StartSessionAsync(
        StartVoiceSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var stt = voiceProviderRegistry.GetSpeechToTextProvider(command.SpeechToTextProvider);
        var tts = voiceProviderRegistry.GetTextToSpeechProvider(command.TextToSpeechProvider);
        var conversation = command.ConversationId is Guid conversationId
            ? await conversationService.GetConversationAsync(command.UserProfileId, conversationId, cancellationToken)
                ?? throw new KeyNotFoundException("Conversation was not found.")
            : await conversationService.GetOrCreateDefaultConversationAsync(command.UserProfileId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var session = new VoiceSession
        {
            Id = Guid.NewGuid(),
            UserProfileId = command.UserProfileId,
            ConversationId = conversation.Id,
            Status = VoiceSessionStatus.Listening,
            SpeechToTextProvider = stt.Name,
            TextToSpeechProvider = tts.Name,
            IsWakeSession = command.IsWakeSession,
            WakePhrase = command.WakePhrase?.Trim(),
            StartedUtc = now,
            LastActivityUtc = now
        };

        dbContext.VoiceSessions.Add(session);
        if (command.IsWakeSession)
        {
            dbContext.VoiceInteractions.Add(NewInteraction(session, VoiceInteractionType.Wake, stt.Name, command.WakePhrase ?? "wake", now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(
            command.UserProfileId,
            AuditEventTypes.VoiceSessionStarted,
            nameof(VoiceSession),
            session.Id.ToString(),
            $"Started voice session using {stt.Name} STT and {tts.Name} TTS.",
            cancellationToken);

        return session;
    }

    public async Task<VoiceTranscriptionResult> TranscribeAsync(
        Guid userProfileId,
        Guid sessionId,
        VoiceTranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(userProfileId, sessionId, cancellationToken);
        var provider = voiceProviderRegistry.GetSpeechToTextProvider(session.SpeechToTextProvider);
        var result = await provider.TranscribeAsync(request, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        session.Status = VoiceSessionStatus.Active;
        session.LastActivityUtc = now;
        dbContext.VoiceInteractions.Add(NewInteraction(session, VoiceInteractionType.Transcription, result.Provider, result.Transcript, now, result.LatencyMs));
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(userProfileId, AuditEventTypes.VoiceTranscribed, nameof(VoiceSession), session.Id.ToString(), "Transcribed voice input.", cancellationToken);
        return result;
    }

    public async Task<VoiceSpeechResult> SpeakAsync(
        Guid userProfileId,
        Guid sessionId,
        VoiceSpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(userProfileId, sessionId, cancellationToken);
        var provider = voiceProviderRegistry.GetTextToSpeechProvider(session.TextToSpeechProvider);
        var result = await provider.SynthesizeAsync(request, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        session.LastActivityUtc = now;
        dbContext.VoiceInteractions.Add(NewInteraction(session, VoiceInteractionType.AssistantSpeech, result.Provider, request.Text, now, result.LatencyMs, result.Format));
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(userProfileId, AuditEventTypes.VoiceSpoken, nameof(VoiceSession), session.Id.ToString(), "Synthesized voice output.", cancellationToken);
        return result;
    }

    public async Task<VoiceConversationResult> ConverseAsync(
        Guid userProfileId,
        Guid sessionId,
        VoiceTranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transcript = await TranscribeAsync(userProfileId, sessionId, request, cancellationToken);
        var session = await GetSessionAsync(userProfileId, sessionId, cancellationToken);
        var chat = await agentRuntime.ProcessChatAsync(userProfileId, transcript.Transcript, session.ConversationId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        dbContext.VoiceInteractions.Add(NewInteraction(session, VoiceInteractionType.UserUtterance, transcript.Provider, transcript.Transcript, now, transcript.LatencyMs));
        await dbContext.SaveChangesAsync(cancellationToken);

        var speech = await SpeakAsync(userProfileId, sessionId, new VoiceSpeechRequest(chat.Reply, "default", "text/plain;base64"), cancellationToken);
        return new VoiceConversationResult(
            session,
            transcript.Transcript,
            chat.Reply,
            BuildStreamChunks(chat.Reply),
            speech);
    }

    public async Task<VoiceSession> InterruptAsync(
        Guid userProfileId,
        Guid sessionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(userProfileId, sessionId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        session.Status = VoiceSessionStatus.Interrupted;
        session.InterruptedUtc = now;
        session.LastActivityUtc = now;
        dbContext.VoiceInteractions.Add(NewInteraction(session, VoiceInteractionType.Interruption, "System", reason, now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteEventAsync(userProfileId, AuditEventTypes.VoiceInterrupted, nameof(VoiceSession), session.Id.ToString(), $"Interrupted voice session: {reason}", cancellationToken);
        return session;
    }

    public async Task<VoiceSession> EndSessionAsync(Guid userProfileId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(userProfileId, sessionId, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        session.Status = VoiceSessionStatus.Completed;
        session.EndedUtc = now;
        session.LastActivityUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<IReadOnlyList<VoiceInteraction>> GetHistoryAsync(
        Guid userProfileId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetSessionAsync(userProfileId, sessionId, cancellationToken);
        return await dbContext.VoiceInteractions
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && x.VoiceSessionId == sessionId)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    private async Task<VoiceSession> GetSessionAsync(Guid userProfileId, Guid sessionId, CancellationToken cancellationToken)
    {
        return await dbContext.VoiceSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserProfileId == userProfileId, cancellationToken)
            ?? throw new KeyNotFoundException("Voice session was not found.");
    }

    private static VoiceInteraction NewInteraction(
        VoiceSession session,
        VoiceInteractionType type,
        string provider,
        string text,
        DateTime now,
        long? latencyMs = null,
        string? audioReference = null)
    {
        return new VoiceInteraction
        {
            Id = Guid.NewGuid(),
            UserProfileId = session.UserProfileId,
            VoiceSessionId = session.Id,
            Type = type,
            Provider = provider,
            Text = text.Trim(),
            AudioReference = audioReference,
            LatencyMs = latencyMs,
            CreatedUtc = now
        };
    }

    private static IReadOnlyList<string> BuildStreamChunks(string reply)
    {
        return reply
            .Split(['.', '!', '?', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Length > 180 ? x[..180] : x)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .DefaultIfEmpty(reply)
            .ToList();
    }
}
