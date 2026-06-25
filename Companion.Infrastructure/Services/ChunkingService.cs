using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Models;

namespace Companion.Infrastructure.Services;

public class ChunkingService : IChunkingService
{
    private const int MaxChunkLength = 900;

    public Task<IReadOnlyList<ChunkedKnowledgeSegment>> ChunkAsync(
        string content,
        string title,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = NormalizeContent(content, mimeType);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult<IReadOnlyList<ChunkedKnowledgeSegment>>([]);
        }

        var segments = new List<ChunkedKnowledgeSegment>();
        var paragraphs = normalized
            .Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var buffer = new List<string>();
        var bufferLength = 0;
        var chunkIndex = 0;
        string? currentHeading = null;

        foreach (var paragraph in paragraphs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            currentHeading = DetectHeading(paragraph, currentHeading, mimeType);
            var normalizedParagraph = paragraph.Trim();
            if (normalizedParagraph.Length == 0)
            {
                continue;
            }

            if (normalizedParagraph.Length > MaxChunkLength)
            {
                FlushBuffer(buffer, ref bufferLength, ref chunkIndex, segments, title, mimeType, currentHeading);

                foreach (var fragment in SplitLongParagraph(normalizedParagraph))
                {
                    var metadataJson = JsonSerializer.Serialize(new
                    {
                        title,
                        mimeType,
                        heading = currentHeading,
                        length = fragment.Length
                    });
                    segments.Add(new ChunkedKnowledgeSegment(fragment, chunkIndex++, metadataJson));
                }

                continue;
            }

            if (bufferLength + normalizedParagraph.Length + 2 > MaxChunkLength && buffer.Count > 0)
            {
                FlushBuffer(buffer, ref bufferLength, ref chunkIndex, segments, title, mimeType, currentHeading);
            }

            buffer.Add(normalizedParagraph);
            bufferLength += normalizedParagraph.Length + 2;
        }

        FlushBuffer(buffer, ref bufferLength, ref chunkIndex, segments, title, mimeType, currentHeading);

        return Task.FromResult<IReadOnlyList<ChunkedKnowledgeSegment>>(segments);
    }

    private static string NormalizeContent(string content, string mimeType)
    {
        var normalized = content.Replace("\r\n", "\n").Trim();
        if (!string.Equals(mimeType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        using var document = JsonDocument.Parse(normalized);
        return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string? DetectHeading(string paragraph, string? currentHeading, string mimeType)
    {
        if (string.Equals(mimeType, "text/markdown", StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = paragraph.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                return trimmed.Trim('#', ' ', '\t');
            }
        }

        return currentHeading;
    }

    private static IEnumerable<string> SplitLongParagraph(string paragraph)
    {
        var remaining = paragraph.Trim();

        while (remaining.Length > MaxChunkLength)
        {
            var splitAt = remaining.LastIndexOf(' ', Math.Min(MaxChunkLength, remaining.Length - 1));
            if (splitAt < MaxChunkLength / 2)
            {
                splitAt = MaxChunkLength;
            }

            yield return remaining[..splitAt].Trim();
            remaining = remaining[splitAt..].Trim();
        }

        if (remaining.Length > 0)
        {
            yield return remaining;
        }
    }

    private static void FlushBuffer(
        List<string> buffer,
        ref int bufferLength,
        ref int chunkIndex,
        List<ChunkedKnowledgeSegment> segments,
        string title,
        string mimeType,
        string? currentHeading)
    {
        if (buffer.Count == 0)
        {
            return;
        }

        var combined = string.Join("\n\n", buffer).Trim();
        var metadataJson = JsonSerializer.Serialize(new
        {
            title,
            mimeType,
            heading = currentHeading,
            length = combined.Length
        });

        segments.Add(new ChunkedKnowledgeSegment(combined, chunkIndex++, metadataJson));
        buffer.Clear();
        bufferLength = 0;
    }
}
