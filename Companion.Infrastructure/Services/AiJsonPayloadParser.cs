using System.Text.Json;
using System.Text.RegularExpressions;

namespace Companion.Infrastructure.Services;

internal static class AiJsonPayloadParser
{
    private static readonly Regex CodeFenceRegex = new(
        "```(?<lang>[A-Za-z0-9_-]+)?\\s*(?<body>[\\s\\S]*?)```",
        RegexOptions.Compiled);

    public static JsonDocument? ParseObjectDocument(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        if (TryParse(content, out var directDocument))
        {
            return directDocument;
        }

        foreach (var candidate in EnumerateCodeFenceCandidates(content))
        {
            if (TryParse(candidate, out var fencedDocument))
            {
                return fencedDocument;
            }
        }

        JsonDocument? bestDocument = null;
        var bestCandidateLength = -1;

        foreach (var candidate in EnumerateJsonObjectCandidates(content))
        {
            if (!TryParse(candidate, out var candidateDocument))
            {
                continue;
            }

            if (candidate.Length <= bestCandidateLength)
            {
                candidateDocument?.Dispose();
                continue;
            }

            bestDocument?.Dispose();
            bestDocument = candidateDocument;
            bestCandidateLength = candidate.Length;
        }

        return bestDocument;
    }

    private static bool TryParse(string content, out JsonDocument? document)
    {
        try
        {
            document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            document = null;
            return false;
        }
    }

    private static IEnumerable<string> EnumerateCodeFenceCandidates(string content)
    {
        var jsonCandidates = new List<string>();
        var plainCandidates = new List<string>();

        foreach (Match match in CodeFenceRegex.Matches(content))
        {
            var body = match.Groups["body"].Value.Trim();
            if (string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            var language = match.Groups["lang"].Value.Trim();

            if (string.Equals(language, "json", StringComparison.OrdinalIgnoreCase))
            {
                jsonCandidates.Add(body);
            }
            else
            {
                plainCandidates.Add(body);
            }
        }

        return jsonCandidates.Concat(plainCandidates);
    }

    private static IEnumerable<string> EnumerateJsonObjectCandidates(string content)
    {
        var candidates = new List<string>();
        var stackDepth = 0;
        var startIndex = -1;
        var inString = false;
        var isEscaped = false;

        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];

            if (isEscaped)
            {
                isEscaped = false;
                continue;
            }

            if (character == '\\' && inString)
            {
                isEscaped = true;
                continue;
            }

            if (character == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (character == '{')
            {
                if (stackDepth == 0)
                {
                    startIndex = index;
                }

                stackDepth++;
            }
            else if (character == '}' && stackDepth > 0)
            {
                stackDepth--;

                if (stackDepth == 0 && startIndex >= 0)
                {
                    candidates.Add(content[startIndex..(index + 1)]);
                    startIndex = -1;
                }
            }
        }

        return candidates;
    }
}
