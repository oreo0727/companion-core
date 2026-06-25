using Companion.Core.Entities;

namespace Companion.Core.Abstractions;

public interface IToolRegistry
{
    void RegisterTool(ITool tool);

    ITool? GetTool(string name);

    IReadOnlyList<ITool> GetAvailableTools();

    IReadOnlyList<ToolDefinition> BuildDefinitionsSnapshot();
}
