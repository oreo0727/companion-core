using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class OpenLoopService(CompanionDbContext dbContext, TimeProvider timeProvider) : IOpenLoopService
{
    public async Task<IReadOnlyList<OpenLoop>> GetOpenLoopsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OpenLoops
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != OpenLoopStatus.Closed)
            .OrderByDescending(x => x.Status == OpenLoopStatus.Waiting)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<OpenLoop> CreateOpenLoopAsync(
        Guid userProfileId,
        CreateOpenLoopCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var status = command.Status;
        var openLoop = new OpenLoop
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = PlanningText.NormalizeTitle(command.Title),
            Description = PlanningText.NormalizeDescription(command.Description),
            Status = status,
            CreatedUtc = now,
            ClosedUtc = status == OpenLoopStatus.Closed ? now : null
        };

        dbContext.OpenLoops.Add(openLoop);
        await dbContext.SaveChangesAsync(cancellationToken);

        return openLoop;
    }

    public async Task<OpenLoop?> CaptureOpenLoopAsync(
        Guid userProfileId,
        CreateOpenLoopCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(PlanningText.NormalizeKey(command.Title)))
        {
            return null;
        }

        var normalizedTitle = PlanningText.NormalizeKey(command.Title);
        var existingOpenLoops = await dbContext.OpenLoops
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != OpenLoopStatus.Closed)
            .ToListAsync(cancellationToken);

        if (existingOpenLoops.Any(x => PlanningText.NormalizeKey(x.Title) == normalizedTitle))
        {
            return null;
        }

        return await CreateOpenLoopAsync(userProfileId, command, cancellationToken);
    }

    public async Task<OpenLoop?> CloseOpenLoopAsync(
        Guid userProfileId,
        Guid openLoopId,
        CancellationToken cancellationToken = default)
    {
        var openLoop = await dbContext.OpenLoops
            .FirstOrDefaultAsync(
                x => x.Id == openLoopId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (openLoop is null)
        {
            return null;
        }

        openLoop.Status = OpenLoopStatus.Closed;
        openLoop.ClosedUtc = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        return openLoop;
    }
}
