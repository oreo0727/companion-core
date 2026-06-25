namespace Companion.Core.Models;

public sealed record AiUsage(
    int PromptTokens,
    int CompletionTokens)
{
    public int TotalTokens => PromptTokens + CompletionTokens;
}
