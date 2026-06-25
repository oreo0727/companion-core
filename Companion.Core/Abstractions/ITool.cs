using Companion.Core.Enums;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface ITool
{
    string Name { get; }

    string Description { get; }

    ToolRiskLevel RiskLevel { get; }

    Task<ToolExecutionResult> ExecuteAsync(ToolExecutionContext context);
}
