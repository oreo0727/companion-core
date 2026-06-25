using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/preferences")]
[Authorize]
public class PreferencesController(IUserPreferenceService userPreferenceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserPreferenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserPreferenceResponse>>> GetPreferences(CancellationToken cancellationToken)
    {
        var preferences = await userPreferenceService.GetPreferencesAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(preferences.Select(x => x.ToResponse()));
    }

    [HttpPut("{preferenceType}")]
    [ProducesResponseType(typeof(UserPreferenceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserPreferenceResponse>> SetPreference(
        string preferenceType,
        [FromBody] SetUserPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var preference = await userPreferenceService.SetPreferenceAsync(
            User.GetRequiredUserProfileId(),
            preferenceType,
            request.Value,
            cancellationToken);

        return Ok(preference.ToResponse());
    }
}

public sealed class SetUserPreferenceRequest
{
    [Required]
    [MaxLength(4000)]
    public string Value { get; init; } = string.Empty;
}
