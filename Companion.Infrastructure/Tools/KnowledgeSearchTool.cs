using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public class KnowledgeSearchTool(IKnowledgeSearchService knowledgeSearchService) : ITool
{
    public string Name => ToolNames.KnowledgeSearch;

    public string Description => "Search the authenticated user's imported knowledge documents.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var query = context.Input.TryGetProperty("query", out var queryElement)
            ? queryElement.GetString()?.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException("KnowledgeSearch requires a non-empty 'query' value.");
        }

        var matches = await knowledgeSearchService.SearchAsync(
            context.UserProfileId,
            query,
            limit: 8,
            audit: true,
            cancellationToken: context.CancellationToken);

        var output = matches.Select(match => new
        {
            sourceId = match.Source.Id,
            sourceName = match.Source.Name,
            documentId = match.Document.Id,
            documentTitle = match.Document.Title,
            chunkId = match.Chunk.Id,
            match.Chunk.ChunkIndex,
            excerpt = match.Chunk.Content.Length > 300 ? $"{match.Chunk.Content[..300]}..." : match.Chunk.Content,
            relevanceScore = match.RelevanceScore
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} matching knowledge chunk(s).");
    }
}
