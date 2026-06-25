using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class ToolExecutor(
    CompanionDbContext dbContext,
    IToolRegistry toolRegistry,
    IApprovalService approvalService,
    IAuditService auditService,
    TimeProvider timeProvider) : IToolExecutor
{
    public async Task<ToolDefinition?> GetDefinitionAsync(Guid toolDefinitionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ToolDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == toolDefinitionId, cancellationToken);
    }

    public async Task<IReadOnlyList<ToolDefinition>> GetDefinitionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from definition in dbContext.ToolDefinitions.AsNoTracking()
            join permission in dbContext.ToolPermissions.AsNoTracking()
                on definition.Id equals permission.ToolDefinitionId
            where definition.Enabled && permission.UserProfileId == userProfileId && permission.Allowed
            orderby definition.Category, definition.Name
            select definition)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ToolExecution>> GetExecutionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ToolExecutions
            .AsNoTracking()
            .Include(x => x.ToolDefinition)
            .Where(x => x.UserProfileId == userProfileId)
            .OrderByDescending(x => x.StartedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ToolDispatchResult> ExecuteAsync(
        Guid userProfileId,
        string toolName,
        string inputJson,
        Guid? agentRunId = null,
        Guid? conversationId = null,
        Guid? sourceMessageId = null,
        CancellationToken cancellationToken = default)
    {
        var definition = await GetDefinitionForExecutionAsync(userProfileId, toolName, cancellationToken);
        var toolExecution = new ToolExecution
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            ToolDefinitionId = definition.Id,
            AgentRunId = agentRunId,
            Status = RequiresApproval(definition) ? ToolExecutionStatus.AwaitingApproval : ToolExecutionStatus.Pending,
            InputJson = NormalizeJson(inputJson),
            StartedUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.ToolExecutions.Add(toolExecution);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (RequiresApproval(definition))
        {
            var approval = await approvalService.CreateApprovalAsync(
                new CreateApprovalRequestCommand(
                    userProfileId,
                    conversationId,
                    sourceMessageId,
                    ApprovalRequestTypes.ToolExecution,
                    $"Tool '{definition.Name}' requires approval before execution.",
                    JsonSerializer.Serialize(new ToolApprovalPayload(toolExecution.Id, definition.Name, toolExecution.InputJson)),
                    definition.RiskLevel.ToString()),
                cancellationToken);

            await WriteAuditAsync(
                userProfileId,
                AuditEventTypes.ToolExecutionRequested,
                toolExecution,
                definition.Name,
                toolExecution.InputJson,
                "Approval requested before execution.",
                cancellationToken);

            return new ToolDispatchResult(toolExecution, definition, approval, ExecutedImmediately: false);
        }

        await ExecuteNowAsync(toolExecution, definition, cancellationToken);
        return new ToolDispatchResult(toolExecution, definition, ApprovalRequest: null, ExecutedImmediately: true);
    }

    public async Task<ToolExecution?> ExecuteApprovedAsync(
        Guid userProfileId,
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        var approval = await dbContext.ApprovalRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == approvalRequestId &&
                     x.UserProfileId == userProfileId &&
                     x.Type == ApprovalRequestTypes.ToolExecution &&
                     x.Status == ApprovalRequestStatus.Approved,
                cancellationToken);

        if (approval is null || !TryParseApprovalPayload(approval.Payload, out var payload))
        {
            return null;
        }

        var execution = await dbContext.ToolExecutions
            .Include(x => x.ToolDefinition)
            .FirstOrDefaultAsync(
                x => x.Id == payload.ToolExecutionId &&
                     x.UserProfileId == userProfileId &&
                     x.Status == ToolExecutionStatus.AwaitingApproval,
                cancellationToken);

        if (execution is null || execution.ToolDefinition is null)
        {
            return null;
        }

        await ExecuteNowAsync(execution, execution.ToolDefinition, cancellationToken);
        return execution;
    }

    public async Task<ToolExecution?> RejectApprovedAsync(
        Guid userProfileId,
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        var approval = await dbContext.ApprovalRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == approvalRequestId &&
                     x.UserProfileId == userProfileId &&
                     x.Type == ApprovalRequestTypes.ToolExecution &&
                     x.Status == ApprovalRequestStatus.Rejected,
                cancellationToken);

        if (approval is null || !TryParseApprovalPayload(approval.Payload, out var payload))
        {
            return null;
        }

        var execution = await dbContext.ToolExecutions
            .Include(x => x.ToolDefinition)
            .FirstOrDefaultAsync(
                x => x.Id == payload.ToolExecutionId &&
                     x.UserProfileId == userProfileId &&
                     x.Status == ToolExecutionStatus.AwaitingApproval,
                cancellationToken);

        if (execution is null)
        {
            return null;
        }

        execution.Status = ToolExecutionStatus.Rejected;
        execution.Error = "Tool execution was rejected during approval.";
        execution.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(
            userProfileId,
            AuditEventTypes.ToolExecutionRejected,
            execution,
            execution.ToolDefinition?.Name ?? payload.ToolName,
            execution.InputJson,
            execution.Error,
            cancellationToken);

        return execution;
    }

    private async Task<ToolDefinition> GetDefinitionForExecutionAsync(
        Guid userProfileId,
        string toolName,
        CancellationToken cancellationToken)
    {
        var normalizedToolName = toolName.Trim();
        var definition = await dbContext.ToolDefinitions
            .FirstOrDefaultAsync(x => x.Name == normalizedToolName, cancellationToken)
            ?? throw new KeyNotFoundException($"Tool '{normalizedToolName}' was not found.");

        if (!definition.Enabled)
        {
            throw new InvalidOperationException($"Tool '{normalizedToolName}' is disabled.");
        }

        var permission = await dbContext.ToolPermissions
            .FirstOrDefaultAsync(
                x => x.UserProfileId == userProfileId && x.ToolDefinitionId == definition.Id,
                cancellationToken);

        if (permission is null || !permission.Allowed)
        {
            throw new UnauthorizedAccessException($"Tool '{normalizedToolName}' is not permitted for this user.");
        }

        if (toolRegistry.GetTool(definition.Name) is null)
        {
            throw new InvalidOperationException($"Tool '{definition.Name}' is enabled in data but not registered in the runtime.");
        }

        return definition;
    }

    private async Task ExecuteNowAsync(
        ToolExecution execution,
        ToolDefinition definition,
        CancellationToken cancellationToken)
    {
        var tool = toolRegistry.GetTool(definition.Name)
            ?? throw new InvalidOperationException($"Tool '{definition.Name}' is not registered.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        execution.Status = ToolExecutionStatus.Running;
        execution.Error = null;
        execution.CompletedUtc = null;
        execution.StartedUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            using var inputDocument = JsonDocument.Parse(execution.InputJson);
            var result = await tool.ExecuteAsync(
                new ToolExecutionContext(
                    execution.UserProfileId,
                    execution.ToolDefinitionId,
                    execution.AgentRunId,
                    inputDocument.RootElement.Clone(),
                    cancellationToken));

            execution.Status = ToolExecutionStatus.Completed;
            execution.OutputJson = JsonSerializer.Serialize(result.Output);
            execution.Error = null;
            execution.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);

            await WriteAuditAsync(
                execution.UserProfileId,
                AuditEventTypes.ToolExecutionCompleted,
                execution,
                definition.Name,
                execution.InputJson,
                result.Summary,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            execution.Status = ToolExecutionStatus.Failed;
            execution.Error = ex.Message;
            execution.CompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);

            await WriteAuditAsync(
                execution.UserProfileId,
                AuditEventTypes.ToolExecutionFailed,
                execution,
                definition.Name,
                execution.InputJson,
                ex.Message,
                cancellationToken);
        }
    }

    private async Task WriteAuditAsync(
        Guid userProfileId,
        string eventType,
        ToolExecution execution,
        string toolName,
        string inputJson,
        string? result,
        CancellationToken cancellationToken)
    {
        var description = $"Tool={toolName}; Status={execution.Status}; Input={Truncate(inputJson, 600)}; Result={Truncate(result, 900)}";
        await auditService.WriteEventAsync(
            userProfileId,
            eventType,
            nameof(ToolExecution),
            execution.Id.ToString(),
            description,
            cancellationToken);
    }

    private static bool RequiresApproval(ToolDefinition definition)
    {
        return definition.RequiresApproval || definition.RiskLevel != ToolRiskLevel.Low;
    }

    private static string NormalizeJson(string inputJson)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(inputJson) ? "{}" : inputJson);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static bool TryParseApprovalPayload(string payload, out ToolApprovalPayload result)
    {
        result = default!;

        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ToolApprovalPayload>(payload);
            if (parsed is null || parsed.ToolExecutionId == Guid.Empty || string.IsNullOrWhiteSpace(parsed.ToolName))
            {
                return false;
            }

            result = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Truncate(string? value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= limit ? trimmed : $"{trimmed[..Math.Max(0, limit - 3)]}...";
    }
}
