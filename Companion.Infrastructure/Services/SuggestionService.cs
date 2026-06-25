using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class SuggestionService(
    CompanionDbContext dbContext,
    IMemoryService memoryService,
    ITaskService taskService,
    IGoalService goalService,
    IProjectService projectService,
    TimeProvider timeProvider) : ISuggestionService
{
    public async Task<IReadOnlyList<MemorySuggestion>> CaptureMemorySuggestionsAsync(
        Guid userProfileId,
        IReadOnlyList<MemorySuggestionCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var pendingSuggestions = await dbContext.MemorySuggestions
            .Where(x => x.UserProfileId == userProfileId && x.Status == SuggestionStatus.Pending)
            .ToListAsync(cancellationToken);
        var existingMemories = await dbContext.MemoryEntries
            .Where(x => x.UserProfileId == userProfileId && !x.IsArchived)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var createdSuggestions = new List<MemorySuggestion>();

        foreach (var candidate in candidates)
        {
            var normalizedKey = NormalizeKey($"{candidate.Type}:{candidate.Summary}:{candidate.Content}");

            if (existingMemories.Any(x => NormalizeKey($"{x.Type}:{x.Summary}:{x.Content}") == normalizedKey) ||
                pendingSuggestions.Any(x => NormalizeKey($"{x.Type}:{x.Summary}:{x.Content}") == normalizedKey))
            {
                continue;
            }

            var suggestion = new MemorySuggestion
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Type = candidate.Type.Trim(),
                Summary = candidate.Summary.Trim(),
                Content = candidate.Content.Trim(),
                Confidence = Math.Clamp(candidate.Confidence, 0m, 1m),
                Source = string.IsNullOrWhiteSpace(candidate.Source) ? "Conversation" : candidate.Source.Trim(),
                Importance = Math.Clamp(candidate.Importance, 1, 5),
                Sensitivity = string.IsNullOrWhiteSpace(candidate.Sensitivity) ? "Normal" : candidate.Sensitivity.Trim(),
                Status = SuggestionStatus.Pending,
                CreatedUtc = now
            };

            dbContext.MemorySuggestions.Add(suggestion);
            createdSuggestions.Add(suggestion);
            pendingSuggestions.Add(suggestion);
        }

        if (createdSuggestions.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return createdSuggestions;
    }

    public async Task<IReadOnlyList<TaskSuggestion>> CaptureTaskSuggestionsAsync(
        Guid userProfileId,
        IReadOnlyList<TaskSuggestionCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var pendingSuggestions = await dbContext.TaskSuggestions
            .Where(x => x.UserProfileId == userProfileId && x.Status == SuggestionStatus.Pending)
            .ToListAsync(cancellationToken);
        var existingTasks = await dbContext.TaskItems
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != TaskItemStatus.Completed &&
                x.Status != TaskItemStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var createdSuggestions = new List<TaskSuggestion>();

        foreach (var candidate in candidates)
        {
            var normalizedTitle = NormalizeKey(candidate.Title);

            if (existingTasks.Any(x => NormalizeKey(x.Title) == normalizedTitle) ||
                pendingSuggestions.Any(x => NormalizeKey(x.Title) == normalizedTitle))
            {
                continue;
            }

            var suggestion = new TaskSuggestion
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Title = candidate.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(candidate.Description) ? null : candidate.Description.Trim(),
                Priority = candidate.Priority,
                DueDateUtc = candidate.DueDateUtc,
                Status = SuggestionStatus.Pending,
                CreatedUtc = now
            };

            dbContext.TaskSuggestions.Add(suggestion);
            createdSuggestions.Add(suggestion);
            pendingSuggestions.Add(suggestion);
        }

        if (createdSuggestions.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return createdSuggestions;
    }

    public async Task<IReadOnlyList<SuggestionRecord>> GetSuggestionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        var memorySuggestions = await dbContext.MemorySuggestions
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);
        var goalSuggestions = await dbContext.GoalSuggestions
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);
        var projectSuggestions = await dbContext.ProjectSuggestions
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);
        var taskSuggestions = await dbContext.TaskSuggestions
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);

        return memorySuggestions.Select(ToRecord)
            .Concat(goalSuggestions.Select(ToRecord))
            .Concat(projectSuggestions.Select(ToRecord))
            .Concat(taskSuggestions.Select(ToRecord))
            .OrderBy(x => x.Status != SuggestionStatus.Pending)
            .ThenByDescending(x => x.CreatedUtc)
            .ToList();
    }

    public async Task<SuggestionActionResult?> ApproveSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var memorySuggestion = await dbContext.MemorySuggestions
            .FirstOrDefaultAsync(x => x.Id == suggestionId && x.UserProfileId == userProfileId, cancellationToken);
        if (memorySuggestion is not null)
        {
            var memory = await EnsureMemoryFromSuggestionAsync(userProfileId, memorySuggestion, cancellationToken);

            if (memorySuggestion.Status == SuggestionStatus.Pending)
            {
                memorySuggestion.Status = SuggestionStatus.Approved;
                memorySuggestion.ReviewedUtc = now;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new SuggestionActionResult(ToRecord(memorySuggestion), nameof(MemoryEntry), memory.Id, SuggestionKind.Memory);
        }

        var goal = await goalService.ApproveSuggestionAsync(userProfileId, suggestionId, cancellationToken);
        if (goal is not null)
        {
            var suggestion = await dbContext.GoalSuggestions
                .AsNoTracking()
                .FirstAsync(x => x.Id == suggestionId, cancellationToken);
            return new SuggestionActionResult(ToRecord(suggestion), nameof(Goal), goal.Id, SuggestionKind.Goal);
        }

        var project = await projectService.ApproveSuggestionAsync(userProfileId, suggestionId, cancellationToken);
        if (project is not null)
        {
            var suggestion = await dbContext.ProjectSuggestions
                .AsNoTracking()
                .FirstAsync(x => x.Id == suggestionId, cancellationToken);
            return new SuggestionActionResult(ToRecord(suggestion), nameof(Project), project.Id, SuggestionKind.Project);
        }

        var taskSuggestion = await dbContext.TaskSuggestions
            .FirstOrDefaultAsync(x => x.Id == suggestionId && x.UserProfileId == userProfileId, cancellationToken);
        if (taskSuggestion is not null)
        {
            var task = await EnsureTaskFromSuggestionAsync(userProfileId, taskSuggestion, cancellationToken);

            if (taskSuggestion.Status == SuggestionStatus.Pending)
            {
                taskSuggestion.Status = SuggestionStatus.Approved;
                taskSuggestion.ReviewedUtc = now;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return new SuggestionActionResult(ToRecord(taskSuggestion), nameof(TaskItem), task.Id, SuggestionKind.Task);
        }

        return null;
    }

    public async Task<SuggestionRecord?> RejectSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var memorySuggestion = await dbContext.MemorySuggestions
            .FirstOrDefaultAsync(x => x.Id == suggestionId && x.UserProfileId == userProfileId, cancellationToken);
        if (memorySuggestion is not null)
        {
            if (memorySuggestion.Status == SuggestionStatus.Pending)
            {
                memorySuggestion.Status = SuggestionStatus.Rejected;
                memorySuggestion.ReviewedUtc = now;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return ToRecord(memorySuggestion);
        }

        var goalSuggestion = await goalService.RejectSuggestionAsync(userProfileId, suggestionId, cancellationToken);
        if (goalSuggestion is not null)
        {
            return ToRecord(goalSuggestion);
        }

        var projectSuggestion = await projectService.RejectSuggestionAsync(userProfileId, suggestionId, cancellationToken);
        if (projectSuggestion is not null)
        {
            return ToRecord(projectSuggestion);
        }

        var taskSuggestion = await dbContext.TaskSuggestions
            .FirstOrDefaultAsync(x => x.Id == suggestionId && x.UserProfileId == userProfileId, cancellationToken);
        if (taskSuggestion is not null)
        {
            if (taskSuggestion.Status == SuggestionStatus.Pending)
            {
                taskSuggestion.Status = SuggestionStatus.Rejected;
                taskSuggestion.ReviewedUtc = now;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return ToRecord(taskSuggestion);
        }

        return null;
    }

    private async Task<MemoryEntry> EnsureMemoryFromSuggestionAsync(
        Guid userProfileId,
        MemorySuggestion suggestion,
        CancellationToken cancellationToken)
    {
        var existingMemories = await dbContext.MemoryEntries
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId && !x.IsArchived)
            .ToListAsync(cancellationToken);
        var suggestionKey = NormalizeKey($"{suggestion.Type}:{suggestion.Summary}:{suggestion.Content}");
        var existingMemory = existingMemories.FirstOrDefault(
            x => NormalizeKey($"{x.Type}:{x.Summary}:{x.Content}") == suggestionKey);

        if (existingMemory is not null)
        {
            return existingMemory;
        }

        return await memoryService.CreateMemoryAsync(
            userProfileId,
            new CreateMemoryCommand(
                suggestion.Type,
                suggestion.Summary,
                suggestion.Content,
                suggestion.Source,
                suggestion.Importance,
                suggestion.Sensitivity,
                suggestion.Confidence),
            cancellationToken);
    }

    private async Task<TaskItem> EnsureTaskFromSuggestionAsync(
        Guid userProfileId,
        TaskSuggestion suggestion,
        CancellationToken cancellationToken)
    {
        var existingTasks = await dbContext.TaskItems
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != TaskItemStatus.Completed &&
                x.Status != TaskItemStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var normalizedSuggestionTitle = NormalizeKey(suggestion.Title);
        var existingTask = existingTasks.FirstOrDefault(x => NormalizeKey(x.Title) == normalizedSuggestionTitle);

        if (existingTask is not null)
        {
            return existingTask;
        }

        return await taskService.CreateTaskAsync(
            userProfileId,
            new CreateTaskItemCommand(
                suggestion.Title,
                suggestion.Description,
                suggestion.Priority,
                suggestion.DueDateUtc,
                null),
            cancellationToken);
    }

    private static SuggestionRecord ToRecord(MemorySuggestion suggestion)
    {
        return new SuggestionRecord(
            suggestion.Id,
            SuggestionKind.Memory,
            suggestion.Summary,
            suggestion.Type,
            suggestion.Status,
            suggestion.CreatedUtc,
            suggestion.ReviewedUtc,
            suggestion.Content,
            $"importance={suggestion.Importance};confidence={suggestion.Confidence:0.00};sensitivity={suggestion.Sensitivity};source={suggestion.Source}");
    }

    private static SuggestionRecord ToRecord(GoalSuggestion suggestion)
    {
        return new SuggestionRecord(
            suggestion.Id,
            SuggestionKind.Goal,
            suggestion.Title,
            suggestion.Description,
            suggestion.Status,
            suggestion.CreatedUtc,
            suggestion.ReviewedUtc);
    }

    private static SuggestionRecord ToRecord(ProjectSuggestion suggestion)
    {
        return new SuggestionRecord(
            suggestion.Id,
            SuggestionKind.Project,
            suggestion.Title,
            suggestion.Description,
            suggestion.Status,
            suggestion.CreatedUtc,
            suggestion.ReviewedUtc,
            null,
            $"mentionCount={suggestion.MentionCount}");
    }

    private static SuggestionRecord ToRecord(TaskSuggestion suggestion)
    {
        return new SuggestionRecord(
            suggestion.Id,
            SuggestionKind.Task,
            suggestion.Title,
            suggestion.Description,
            suggestion.Status,
            suggestion.CreatedUtc,
            suggestion.ReviewedUtc,
            null,
            $"priority={suggestion.Priority};dueDateUtc={(suggestion.DueDateUtc.HasValue ? suggestion.DueDateUtc.Value.ToString("O") : "null")}");
    }

    private static string NormalizeKey(string value)
    {
        return string.Join(
            ' ',
            value
                .Trim()
                .ToLowerInvariant()
                .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries));
    }
}
