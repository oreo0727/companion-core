using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetProjectsAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetActiveProjectsAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<Project> CreateProjectAsync(
        Guid userProfileId,
        CreateProjectCommand command,
        CancellationToken cancellationToken = default);

    Task<Project?> UpdateProjectAsync(
        Guid userProfileId,
        Guid projectId,
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectSuggestion>> GetProjectSuggestionsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<ProjectSuggestion?> CaptureProjectSuggestionAsync(
        Guid userProfileId,
        CreateProjectSuggestionCommand command,
        CancellationToken cancellationToken = default);

    Task<Project?> ApproveSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);

    Task<ProjectSuggestion?> RejectSuggestionAsync(
        Guid userProfileId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);
}
