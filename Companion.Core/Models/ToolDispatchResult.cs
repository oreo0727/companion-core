using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record ToolDispatchResult(
    ToolExecution Execution,
    ToolDefinition Definition,
    ApprovalRequest? ApprovalRequest,
    bool ExecutedImmediately);
