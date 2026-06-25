using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;

namespace Companion.Infrastructure.Services;

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> registeredTools = new(StringComparer.OrdinalIgnoreCase);

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        foreach (var tool in tools)
        {
            RegisterTool(tool);
        }
    }

    public void RegisterTool(ITool tool)
    {
        registeredTools[tool.Name] = tool;
    }

    public ITool? GetTool(string name)
    {
        registeredTools.TryGetValue(name, out var tool);
        return tool;
    }

    public IReadOnlyList<ITool> GetAvailableTools()
    {
        return registeredTools.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<ToolDefinition> BuildDefinitionsSnapshot()
    {
        return GetAvailableTools()
            .Select(tool => new ToolDefinition
            {
                Id = Guid.Empty,
                Name = tool.Name,
                Description = tool.Description,
                Category = string.Empty,
                RiskLevel = tool.RiskLevel,
                RequiresApproval = tool.RiskLevel != ToolRiskLevel.Low,
                Enabled = true,
                CreatedUtc = DateTime.UtcNow
            })
            .ToList();
    }
}
