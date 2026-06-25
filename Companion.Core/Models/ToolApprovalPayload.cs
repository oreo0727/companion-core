namespace Companion.Core.Models;

public sealed record ToolApprovalPayload(
    Guid ToolExecutionId,
    string ToolName,
    string InputJson);
