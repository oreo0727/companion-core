using System.Text.Json;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public sealed class MultiAgentOrchestrator(
    CompanionDbContext dbContext,
    IAgentCatalog agentCatalog,
    IAuditService auditService,
    TimeProvider timeProvider) : IMultiAgentOrchestrator
{
    public async Task<AgentRun> ExecuteAsync(AgentRun agentRun, CancellationToken cancellationToken = default)
    {
        var definition = await agentCatalog.GetAgentAsync(agentRun.AgentName, cancellationToken)
            ?? await agentCatalog.GetAgentAsync(AgentNames.ChiefOfStaff, cancellationToken)
            ?? throw new InvalidOperationException("ChiefOfStaff agent definition is not configured.");

        agentRun.AgentDefinitionId = definition.Id;
        agentRun.AgentName = definition.Name;
        agentRun.Output = BuildOutput(agentRun, definition);
        agentRun.MetadataJson = BuildMetadata(agentRun, definition);

        if (agentRun.UserProfileId is { } userProfileId &&
            agentRun.ParentAgentRunId is null &&
            string.Equals(definition.Name, AgentNames.ChiefOfStaff, StringComparison.Ordinal))
        {
            var delegatedAgents = SelectDelegatedAgents(agentRun.Input);
            foreach (var delegatedAgent in delegatedAgents)
            {
                await QueueDelegatedRunAsync(agentRun, userProfileId, delegatedAgent, cancellationToken);
            }
        }

        return agentRun;
    }

    private async Task QueueDelegatedRunAsync(
        AgentRun parentRun,
        Guid userProfileId,
        string agentName,
        CancellationToken cancellationToken)
    {
        var definition = await agentCatalog.GetAgentAsync(agentName, cancellationToken);
        if (definition is null)
        {
            return;
        }

        var alreadyQueued = await dbContext.AgentRuns.AnyAsync(
            x => x.ParentAgentRunId == parentRun.Id && x.AgentName == definition.Name,
            cancellationToken);
        if (alreadyQueued)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var childRun = new AgentRun
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            ConversationId = parentRun.ConversationId,
            AgentDefinitionId = definition.Id,
            ParentAgentRunId = parentRun.Id,
            AgentName = definition.Name,
            DelegationReason = $"ChiefOfStaff delegated based on the request: {BuildShortInput(parentRun.Input)}",
            Status = AgentRunStatus.Pending,
            Input = parentRun.Input,
            CreatedUtc = now,
            MetadataJson = JsonSerializer.Serialize(new
            {
                kind = "agent-delegation",
                parentAgentRunId = parentRun.Id,
                delegatedBy = AgentNames.ChiefOfStaff,
                agent = definition.Name,
                reason = "Matched specialist responsibility."
            })
        };

        dbContext.AgentRuns.Add(childRun);
        await auditService.WriteEventAsync(
            userProfileId,
            AuditEventTypes.AgentRunDelegated,
            nameof(AgentRun),
            childRun.Id.ToString(),
            $"ChiefOfStaff delegated work to {definition.Name}.",
            cancellationToken);
    }

    private static string BuildOutput(AgentRun agentRun, AgentDefinition definition)
    {
        var tools = DeserializeStringArray(definition.ToolNamesJson);
        var contextPolicy = DeserializeObject(definition.ContextPolicyJson);
        var lines = new List<string>
        {
            $"{definition.DisplayName} handled the run.",
            $"Focus: {definition.Description}",
            $"Input: {BuildShortInput(agentRun.Input)}",
            $"Memory weight: {definition.MemoryWeight:0.00}",
            $"Allowed tools: {(tools.Count == 0 ? "none" : string.Join(", ", tools))}",
            $"Context policy: {JsonSerializer.Serialize(contextPolicy)}"
        };

        if (agentRun.ParentAgentRunId is not null)
        {
            lines.Add($"Delegated from AgentRun {agentRun.ParentAgentRunId}.");
        }

        lines.Add("Result: specialist run completed with explainable deterministic orchestration.");
        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildMetadata(AgentRun agentRun, AgentDefinition definition)
    {
        return JsonSerializer.Serialize(new
        {
            kind = "multi-agent-run",
            agent = definition.Name,
            agentDefinitionId = definition.Id,
            parentAgentRunId = agentRun.ParentAgentRunId,
            delegationReason = agentRun.DelegationReason,
            prompt = definition.Prompt,
            toolNames = DeserializeStringArray(definition.ToolNamesJson),
            contextPolicy = DeserializeObject(definition.ContextPolicyJson),
            memoryWeight = definition.MemoryWeight
        });
    }

    private static IReadOnlyList<string> SelectDelegatedAgents(string input)
    {
        var agents = new List<string>();
        AddIfMatches(agents, input, AgentNames.Planner, "plan", "schedule", "roadmap", "priority", "task");
        AddIfMatches(agents, input, AgentNames.Research, "research", "learn", "investigate", "document", "knowledge");
        AddIfMatches(agents, input, AgentNames.Coder, "code", "bug", "build", "api", "test", "refactor");
        AddIfMatches(agents, input, AgentNames.Writer, "write", "draft", "message", "summary", "doc");
        AddIfMatches(agents, input, AgentNames.Travel, "travel", "trip", "flight", "hotel", "itinerary");
        AddIfMatches(agents, input, AgentNames.Finance, "budget", "bill", "invoice", "finance", "payment");
        AddIfMatches(agents, input, AgentNames.Health, "health", "doctor", "medication", "exercise", "sleep");
        AddIfMatches(agents, input, AgentNames.Home, "home", "light", "sensor", "thermostat", "device");

        return agents.Count == 0 ? [AgentNames.Planner] : agents.Distinct(StringComparer.Ordinal).Take(4).ToList();
    }

    private static void AddIfMatches(List<string> agents, string input, string agentName, params string[] markers)
    {
        if (markers.Any(marker => input.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            agents.Add(agentName);
        }
    }

    private static string BuildShortInput(string input)
    {
        var normalized = input.Trim();
        return normalized.Length <= 240 ? normalized : $"{normalized[..237].Trim()}...";
    }

    private static IReadOnlyList<string> DeserializeStringArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static object DeserializeObject(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>();
        }
    }
}
