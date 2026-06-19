using Companion.Core.Enums;

namespace Companion.Core.Models;

public sealed record CreateOpenLoopCommand(
    string Title,
    string? Description,
    OpenLoopStatus Status = OpenLoopStatus.Open);
