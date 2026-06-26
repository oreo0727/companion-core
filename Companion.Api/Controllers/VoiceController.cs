using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/voice")]
[Authorize]
public class VoiceController(IVoiceSessionService voiceSessionService) : ControllerBase
{
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IEnumerable<VoiceProviderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<VoiceProviderResponse>>> GetProviders(CancellationToken cancellationToken)
    {
        var providers = await voiceSessionService.GetProvidersAsync(cancellationToken);
        return Ok(providers.Select(x => x.ToResponse()));
    }

    [HttpPost("sessions")]
    [ProducesResponseType(typeof(VoiceSessionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<VoiceSessionResponse>> StartSession(
        [FromBody] StartVoiceSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await voiceSessionService.StartSessionAsync(
            new StartVoiceSessionCommand(
                User.GetRequiredUserProfileId(),
                request.ConversationId,
                request.SpeechToTextProvider,
                request.TextToSpeechProvider,
                request.IsWakeSession,
                request.WakePhrase),
            cancellationToken);

        return Created($"/api/voice/sessions/{session.Id}", session.ToResponse());
    }

    [HttpPost("wake")]
    [ProducesResponseType(typeof(VoiceSessionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<VoiceSessionResponse>> Wake(
        [FromBody] WakeVoiceSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await voiceSessionService.StartSessionAsync(
            new StartVoiceSessionCommand(
                User.GetRequiredUserProfileId(),
                null,
                request.SpeechToTextProvider,
                request.TextToSpeechProvider,
                true,
                request.WakePhrase),
            cancellationToken);

        return Created($"/api/voice/sessions/{session.Id}", session.ToResponse());
    }

    [HttpPost("sessions/{sessionId:guid}/transcribe")]
    [ProducesResponseType(typeof(VoiceTranscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceTranscriptionResponse>> Transcribe(
        Guid sessionId,
        [FromBody] VoiceTranscriptionApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await voiceSessionService.TranscribeAsync(
                User.GetRequiredUserProfileId(),
                sessionId,
                new VoiceTranscriptionRequest(request.AudioContentBase64, request.SimulatedTranscript, request.Language),
                cancellationToken);
            return Ok(result.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("sessions/{sessionId:guid}/conversation")]
    [ProducesResponseType(typeof(VoiceConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceConversationResponse>> Converse(
        Guid sessionId,
        [FromBody] VoiceTranscriptionApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await voiceSessionService.ConverseAsync(
                User.GetRequiredUserProfileId(),
                sessionId,
                new VoiceTranscriptionRequest(request.AudioContentBase64, request.SimulatedTranscript, request.Language),
                cancellationToken);
            return Ok(result.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("sessions/{sessionId:guid}/speak")]
    [ProducesResponseType(typeof(VoiceSpeechResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceSpeechResponse>> Speak(
        Guid sessionId,
        [FromBody] VoiceSpeechApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await voiceSessionService.SpeakAsync(
                User.GetRequiredUserProfileId(),
                sessionId,
                new VoiceSpeechRequest(request.Text, request.Voice, request.Format),
                cancellationToken);
            return Ok(result.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("sessions/{sessionId:guid}/interrupt")]
    [ProducesResponseType(typeof(VoiceSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceSessionResponse>> Interrupt(
        Guid sessionId,
        [FromBody] InterruptVoiceSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await voiceSessionService.InterruptAsync(
                User.GetRequiredUserProfileId(),
                sessionId,
                request.Reason,
                cancellationToken);
            return Ok(session.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("sessions/{sessionId:guid}/end")]
    [ProducesResponseType(typeof(VoiceSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceSessionResponse>> End(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await voiceSessionService.EndSessionAsync(User.GetRequiredUserProfileId(), sessionId, cancellationToken);
            return Ok(session.ToResponse());
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("sessions/{sessionId:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<VoiceInteractionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VoiceInteractionResponse>>> GetHistory(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await voiceSessionService.GetHistoryAsync(User.GetRequiredUserProfileId(), sessionId, cancellationToken);
            return Ok(history.Select(x => x.ToResponse()));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public sealed class StartVoiceSessionRequest
{
    public Guid? ConversationId { get; init; }

    [MaxLength(100)]
    public string? SpeechToTextProvider { get; init; }

    [MaxLength(100)]
    public string? TextToSpeechProvider { get; init; }

    public bool IsWakeSession { get; init; }

    [MaxLength(200)]
    public string? WakePhrase { get; init; }
}

public sealed class WakeVoiceSessionRequest
{
    [Required]
    [MaxLength(200)]
    public string WakePhrase { get; init; } = "Companion";

    [MaxLength(100)]
    public string? SpeechToTextProvider { get; init; }

    [MaxLength(100)]
    public string? TextToSpeechProvider { get; init; }
}

public sealed class VoiceTranscriptionApiRequest
{
    public string? AudioContentBase64 { get; init; }

    [MaxLength(4000)]
    public string? SimulatedTranscript { get; init; }

    [MaxLength(40)]
    public string Language { get; init; } = "en-US";
}

public sealed class VoiceSpeechApiRequest
{
    [Required]
    [MaxLength(4000)]
    public string Text { get; init; } = string.Empty;

    [MaxLength(100)]
    public string Voice { get; init; } = "default";

    [MaxLength(100)]
    public string Format { get; init; } = "text/plain;base64";
}

public sealed class InterruptVoiceSessionRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = "User interruption";
}
