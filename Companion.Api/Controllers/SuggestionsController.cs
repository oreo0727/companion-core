using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/suggestions")]
public class SuggestionsController(ISuggestionService suggestionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SuggestionRecordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SuggestionRecordResponse>>> GetSuggestions(CancellationToken cancellationToken)
    {
        var suggestions = await suggestionService.GetSuggestionsAsync(CompanionDefaults.LocalUserProfileId, cancellationToken);
        return Ok(suggestions.Select(x => x.ToResponse()));
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(SuggestionActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuggestionActionResponse>> ApproveSuggestion(Guid id, CancellationToken cancellationToken)
    {
        var result = await suggestionService.ApproveSuggestionAsync(
            CompanionDefaults.LocalUserProfileId,
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
            CompanionDefaults.LocalUserProfileId,
            id,
            cancellationToken);

        return result is null ? NotFound() : Ok(result.ToResponse());
    }
}
