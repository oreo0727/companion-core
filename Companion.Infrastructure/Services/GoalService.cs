using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class GoalService(CompanionDbContext dbContext, TimeProvider timeProvider) : IGoalService
{
    public async Task<IReadOnlyList<Goal>> GetGoalsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Goals
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.TargetDateUtc)
            .ThenByDescending(x => x.UpdatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Goal>> GetActiveGoalsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Goals
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != GoalStatus.Completed &&
                x.Status != GoalStatus.Cancelled)
            .OrderBy(x => x.TargetDateUtc)
            .ThenByDescending(x => x.Priority)
            .ThenByDescending(x => x.UpdatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Goal> CreateGoalAsync(
        Guid userProfileId,
        CreateGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = PlanningText.NormalizeTitle(command.Title),
            Description = PlanningText.NormalizeDescription(command.Description),
            Status = command.Status,
            Priority = command.Priority,
            TargetDateUtc = command.TargetDateUtc,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        dbContext.Goals.Add(goal);
        await dbContext.SaveChangesAsync(cancellationToken);

        return goal;
    }

    public async Task<Goal?> UpdateGoalAsync(
        Guid userProfileId,
        Guid goalId,
        UpdateGoalCommand command,
        CancellationToken cancellationToken = default)
    {
        var goal = await dbContext.Goals
            .FirstOrDefaultAsync(
                x => x.Id == goalId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (goal is null)
        {
            return null;
        }

        goal.Title = PlanningText.NormalizeTitle(command.Title);
        goal.Description = PlanningText.NormalizeDescription(command.Description);
        goal.Status = command.Status ?? goal.Status;
        goal.Priority = command.Priority ?? goal.Priority;
        goal.TargetDateUtc = command.TargetDateUtc;
        goal.UpdatedUtc = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        return goal;
    }

    public async Task<IReadOnlyList<GoalSuggestion>> GetGoalSuggestionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.GoalSuggestions
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<GoalSuggestion?> CaptureGoalSuggestionAsync(
        Guid userProfileId,
        CreateGoalSuggestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(PlanningText.NormalizeKey(command.Title)))
        {
            return null;
        }

        var normalizedTitle = PlanningText.NormalizeKey(command.Title);
        var activeGoals = await dbContext.Goals
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != GoalStatus.Completed &&
                x.Status != GoalStatus.Cancelled)
            .ToListAsync(cancellationToken);

        if (activeGoals.Any(x => PlanningText.NormalizeKey(x.Title) == normalizedTitle))
        {
            return null;
        }

        var pendingSuggestions = await dbContext.GoalSuggestions
            .Where(x => x.UserProfileId == userProfileId && x.Status == SuggestionStatus.Pending)
            .ToListAsync(cancellationToken);

        var existingPendingSuggestion = pendingSuggestions
            .FirstOrDefault(x => PlanningText.NormalizeKey(x.Title) == normalizedTitle);

        if (existingPendingSuggestion is not null)
        {
            existingPendingSuggestion.Description ??= PlanningText.NormalizeDescription(command.Description);
            await dbContext.SaveChangesAsync(cancellationToken);
            return existingPendingSuggestion;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var suggestion = new GoalSuggestion
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = PlanningText.NormalizeTitle(command.Title),
            Description = PlanningText.NormalizeDescription(command.Description),
            Status = SuggestionStatus.Pending,
            CreatedUtc = now,
            ReviewedUtc = null
        };

        dbContext.GoalSuggestions.Add(suggestion);
        await dbContext.SaveChangesAsync(cancellationToken);

        return suggestion;
    }

    public async Task<Goal?> ApproveSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await dbContext.GoalSuggestions
            .FirstOrDefaultAsync(
                x => x.Id == suggestionId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (suggestion is null || suggestion.Status == SuggestionStatus.Rejected)
        {
            return null;
        }

        var normalizedTitle = PlanningText.NormalizeKey(suggestion.Title);
        var existingGoal = await dbContext.Goals
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != GoalStatus.Completed &&
                x.Status != GoalStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var goal = existingGoal.FirstOrDefault(x => PlanningText.NormalizeKey(x.Title) == normalizedTitle);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (goal is null)
        {
            goal = new Goal
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Title = PlanningText.NormalizeTitle(suggestion.Title),
                Description = PlanningText.NormalizeDescription(suggestion.Description),
                Status = GoalStatus.Active,
                Priority = PlanningPriority.Normal,
                TargetDateUtc = null,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            dbContext.Goals.Add(goal);
        }

        if (suggestion.Status == SuggestionStatus.Pending)
        {
            suggestion.Status = SuggestionStatus.Approved;
            suggestion.ReviewedUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return goal;
    }

    public async Task<GoalSuggestion?> RejectSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await dbContext.GoalSuggestions
            .FirstOrDefaultAsync(
                x => x.Id == suggestionId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (suggestion is null || suggestion.Status != SuggestionStatus.Pending)
        {
            return null;
        }

        suggestion.Status = SuggestionStatus.Rejected;
        suggestion.ReviewedUtc = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        return suggestion;
    }
}
