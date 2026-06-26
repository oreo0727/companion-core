using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Infrastructure.Tools;

public class EmailSearchTool(IConnectorSyncService connectorSyncService) : ITool
{
    public string Name => ToolNames.EmailSearch;

    public string Description => "Search read-only email snapshots for the authenticated user.";

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Low;

    public async Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context)
    {
        var query = context.Input.TryGetProperty("query", out var queryElement)
            ? queryElement.GetString()?.Trim() ?? string.Empty
            : string.Empty;
        var limit = context.Input.TryGetProperty("limit", out var limitElement) && limitElement.TryGetInt32(out var parsedLimit)
            ? Math.Clamp(parsedLimit, 1, 100)
            : 10;

        var messages = string.IsNullOrWhiteSpace(query)
            ? await connectorSyncService.GetRecentEmailMessagesAsync(context.UserProfileId, 14, limit, audit: true, cancellationToken: context.CancellationToken)
            : await connectorSyncService.SearchEmailMessagesAsync(context.UserProfileId, query, limit, audit: true, cancellationToken: context.CancellationToken);

        var output = messages.Select(x => new
        {
            id = x.Id,
            subject = x.Subject,
            fromName = x.FromName,
            fromAddress = x.FromAddress,
            preview = x.Preview,
            receivedUtc = x.ReceivedUtc,
            isRead = x.IsRead,
            hasAttachments = x.HasAttachments,
            isAnswered = x.IsAnswered,
            connector = x.ConnectorConnection?.DisplayName
        }).ToList();

        return new ToolExecutionResult(output, $"Found {output.Count} email message snapshot(s).");
    }
}
