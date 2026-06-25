using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public class MemorySearchTool(IMemoryService memoryService) : ITool
{
    public string Name => ToolNames.MemorySearch;

    public string Description => "Search the authenticated user's saved memories.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var query = context.Input.TryGetProperty("query", out var queryElement)
            ? queryElement.GetString()?.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException("MemorySearch requires a non-empty 'query' value.");
        }

        var matches = await memoryService.SearchAsync(context.UserProfileId, query, 8, context.CancellationToken);
        var output = matches.Select(memory => new
        {
            memory.Id,
            memory.Type,
            memory.Summary,
            memory.Content,
            memory.Source,
            memory.Importance,
            memory.Sensitivity
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} matching memory item(s).");
    }
}
