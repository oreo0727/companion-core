using System.Diagnostics;
using System.Text;
using Companion.Core.Abstractions;
using Companion.Core.Models;

namespace Companion.Infrastructure.Services;

public abstract class SimulatedSpeechToTextProvider : ISpeechToTextProvider
{
    public abstract string Name { get; }

    public abstract bool IsLocal { get; }

    public virtual bool StreamingReady => true;

    public Task<VoiceTranscriptionResult> TranscribeAsync(
        VoiceTranscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        cancellationToken.ThrowIfCancellationRequested();
        var transcript = string.IsNullOrWhiteSpace(request.SimulatedTranscript)
            ? $"[{Name}] transcription placeholder"
            : request.SimulatedTranscript.Trim();
        stopwatch.Stop();
        return Task.FromResult(new VoiceTranscriptionResult(Name, transcript, stopwatch.ElapsedMilliseconds));
    }
}

public abstract class SimulatedTextToSpeechProvider : ITextToSpeechProvider
{
    public abstract string Name { get; }

    public abstract bool IsLocal { get; }

    public virtual bool StreamingReady => true;

    public Task<VoiceSpeechResult> SynthesizeAsync(
        VoiceSpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = Encoding.UTF8.GetBytes($"{Name}:{request.Voice}:{request.Text.Trim()}");
        stopwatch.Stop();
        return Task.FromResult(new VoiceSpeechResult(
            Name,
            Convert.ToBase64String(bytes),
            string.IsNullOrWhiteSpace(request.Format) ? "text/plain;base64" : request.Format.Trim(),
            stopwatch.ElapsedMilliseconds));
    }
}

public sealed class OpenAISpeechToTextProvider : SimulatedSpeechToTextProvider
{
    public override string Name => "OpenAI";
    public override bool IsLocal => false;
}

public sealed class AzureSpeechToTextProvider : SimulatedSpeechToTextProvider
{
    public override string Name => "Azure";
    public override bool IsLocal => false;
}

public sealed class LocalWhisperSpeechToTextProvider : SimulatedSpeechToTextProvider
{
    public override string Name => "LocalWhisper";
    public override bool IsLocal => true;
}

public sealed class OpenAITextToSpeechProvider : SimulatedTextToSpeechProvider
{
    public override string Name => "OpenAI";
    public override bool IsLocal => false;
}

public sealed class AzureTextToSpeechProvider : SimulatedTextToSpeechProvider
{
    public override string Name => "Azure";
    public override bool IsLocal => false;
}

public sealed class LocalPiperTextToSpeechProvider : SimulatedTextToSpeechProvider
{
    public override string Name => "LocalPiper";
    public override bool IsLocal => true;
}
