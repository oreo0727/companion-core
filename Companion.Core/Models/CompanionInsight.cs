namespace Companion.Core.Models;

public sealed record CompanionInsight(
    string Category,
    string Message,
    int Priority);
