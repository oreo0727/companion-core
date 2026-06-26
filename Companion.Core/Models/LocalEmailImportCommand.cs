namespace Companion.Core.Models;

public sealed record LocalEmailImportCommand(
    string DisplayName,
    IReadOnlyList<LocalEmailImportMessage> Messages);
