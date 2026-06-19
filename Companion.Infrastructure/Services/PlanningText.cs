namespace Companion.Infrastructure.Services;

internal static class PlanningText
{
    public static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var characters = value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
            .ToArray();

        return string.Join(
            ' ',
            new string(characters).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static string NormalizeTitle(string value, int maxLength = 200)
    {
        var compact = CompactWhitespace(value);
        compact = compact.Trim(' ', ':', '-', '.', ',', ';', '!', '?');

        if (string.IsNullOrWhiteSpace(compact))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(value));
        }

        var capitalized = compact.Length == 1
            ? compact.ToUpperInvariant()
            : $"{char.ToUpperInvariant(compact[0])}{compact[1..]}";

        return Truncate(capitalized, maxLength);
    }

    public static string? NormalizeDescription(string? value, int maxLength = 2000)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Truncate(CompactWhitespace(value.Trim()), maxLength);
    }

    public static bool ContainsPhrase(string? text, string phrase)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalizedText = NormalizeKey(text);
        var normalizedPhrase = NormalizeKey(phrase);

        if (string.IsNullOrWhiteSpace(normalizedPhrase))
        {
            return false;
        }

        return $" {normalizedText} ".Contains($" {normalizedPhrase} ", StringComparison.Ordinal);
    }

    public static string CompactWhitespace(string value)
    {
        return string.Join(
            ' ',
            value.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..Math.Max(maxLength - 3, 1)].Trim()}...";
    }
}
