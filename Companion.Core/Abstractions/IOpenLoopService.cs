using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IOpenLoopService
{
    Task<IReadOnlyList<OpenLoop>> GetOpenLoopsAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<OpenLoop> CreateOpenLoopAsync(
        Guid userProfileId,
        CreateOpenLoopCommand command,
        CancellationToken cancellationToken = default);

    Task<OpenLoop?> CaptureOpenLoopAsync(
        Guid userProfileId,
        CreateOpenLoopCommand command,
        CancellationToken cancellationToken = default);

    Task<OpenLoop?> CloseOpenLoopAsync(
        Guid userProfileId,
        Guid openLoopId,
        CancellationToken cancellationToken = default);
}
