using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class ProjectService(
    CompanionDbContext dbContext,
    IAdaptiveLearningService learningService,
    TimeProvider timeProvider) : IProjectService
{
    public async Task<IReadOnlyList<Project>> GetProjectsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.Priority)
            .ThenByDescending(x => x.UpdatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetActiveProjectsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != ProjectStatus.Completed &&
                x.Status != ProjectStatus.Archived)
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.UpdatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project> CreateProjectAsync(
        Guid userProfileId,
        CreateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = PlanningText.NormalizeTitle(command.Title),
            Description = PlanningText.NormalizeDescription(command.Description),
            Status = command.Status,
            Priority = command.Priority,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return project;
    }

    public async Task<Project?> UpdateProjectAsync(
        Guid userProfileId,
        Guid projectId,
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(
                x => x.Id == projectId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (project is null)
        {
            return null;
        }

        var previousStatus = project.Status;
        project.Title = PlanningText.NormalizeTitle(command.Title);
        project.Description = PlanningText.NormalizeDescription(command.Description);
        project.Status = command.Status ?? project.Status;
        project.Priority = command.Priority ?? project.Priority;
        project.UpdatedUtc = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        if (previousStatus != ProjectStatus.Completed && project.Status == ProjectStatus.Completed)
        {
            await learningService.RecordEventAsync(
                userProfileId,
                new RecordLearningEventCommand(
                    LearningEventTypes.ProjectCompleted,
                    nameof(Project),
                    project.Id.ToString(),
                    $"Project completed: {project.Title}",
                    3m),
                cancellationToken);
        }

        return project;
    }

    public async Task<IReadOnlyList<ProjectSuggestion>> GetProjectSuggestionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ProjectSuggestions
            .AsNoTracking()
            .Where(x => x.UserProfileId == userProfileId)
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.MentionCount)
            .ThenByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectSuggestion?> CaptureProjectSuggestionAsync(
        Guid userProfileId,
        CreateProjectSuggestionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(PlanningText.NormalizeKey(command.Title)))
        {
            return null;
        }

        var normalizedTitle = PlanningText.NormalizeKey(command.Title);
        var activeProjects = await dbContext.Projects
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != ProjectStatus.Completed &&
                x.Status != ProjectStatus.Archived)
            .ToListAsync(cancellationToken);

        if (activeProjects.Any(x => PlanningText.NormalizeKey(x.Title) == normalizedTitle))
        {
            return null;
        }

        var suggestions = await dbContext.ProjectSuggestions
            .Where(x => x.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);

        var existingPendingSuggestion = suggestions
            .FirstOrDefault(x =>
                x.Status == SuggestionStatus.Pending &&
                PlanningText.NormalizeKey(x.Title) == normalizedTitle);

        if (existingPendingSuggestion is not null)
        {
            existingPendingSuggestion.MentionCount = Math.Max(existingPendingSuggestion.MentionCount, command.MentionCount);
            existingPendingSuggestion.Description = PlanningText.NormalizeDescription(command.Description)
                ?? existingPendingSuggestion.Description;

            await dbContext.SaveChangesAsync(cancellationToken);
            return existingPendingSuggestion;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var suggestion = new ProjectSuggestion
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Title = PlanningText.NormalizeTitle(command.Title),
            Description = PlanningText.NormalizeDescription(command.Description),
            MentionCount = Math.Max(command.MentionCount, 1),
            Status = SuggestionStatus.Pending,
            CreatedUtc = now,
            ReviewedUtc = null
        };

        dbContext.ProjectSuggestions.Add(suggestion);
        await dbContext.SaveChangesAsync(cancellationToken);

        return suggestion;
    }

    public async Task<Project?> ApproveSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await dbContext.ProjectSuggestions
            .FirstOrDefaultAsync(
                x => x.Id == suggestionId && x.UserProfileId == userProfileId,
                cancellationToken);

        if (suggestion is null || suggestion.Status == SuggestionStatus.Rejected)
        {
            return null;
        }

        var normalizedTitle = PlanningText.NormalizeKey(suggestion.Title);
        var existingProjects = await dbContext.Projects
            .Where(x =>
                x.UserProfileId == userProfileId &&
                x.Status != ProjectStatus.Completed &&
                x.Status != ProjectStatus.Archived)
            .ToListAsync(cancellationToken);

        var project = existingProjects.FirstOrDefault(x => PlanningText.NormalizeKey(x.Title) == normalizedTitle);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (project is null)
        {
            project = new Project
            {
                Id = Guid.NewGuid(),
                UserProfileId = userProfileId,
                Title = PlanningText.NormalizeTitle(suggestion.Title),
                Description = PlanningText.NormalizeDescription(suggestion.Description),
                Status = ProjectStatus.Active,
                Priority = suggestion.MentionCount >= 5 ? PlanningPriority.High : PlanningPriority.Normal,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            dbContext.Projects.Add(project);
        }

        if (suggestion.Status == SuggestionStatus.Pending)
        {
            suggestion.Status = SuggestionStatus.Approved;
            suggestion.ReviewedUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<ProjectSuggestion?> RejectSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await dbContext.ProjectSuggestions
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
