using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/suggestions")]
[Authorize]
public class SuggestionsController(ISuggestionService suggestionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SuggestionRecordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SuggestionRecordResponse>>> GetSuggestions(CancellationToken cancellationToken)
    {
        var suggestions = await suggestionService.GetSuggestionsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(suggestions.Select(x => x.ToResponse()));
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(SuggestionActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuggestionActionResponse>> ApproveSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var result = await suggestionService.ApproveSuggestionAsync(
            User.GetRequiredUserProfileId(),
            id,
            cancellationToken);

        return result is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(SuggestionRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuggestionRecordResponse>> RejectSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var result = await suggestionService.RejectSuggestionAsync(
            User.GetRequiredUserProfileId(),
            id,
            cancellationToken);

        return result is null ? NotFound() : Ok(result.ToResponse());
    }

    [HttpPost("{id:guid}/ignore")]
    [ProducesResponseType(typeof(SuggestionRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuggestionRecordResponse>> IgnoreSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var result = await suggestionService.MarkSuggestionIgnoredAsync(
            User.GetRequiredUserProfileId(),
            id,
            cancellationToken);

        return result is null ? NotFound() : Ok(result.ToResponse());
    }
}
