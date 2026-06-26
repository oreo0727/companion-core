using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface ICompanionOperatingSystemService
{
    Task<IReadOnlyList<OperatingSystemRun>> GetRunsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<OperatingSystemRunResult> GenerateRunAsync(
        Guid userProfileId,
        GenerateOperatingSystemRunCommand command,
        CancellationToken cancellationToken = default);

    Task<OperatingSystemRunResult> OptimizeContextAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);
}
